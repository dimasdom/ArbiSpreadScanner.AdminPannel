using ArbiScannerAdminPanel.Abstractions.Interfaces.Services;
using ArbiScannerAdminPanel.Abstractions.Interfaces.Repositories;
using ArbiScannerAdminPanel.Domain.Models;
using ArbiScannerAdminPanel.Domain.Models.DTOs;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace ArbiScannerAdminPanel.Application.Services
{
    public class PaymentsService : IPaymentsService
    {
        private readonly IPaymentsRepository _paymentsRepository;
        private readonly ISubscriptionsRepository _subscriptionsRepository;
        private readonly IWebAppUserRepository _webAppUserRepository;
        private readonly ISubscriptionService _subscriptionService;
        private readonly IOxaPayService _oxaPayService;
        private readonly ILogger<PaymentsService> _logger;

        public PaymentsService(
            IPaymentsRepository paymentsRepository,
            ISubscriptionsRepository subscriptionsRepository,
            IWebAppUserRepository webAppUserRepository,
            ISubscriptionService subscriptionService,
            IOxaPayService oxaPayService,
            ILogger<PaymentsService> logger)
        {
            _paymentsRepository = paymentsRepository;
            _subscriptionsRepository = subscriptionsRepository;
            _webAppUserRepository = webAppUserRepository;
            _subscriptionService = subscriptionService;
            _oxaPayService = oxaPayService;
            _logger = logger;
        }

        public async Task<Result<UserSubscriptionPayment>> AcceptPayment(string TransactionId)
        {
            var payment = await _paymentsRepository.GetUserSubscriptionPaymentByTransactionId(TransactionId, forUpdate: true);

            if (payment == null && int.TryParse(TransactionId, out var paymentId))
            {
                payment = await _paymentsRepository.GetUserSubscriptionPaymentByPaymentId(paymentId, forUpdate: true);
            }

            if (payment == null)
            {
                _logger.LogWarning("AcceptPayment failed: payment not found for TransactionId {TransactionId}", TransactionId);
                return Result.Fail<UserSubscriptionPayment>("Payment not found");
            }

            if (payment.Payment == null)
            {
                _logger.LogWarning("AcceptPayment failed: payment model not found for TransactionId {TransactionId}", TransactionId);
                return Result.Fail<UserSubscriptionPayment>("Payment model not found");
            }

            if (payment.Payment.Status == PaymentStatus.Completed)
            {
                return Result.Ok(payment);
            }

            payment.Payment.Status = PaymentStatus.Completed;
            await _paymentsRepository.SaveChangesAsync();

            var res = await _subscriptionService.AssignSubscriptionToUser(payment);
            if (res.IsFailed)
            {
                _logger.LogError("AcceptPayment: subscription assignment failed for payment {PaymentId}", payment.Payment.Id);
                return Result.Fail<UserSubscriptionPayment>("Failed to assign subscription to user");
            }
            return Result.Ok(payment);
        }

        public async Task<Result> CancelPayment(int userSubscriptionPaymentId)
        {
            var userPayment = await _paymentsRepository.GetUserSubscriptionPaymentWithDetails(userSubscriptionPaymentId, forUpdate: true);
            if (userPayment?.Payment == null)
            {
                _logger.LogWarning("CancelPayment failed: payment {PaymentId} not found", userSubscriptionPaymentId);
                return Result.Fail("User payment not found");
            }
            userPayment.Payment.Status = PaymentStatus.Refunded;
            await _paymentsRepository.SaveChangesAsync();
            return Result.Ok();
        }

        public async Task<Result<OxaPayInvoiceResultDTO>> GenerateInvoice(int userSubscriptionPaymentId, OxaPayInvoiceCreateOptionsDTO? options = null)
        {
            var userPayment = await _paymentsRepository.GetUserSubscriptionPaymentWithDetails(userSubscriptionPaymentId, forUpdate: true);

            if (userPayment == null || userPayment.Payment == null)
            {
                _logger.LogWarning("GenerateInvoice failed: payment {PaymentId} not found", userSubscriptionPaymentId);
                return Result.Fail<OxaPayInvoiceResultDTO>("User payment not found");
            }

            if (userPayment.Payment.Status == PaymentStatus.Completed)
            {
                _logger.LogWarning("GenerateInvoice failed: payment {PaymentId} is already completed", userSubscriptionPaymentId);
                return Result.Fail<OxaPayInvoiceResultDTO>("Invoice cannot be created for completed payment");
            }

            var userEmail = options?.Email;
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                var user = await _webAppUserRepository.GetById(userPayment.UserId);
                if (user is not null)
                {
                    userEmail = user.Email;
                }
            }

            var invoiceResult = await _oxaPayService.GenerateInvoice(userPayment, userEmail, options);
            if (invoiceResult.IsFailed)
            {
                return Result.Fail<OxaPayInvoiceResultDTO>(invoiceResult.Errors);
            }

            var invoice = invoiceResult.Value;

            userPayment.Payment.TransactionId = invoice.TrackId;
            userPayment.Payment.PaymentUrl = invoice.PaymentUrl;
            userPayment.ExpirationDate = DateTimeOffset.FromUnixTimeSeconds(invoice.ExpiredAt).UtcDateTime;
            await _paymentsRepository.SaveChangesAsync();

            return Result.Ok(invoice);
        }

        public async Task<Result<UserSubscriptionPayment>> CreatePaymentForUser(UserSubscriptionPayment payment)
        {
            var subscriptionModel = await _subscriptionsRepository.GetSubscriptionById(payment.SubscriptionId);
            if (subscriptionModel == null)
            {
                _logger.LogWarning("CreatePaymentForUser failed: subscription {SubscriptionId} not found", payment.SubscriptionId);
                return Result.Fail<UserSubscriptionPayment>("Subscription not found");
            }
            var activePayment = await GetActivePaymentForUser(payment.UserId);
            if(activePayment.IsSuccess && activePayment.Value != null)
            {
                return Result.Ok(activePayment.Value);
            }
            
            var createdPayment = new PaymentModel
            {
                UserId = payment.UserId,
                Amount = subscriptionModel.Price,
                PaymentDate = DateTime.UtcNow,
                TransactionId = string.Empty
            };
            var UserSubscriptionPayment = new UserSubscriptionPayment
            {
                UserId = payment.UserId,
                SubscriptionId = payment.SubscriptionId,
                Payment = createdPayment,
                ExpirationDate = DateTime.UtcNow.AddMinutes(30) // Set expiration date to 30 minutes from now
            };
            await _paymentsRepository.AddUserSubscriptionPayment(UserSubscriptionPayment);
            await _paymentsRepository.SaveChangesAsync();
            var invoice = await GenerateInvoice(UserSubscriptionPayment.Id);
            if(invoice.IsFailed)
            {
                _logger.LogError("CreatePaymentForUser: invoice generation failed for user {UserId}, payment {PaymentId}",
                    payment.UserId, UserSubscriptionPayment.Id);
                return Result.Fail<UserSubscriptionPayment>("Failed to generate invoice for the payment");
            }
            return Result.Ok(UserSubscriptionPayment);
        }

        public async Task<Result<UserSubscriptionPayment>> GetActivePaymentForUser(string userId)
        {
            var activePayment = await _paymentsRepository.GetActiveUserPayment(userId, DateTime.UtcNow, forUpdate: true);
            if (activePayment == null)
            {
                _logger.LogWarning("GetActivePaymentForUser: no active payment for user {UserId}", userId);
                return Result.Fail<UserSubscriptionPayment>("No active payment found for user");
            }
            if (string.IsNullOrWhiteSpace(activePayment.Payment?.TransactionId))
            {
                return Result.Ok(activePayment);
            }

            var invoiceStatusResult = await GetInvoiceStatus(activePayment.Payment.TransactionId);
            if (invoiceStatusResult.IsSuccess){
                var invoiceStatus = invoiceStatusResult.Value;
                if (invoiceStatus.LocalStatus == PaymentStatus.Completed)
                {
                    var res = await AcceptPayment(activePayment.Payment.TransactionId);
                    if (res.IsFailed)
                    {
                        _logger.LogError("GetActivePaymentForUser: subscription assignment failed for user {UserId}, transaction {TransactionId}",
                            userId, activePayment.Payment.TransactionId);
                        return Result.Fail<UserSubscriptionPayment>("Payment is completed, but subscription assignment failed");
                    }
                }
                else if (invoiceStatus.LocalStatus == PaymentStatus.Failed || invoiceStatus.LocalStatus == PaymentStatus.Refunded)
                {
                    activePayment.Payment.Status = invoiceStatus.LocalStatus;
                    await _paymentsRepository.SaveChangesAsync();
                }
            }
            return Result.Ok(activePayment);
        }

        public async Task<Result<List<PaymentModel>>> GetAllPayments(int page = 1)
        {
            return Result.Ok<List<PaymentModel>>(await _paymentsRepository.GetAllPayments(page));
        }

        public async Task<Result<PaymentModel>> GetPaymentById(int id)
        {
            var payment =  await _paymentsRepository.GetPaymentById(id);
            if (payment == null)
            {
                _logger.LogWarning("GetPaymentById failed: payment {PaymentId} not found", id);
                return Result.Fail<PaymentModel>("Payment not found");
            }
            return Result.Ok<PaymentModel>(payment);
        }

        public async Task<Result<PaymentResultDTO>> GetPaymentDTOById(int id)
        {
            var paymentResult = await GetPaymentById(id);
            if (paymentResult.IsFailed)
            {
                return Result.Fail<PaymentResultDTO>(paymentResult.Errors);
            }

            var payment = paymentResult.Value;
            var user = await _webAppUserRepository.GetById(payment.UserId);

            if (user == null)
            {
                _logger.LogWarning("GetPaymentDTOById failed: user {UserId} not found for payment {PaymentId}", payment.UserId, id);
                return Result.Fail<PaymentResultDTO>("User not found");
            }

            var paymentDTO = new PaymentResultDTO
            {
                Id = payment.Id,
                UserId = payment.UserId,
                UserEmail = user.Email ?? string.Empty,
                Amount = payment.Amount,
                PaymentUrl = payment.PaymentUrl,
                PaymentDate = payment.PaymentDate,
                Status = payment.Status,
                TransactionId = payment.TransactionId
            };

            return Result.Ok(paymentDTO);
        }

        public async Task<Result<List<UserSubscriptionPayment>>> GetPaymentsForUser(string userId)
        {
            var user = await _webAppUserRepository.GetById(userId);
            if (user == null)
            {
                return Result.Fail<List<UserSubscriptionPayment>>("User not found");
            }
            var payments = await _paymentsRepository.GetPaymentsForUser(userId);
            return Result.Ok<List<UserSubscriptionPayment>>(payments);
        }

        public async Task<Result<UserSubscriptionPayment>> GetUserPaymentByIdAsync(int paymentId)
        {
            var userPayment = await _paymentsRepository.GetUserSubscriptionPaymentWithDetails(paymentId, forUpdate: true);
            if (userPayment == null || userPayment.Payment == null)
            {
                _logger.LogWarning("GetUserPaymentByIdAsync failed: payment {PaymentId} not found", paymentId);
                return Result.Fail<UserSubscriptionPayment>("User payment not found");
            }
            var invoiceStatusResult = await GetInvoiceStatus(userPayment.Payment.TransactionId);
            if (invoiceStatusResult.IsSuccess){
                var invoiceStatus = invoiceStatusResult.Value;
                if (invoiceStatus.LocalStatus == PaymentStatus.Completed)
                {
                    var res = await AcceptPayment(userPayment.Payment.TransactionId);
                    if (res.IsFailed)
                    {
                        _logger.LogError("GetUserPaymentByIdAsync: subscription assignment failed for payment {PaymentId}, transaction {TransactionId}",
                            paymentId, userPayment.Payment.TransactionId);
                        return Result.Fail<UserSubscriptionPayment>("Payment is completed, but subscription assignment failed");
                    }
                }
                else if (invoiceStatus.LocalStatus == PaymentStatus.Failed || invoiceStatus.LocalStatus == PaymentStatus.Refunded)
                {
                    userPayment.Payment.Status = invoiceStatus.LocalStatus;
                    await _paymentsRepository.SaveChangesAsync();
                }
            }
            return Result.Ok(userPayment);
        }

        public async Task<Result> RemovePayment(int id)
        {
            var payment = await _paymentsRepository.GetPaymentById(id);
            if (payment == null)
            {
                _logger.LogWarning("RemovePayment failed: payment {PaymentId} not found", id);
                return Result.Fail("Payment not found");
            }
            _paymentsRepository.RemovePayment(payment);
            await _paymentsRepository.SaveChangesAsync();
            return Result.Ok();
        }

        public async Task<Result> RemovePayments(List<int> id)
        {
            var payments = await _paymentsRepository.GetPaymentsByIds(id);
            _paymentsRepository.RemovePayments(payments);
            await _paymentsRepository.SaveChangesAsync();
            return Result.Ok();
        }

        public async Task<Result<OxaPayPaymentStatusDTO>> GetInvoiceStatus(string trackId)
        {
            if (string.IsNullOrWhiteSpace(trackId))
            {
                _logger.LogWarning("GetInvoiceStatus failed: trackId is empty");
                return Result.Fail<OxaPayPaymentStatusDTO>("TrackId is required");
            }

            var statusResult = await _oxaPayService.GetInvoiceStatus(trackId);
            if (statusResult.IsFailed)
            {
                return Result.Fail<OxaPayPaymentStatusDTO>(statusResult.Errors);
            }

            var statusDto = statusResult.Value;
            var localStatus = statusDto.LocalStatus;
            var localPayment = await _paymentsRepository.GetUserSubscriptionPaymentByTransactionId(trackId, forUpdate: true);

            if (localPayment?.Payment != null)
            {
                var currentStatus = localPayment.Payment.Status;
                if (currentStatus != localStatus)
                {
                    localPayment.Payment.Status = localStatus;
                    await _paymentsRepository.SaveChangesAsync();
                }

                if (localStatus == PaymentStatus.Completed && currentStatus != PaymentStatus.Completed)
                {
                    var assignResult = await _subscriptionService.AssignSubscriptionToUser(localPayment);
                    if (assignResult.IsFailed)
                    {
                        _logger.LogError("GetInvoiceStatus: subscription assignment failed for trackId {TrackId}", trackId);
                        return Result.Fail<OxaPayPaymentStatusDTO>("Payment is completed, but subscription assignment failed");
                    }
                }
            }

            statusDto.LocalStatus = localPayment?.Payment?.Status ?? localStatus;
            return Result.Ok(statusDto);
        }
    }
}
