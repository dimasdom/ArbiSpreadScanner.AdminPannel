using ArbiScannerAdminPanel.Abstractions.Interfaces.Services;
using ArbiScannerAdminPanel.Abstractions.Interfaces.Repositories;
using ArbiScannerAdminPanel.Domain.Models;
using ArbiScannerAdminPanel.Domain.Models.DTOs;
using FluentResults;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            return await _webAppUserRepository.DeleteUser(id);
        }

        public async Task<Result> DeleteClientUsers(List<string> ids)
        {
            return await _webAppUserRepository.DeleteUsers(ids);
        }

        public async Task<Result<ClientAccountDTO>> GetClientUserById(string id)
        {
            var userResult = await _webAppUserRepository.GetById(id);
            if (userResult.IsFailed)
            {
                return Result.Fail<ClientAccountDTO>(userResult.Errors);
            }

            var user = userResult.Value;
            if (user == null)
            {
                _logger.LogWarning("GetClientUserById failed: user {UserId} not found", id);
                return Result.Fail("User not found");
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
            var usersResult = await _webAppUserRepository.GetUsers(page, 20);
            if (usersResult.IsFailed)
            {
                return Result.Fail<List<ClientAccountTableRowDTO>>(usersResult.Errors);
            }

            var users = usersResult.Value;
            var clientAccountTableRowDTOs = new List<ClientAccountTableRowDTO>();
            foreach (var user in users)
            {
                var userSubscription = await _adminUsersRepository.GetUserSubscriptionByUserId(user.Id);
                var clientAccountTableRowDTO = new ClientAccountTableRowDTO
                {
                    Id = user.Id,
                    UserMail = user.Email ?? string.Empty,
                    IsActiveSubscription = userSubscription != null && userSubscription.EndDate > DateTime.UtcNow,
                    SubscriptionStartDate = userSubscription?.StartDate,
                    SubscriptionEndDate = userSubscription?.EndDate
                };
                clientAccountTableRowDTOs.Add(clientAccountTableRowDTO);
            }
            return Result.Ok(clientAccountTableRowDTOs);
        }

        public async Task<Result<List<ClientAccountTableRowDTO>>> GetUsersByEmail(string email)
        {
            var usersResult = await _webAppUserRepository.SearchByEmail(email);
            if (usersResult.IsFailed)
            {
                return Result.Fail<List<ClientAccountTableRowDTO>>(usersResult.Errors);
            }

            var users = usersResult.Value;
            var clientAccountTableRowDTOs = new List<ClientAccountTableRowDTO>();
            foreach (var user in users)
            {
                var userSubscription = await _adminUsersRepository.GetUserSubscriptionByUserId(user.Id);
                var clientAccountTableRowDTO = new ClientAccountTableRowDTO
                {
                    Id = user.Id,
                    UserMail = user.Email ?? string.Empty,
                    IsActiveSubscription = userSubscription != null && userSubscription.EndDate > DateTime.UtcNow,
                    SubscriptionStartDate = userSubscription?.StartDate,
                    SubscriptionEndDate = userSubscription?.EndDate
                };
                clientAccountTableRowDTOs.Add(clientAccountTableRowDTO);
            }
            return Result.Ok(clientAccountTableRowDTOs);
        }

        public async Task<Result<UserSubscriptionModel>> GetUserSubscriptionByUserId(string userId)
        {
            var userSubscription = await _adminUsersRepository.GetUserSubscriptionByUserId(userId);
            if (userSubscription is null)
            {
                _logger.LogWarning("GetUserSubscriptionByUserId failed: no subscription for user {UserId}", userId);
                return Result.Fail<UserSubscriptionModel>("User subscription not found");
            }
            return Result.Ok(userSubscription);
        }

        public async Task<Result> UpdateClientUser(ClientAccountDTO clientAccountDTO)
        {
            return await _webAppUserRepository.UpdateUser(clientAccountDTO.Id, clientAccountDTO.UserMail, clientAccountDTO.UserName);
        }
    }
}
