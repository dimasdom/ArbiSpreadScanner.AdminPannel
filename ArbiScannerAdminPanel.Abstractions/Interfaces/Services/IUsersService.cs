using ArbiScannerAdminPanel.Domain.Models;
using ArbiScannerAdminPanel.Domain.Models.DTOs;
using FluentResults;

namespace ArbiScannerAdminPanel.Abstractions.Interfaces.Services
{
    public interface IUsersService
    {
        Task<Result<List<ClientAccountTableRowDTO>>> GetClientUsers(int page = 1);
        Task<Result<ClientAccountDTO>> GetClientUserById(string id);
        Task<Result> UpdateClientUser(ClientAccountDTO clientAccountDTO);
        Task<Result> DeleteClientUsers(List<string> ids);
        Task<Result<List<ClientAccountTableRowDTO>>> GetUsersByEmail(string email);
        Task<Result<UserSubscriptionModel>> GetUserSubscriptionByUserId(string userId);
    }
}
