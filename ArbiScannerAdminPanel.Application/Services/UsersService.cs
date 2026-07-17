using ArbiScannerAdminPanel.Abstractions.Interfaces.Services;
using ArbiScannerAdminPanel.Abstractions.Interfaces.Repositories;
using ArbiScannerAdminPanel.Domain.Models;
using ArbiScannerAdminPanel.Domain.Models.DTOs;
using ArbiScannerWeb.Infrastructure.Extensions;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace ArbiScannerAdminPanel.Application.Services
{
    public class UsersService : IUsersService
    {
        private readonly IAdminUsersRepository _adminUsersRepository;
        private readonly IWebAppUserRepository _webAppUserRepository;
        private readonly ILogger<UsersService> _logger;

        public UsersService(IAdminUsersRepository adminUsersRepository, IWebAppUserRepository webAppUserRepository, ILogger<UsersService> logger)
        {
            _adminUsersRepository = adminUsersRepository;
            _webAppUserRepository = webAppUserRepository;
            _logger = logger;
        }

        public async Task<Result> DeleteClientUser(string id)
        {
            await _webAppUserRepository.DeleteUser(id);
            return Result.Ok();
        }

        public async Task<Result> DeleteClientUsers(List<string> ids)
        {
            await _webAppUserRepository.DeleteUsers(ids);
            return Result.Ok();
        }

        public async Task<Result<ClientAccountDTO>> GetClientUserById(string id)
        {
            var user = await _webAppUserRepository.GetById(id);
            if (user == null)
            {
                _logger.LogWarning("GetClientUserById failed: user {UserId} not found", id);
                return Result.Fail(TypedErrors.NotFound("User not found"));
            }

            var userSubscription = await _adminUsersRepository.GetUserSubscriptionPaymentByUserId(id);
            var userPayments = await _adminUsersRepository.GetPaymentsByUserId(id);
            var clientAccountDTO = new ClientAccountDTO
            {
                Id = user.Id,
                UserMail = user.Email ?? string.Empty,
                UserName = user.UserName ?? string.Empty,
                Subscription = userSubscription,
                Payments = userPayments
            };
            return Result.Ok(clientAccountDTO);
        }

        public async Task<Result<List<ClientAccountTableRowDTO>>> GetClientUsers(int page = 1)
        {
            page = page < 1 ? 1 : page;
            var users = await _webAppUserRepository.GetUsers(page, 20);

            var clientAccountTableRowDTOs = new List<ClientAccountTableRowDTO>();
            foreach (var user in users)
            {
                var userSubscription = await _adminUsersRepository.GetUserSubscriptionByUserId(user.Id);
                clientAccountTableRowDTOs.Add(new ClientAccountTableRowDTO
                {
                    Id = user.Id,
                    UserMail = user.Email ?? string.Empty,
                    IsActiveSubscription = userSubscription != null && userSubscription.EndDate > DateTime.UtcNow,
                    SubscriptionStartDate = userSubscription?.StartDate,
                    SubscriptionEndDate = userSubscription?.EndDate
                });
            }
            return Result.Ok(clientAccountTableRowDTOs);
        }

        public async Task<Result<List<ClientAccountTableRowDTO>>> GetUsersByEmail(string email)
        {
            var users = await _webAppUserRepository.SearchByEmail(email);

            var clientAccountTableRowDTOs = new List<ClientAccountTableRowDTO>();
            foreach (var user in users)
            {
                var userSubscription = await _adminUsersRepository.GetUserSubscriptionByUserId(user.Id);
                clientAccountTableRowDTOs.Add(new ClientAccountTableRowDTO
                {
                    Id = user.Id,
                    UserMail = user.Email ?? string.Empty,
                    IsActiveSubscription = userSubscription != null && userSubscription.EndDate > DateTime.UtcNow,
                    SubscriptionStartDate = userSubscription?.StartDate,
                    SubscriptionEndDate = userSubscription?.EndDate
                });
            }
            return Result.Ok(clientAccountTableRowDTOs);
        }

        public async Task<Result<UserSubscriptionModel>> GetUserSubscriptionByUserId(string userId)
        {
            var userSubscription = await _adminUsersRepository.GetUserSubscriptionByUserId(userId);
            if (userSubscription is null)
            {
                _logger.LogWarning("GetUserSubscriptionByUserId failed: no subscription for user {UserId}", userId);
                return Result.Fail<UserSubscriptionModel>(TypedErrors.NotFound("User subscription not found"));
            }
            return Result.Ok(userSubscription);
        }

        public async Task<Result> UpdateClientUser(ClientAccountDTO clientAccountDTO)
        {
            await _webAppUserRepository.UpdateUser(clientAccountDTO.Id, clientAccountDTO.UserMail, clientAccountDTO.UserName);
            return Result.Ok();
        }
    }
}
