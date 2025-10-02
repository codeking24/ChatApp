using Microsoft.AspNetCore.Mvc;
using MongoDbTutorial.Models.Subscription;
using MongoDbTutorial.Services;

namespace MongoDbTutorial.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PushSubscriptionController : Controller
    {
        private readonly PushSubscriptionService _pushService;
        public PushSubscriptionController(PushSubscriptionService pushService)
        {
            _pushService = pushService;
        }

        [HttpPost("save")]
        public async Task<IActionResult> SaveSubscription([FromBody] PushSubscriptionDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.UserId) || string.IsNullOrEmpty(dto.Endpoint))
            {
                return BadRequest("Invalid subscription data");
            }

            var subscription = new PushSubscriptionModel
            {
                UserId = dto.UserId,
                Endpoint = dto.Endpoint,
                P256DH = dto.P256DH,
                Auth = dto.Auth
            };

            await _pushService.SaveSubscriptionAsync(subscription);
            return Ok(new { Success = true });
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetSubscriptions(string userId)
        {
            var subscriptions = await _pushService.GetAllUserSubscriptionsAsync(userId);
            return Ok(subscriptions);
        }
    }
}
