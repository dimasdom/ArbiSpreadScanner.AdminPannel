using ArbiScannerAdminPanel.Abstractions.Interfaces.Services;
using ArbiScannerAdminPanel.Domain.Models;
using FluentResults;
using Microsoft.AspNetCore.Mvc;
using ArbiScannerWeb.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authorization;
using System.Runtime.CompilerServices;
using ArbiScannerAdminPanel.Domain.Models.DTOs;

namespace ArbiScannerAdminPanel.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/{controller}")]
    public class SubscriptionsController : ControllerBase
    {
        private readonly ISubscriptionService _subscriptionService;
        public SubscriptionsController(ISubscriptionService subscriptionService)
        {
            _subscriptionService = subscriptionService;
        }

        [HttpGet("GetAllSubscriptions")]
        public async Task<ActionResult<Result<List<SubscriptionModel>>>> GetAllSubscriptions([FromQuery] int page)
        {
            var result = (await _subscriptionService.GetAllSubscriptions(page)).ToSerializable();
            return result;
        }

        [HttpGet("GetSubscriptionById")]
        public async Task<ActionResult<Result<SubscriptionModel>>> GetSubscriptionById([FromQuery] int id)
        {
            var result = (await _subscriptionService.GetSubscriptionById(id)).ToSerializable();
            return result;
        }
        [Authorize(Roles = "Administrator")]
        [HttpPost("CreateSubscription")]
        public async Task<ActionResult<Result>> CreateSubscription([FromBody] SubscriptionModel subscriptionModel)
        {
            var result = await _subscriptionService.CreateSubscription(subscriptionModel);
            return result.ToSerializable();
        }
        [Authorize(Roles = "Administrator")]
        [HttpPost("UpdateSubscription")]
        public async Task<ActionResult<Result>> UpdateSubscription([FromBody] SubscriptionModel subscriptionModel)
        {
            var result = await _subscriptionService.UpdateSubscription(subscriptionModel);
            return result.ToSerializable();
        }
        [Authorize(Roles = "Administrator")]
        [HttpDelete("DeleteSubscriptionsById")]
        public async Task<ActionResult<Result>> DeleteSubscriptionById([FromBody] List<int> ids)
        {
            var result = await _subscriptionService.DeleteSubscriptionsById(ids);
            return result.ToSerializable();
        }

        [HttpGet("GetUserSubscriptionByUserId")]
        public async Task<ActionResult<Result<UserSubscriptionModel>>> GetUserSubscriptionByUserId([FromQuery] string userId)
        {
            var result = (await _subscriptionService.GetUserSubscriptionByUserId(userId)).ToSerializable();
            return result;
        }

        [HttpPost("CreateUserSubscription")]
        public async Task<ActionResult<Result<UserSubscriptionModel>>> CreateUserSubscription([FromBody] UserSubscriptionCreateDTO userSubscriptionCreateDTO)
        {
            var result = (await _subscriptionService.CreateUserSubscription(userSubscriptionCreateDTO)).ToSerializable();
            return result;
        }

        [HttpGet("GetAllUserSubscriptions")]
        public async Task<ActionResult<Result<List<UserSubscriptionRowDTO>>>> GetAllUserSubscriptions([FromQuery] int page)
        {
            var result = (await _subscriptionService.GetAllUserSubscriptions(page)).ToSerializable();
            return result;
        }

        [HttpPost("UpdateUserSubscription")]
        public async Task<ActionResult<Result>> UpdateUserSubscription([FromBody] UserSubscriptionModel userSubscriptionModel)
        {
            var result = await _subscriptionService.UpdateUserSubscription(userSubscriptionModel);
            return result.ToSerializable();
        }

        [HttpDelete("DeleteUserSubscriptionsById")]
        public async Task<ActionResult<Result>> DeleteUserSubscriptionsById([FromBody] List<int> ids)
        {
            var result = await _subscriptionService.DeleteUserSubscriptionsById(ids);
            return result.ToSerializable();
        }

        [HttpGet("GetUserSubscriptionById")]
        public async Task<ActionResult<Result<UserSubscriptionModel>>> GetUserSubscriptionById([FromQuery] int id)
        {
            var result = (await _subscriptionService.GetUserSubscriptionById(id)).ToSerializable();
            return result;
        }
    }
}
