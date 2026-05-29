using ArbiScannerAdminPanel.Abstractions.Interfaces.Services;
using ArbiScannerAdminPanel.Domain.Models.DTOs;
using ArbiScannerAdminPanel.Domain.Models;
using FluentResults;
using Microsoft.AspNetCore.Mvc;
using ArbiScannerWeb.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authorization;

namespace ArbiScannerAdminPanel.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/{controller}")]
    public class UsersController : ControllerBase
    {
        private readonly IUsersService _usersService;
        public UsersController(IUsersService userService)
        {
            _usersService = userService;
        }

        [HttpGet("GetClientUsers")]
        public async Task<ActionResult<Result<List<ClientAccountTableRowDTO>>>> GetClientUsers([FromQuery]int page)
        {
            var result = (await _usersService.GetClientUsers(page)).ToSerializable();
            return result;
        }

        [HttpGet("GetClientUserById")]
        public async Task<ActionResult<Result<ClientAccountDTO>>> GetClientUserById([FromQuery] string id)
        {
            var result = (await _usersService.GetClientUserById(id)).ToSerializable();
            return result;
        }
        [Authorize(Roles = "Administrator")]
        [HttpPost("UpdateClientUser")]
        public async Task<ActionResult<Result>> UpdateClientUser([FromBody] ClientAccountDTO clientAccountDTO)
        {
            var result = await _usersService.UpdateClientUser(clientAccountDTO);
            return result;
        }
        [Authorize(Roles = "Administrator")]
        [HttpDelete("DeleteClientUsers")]
        public async Task<ActionResult<Result>> DeleteClientUsers([FromBody] List<string> ids)
        {
            var result = await _usersService.DeleteClientUsers(ids);
            return result;
        }

        [HttpGet("GetUserSubscriptionByUserId")]
        public async Task<ActionResult<Result<UserSubscriptionModel>>> GetUserSubscriptionByUserId([FromQuery] string userId)
        {
            var result = (await _usersService.GetUserSubscriptionByUserId(userId)).ToSerializable();
            return result;
        }

        [HttpGet("GetUsersByEmail")]
        public async Task<ActionResult<Result<List<ClientAccountTableRowDTO>>>> GetUsersByEmail([FromQuery] string email)
        {
            var result = (await _usersService.GetUsersByEmail(email)).ToSerializable();
            return result;
        }
    }
}
