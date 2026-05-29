using ArbiScannerAdminPanel.Abstractions.Interfaces.Services;
using ArbiScannerAdminPanel.Domain.Models;
using FluentResults;
using Microsoft.AspNetCore.Mvc;
using ArbiScannerWeb.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authorization;
using ArbiScannerAdminPanel.Domain.Models.DTOs;
using System.Linq;
namespace ArbiScannerAdminPanel.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/{controller}")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentsService _paymentsService;

        public PaymentsController(IPaymentsService paymentsService)
        {
            _paymentsService = paymentsService;
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
            return result;
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
            return result;
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
