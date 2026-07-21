using ArbiScannerAdminPanel.Abstractions.Interfaces.Services;
using ArbiScannerAdminPanel.Domain.Models;
using FluentResults;
using Microsoft.AspNetCore.Mvc;
using ArbiScannerWeb.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authorization;
using ArbiScannerAdminPanel.Domain.Models.DTOs;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Text.Json;
namespace ArbiScannerAdminPanel.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/{controller}")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentsService _paymentsService;
        private readonly IOxaPayService _oxaPayService;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(IPaymentsService paymentsService, IOxaPayService oxaPayService, ILogger<PaymentsController> logger)
        {
            _paymentsService = paymentsService;
            _oxaPayService = oxaPayService;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook()
        {
            // HMAC is computed over the exact raw bytes OxaPay sent, so this must read Request.Body
            // directly rather than use [FromBody] model binding — binding would parse then
            // re-serialize the JSON, which can change whitespace/property order and break the
            // signature comparison before verification even runs.
            Request.EnableBuffering();
            string rawBody;
            using (var reader = new StreamReader(Request.Body, leaveOpen: true))
            {
                rawBody = await reader.ReadToEndAsync();
            }
            Request.Body.Position = 0;

            var hmacHeader = Request.Headers["HMAC"].FirstOrDefault();
            if (!_oxaPayService.VerifyWebhookSignature(rawBody, hmacHeader))
            {
                _logger.LogWarning("Rejected OxaPay webhook: invalid or missing HMAC signature");
                return Unauthorized();
            }

            OxaPayWebhookPayloadDTO? payload;
            try
            {
                payload = JsonSerializer.Deserialize<OxaPayWebhookPayloadDTO>(rawBody);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Rejected OxaPay webhook: malformed JSON body");
                return BadRequest();
            }

            if (payload is null)
            {
                return BadRequest();
            }

            await _paymentsService.HandleOxaPayWebhookAsync(payload);
            return Ok();
        }
        [Authorize(Roles = "Administrator")]
        [HttpGet("GetAllPayments")]
        public async Task<ActionResult<Result<List<PaymentModel>>>> GetAllPayments(int page = 1)
        {
            var result = (await _paymentsService.GetAllPayments(page)).ToSerializable();
            return result;
        }

        [HttpGet("GetPaymentById")]
        public async Task<ActionResult<Result<PaymentResultDTO>>> GetPaymentById([FromQuery]int id)
        {
            var result = (await _paymentsService.GetPaymentDTOById(id)).ToSerializable();
            return result;
        }
        [Authorize(Roles = "Administrator")]
        [HttpDelete("RemovePayments")]
        public async Task<ActionResult<Result>> RemovePayment([FromQuery] List<int> ids)
        {
            var result = await _paymentsService.RemovePayments(ids);
            return result.ToSerializable();
        }

        [Authorize(Roles = "Administrator")]
        [HttpGet("GetPaymentsForUser")]
        public async Task<ActionResult<Result<List<UserSubscriptionPaymentDTO>>>> GetPaymentsForUser([FromQuery] string userId)
        {
            var result = await _paymentsService.GetPaymentsForUser(userId);
            if (result.IsFailed)
            {
                return Result.Fail<List<UserSubscriptionPaymentDTO>>(result.Errors).ToSerializable();
            }

            return Result.Ok(result.Value.Select(MapPayment).ToList()).ToSerializable();
        }

        [HttpPost("CreatePaymentForUser")]
        public async Task<ActionResult<Result<UserSubscriptionPaymentDTO>>> CreatePaymentForUser([FromBody] CreatePaymentForUserRequestDTO request)
        {
            var payment = new UserSubscriptionPayment
            {
                UserId = request.UserId,
                SubscriptionId = request.SubscriptionId
            };

            var result = await _paymentsService.CreatePaymentForUser(payment);
            if (result.IsFailed)
            {
                return Result.Fail<UserSubscriptionPaymentDTO>(result.Errors).ToSerializable();
            }

            return Result.Ok(MapPayment(result.Value)).ToSerializable();
        }

        [HttpGet("GetActivePaymentForUser")]
        public async Task<ActionResult<Result<UserSubscriptionPaymentDTO>>> GetActivePaymentForUser([FromQuery] string userId)
        {
            var result = await _paymentsService.GetActivePaymentForUser(userId);
            if (result.IsFailed)
            {
                return Result.Fail<UserSubscriptionPaymentDTO>(result.Errors).ToSerializable();
            }

            return Result.Ok(MapPayment(result.Value)).ToSerializable();
        }


        [HttpGet("GetUserPaymentByIdAsync")]
        public async Task<ActionResult<Result<UserSubscriptionPaymentDTO>>> GetUserPaymentByIdAsync([FromQuery] int paymentId)
        {
            var result = await _paymentsService.GetUserPaymentByIdAsync(paymentId);
            if (result.IsFailed)
            {
                return Result.Fail<UserSubscriptionPaymentDTO>(result.Errors).ToSerializable();
            }

            return Result.Ok(MapPayment(result.Value)).ToSerializable();
        }

        [HttpPost("CancelPayment")]
        public async Task<ActionResult<Result>> CancelPayment([FromQuery] int userSubscriptionPaymentId)
        {
            var result = await _paymentsService.CancelPayment(userSubscriptionPaymentId);
            return result.ToSerializable();
        }

        private static UserSubscriptionPaymentDTO MapPayment(UserSubscriptionPayment payment)
        {
            return new UserSubscriptionPaymentDTO
            {
                Id = payment.Id,
                UserId = payment.UserId,
                SubscriptionId = payment.SubscriptionId,
                PaymentId = payment.PaymentId,
                ExpirationDate = payment.ExpirationDate,
                SubscriptionType = payment.Subscription?.Type ?? string.Empty,
                SubscriptionPrice = payment.Subscription?.Price ?? 0,
                Payment = payment.Payment == null
                    ? null
                    : new PaymentResultDTO
                    {
                        Id = payment.Payment.Id,
                        UserId = payment.Payment.UserId,
                        Amount = payment.Payment.Amount,
                        PaymentUrl = payment.Payment.PaymentUrl,
                        PaymentDate = payment.Payment.PaymentDate,
                        Status = payment.Payment.Status,
                        TransactionId = payment.Payment.TransactionId
                    }
            };
        }
    }
}
