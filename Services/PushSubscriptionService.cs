using MongoDB.Driver;
using MongoDbTutorial.Models.Subscription;

namespace MongoDbTutorial.Services
{
    public class PushSubscriptionService
    {
        private readonly IMongoCollection<PushSubscriptionModel> _subscriptions;
        public PushSubscriptionService(IMongoDatabase mongoDatabase)
        {
            _subscriptions = mongoDatabase.GetCollection<PushSubscriptionModel>("pushSubscription");
        }
        public async Task SaveSubscriptionAsync(PushSubscriptionModel subscription)
        {
            // Check if subscription already exists for user & endpoint
            var existing = await _subscriptions.Find(s =>
                s.UserId == subscription.UserId && s.Endpoint == subscription.Endpoint).FirstOrDefaultAsync();

            if (existing == null)
            {
                await _subscriptions.InsertOneAsync(subscription);
            }
        }

        public async Task<PushSubscriptionModel> GetUserSubscriptionAsync(string userId)
        {
            return await _subscriptions.Find(s => s.UserId == userId).FirstOrDefaultAsync();
        }

        public async Task<List<PushSubscriptionModel>> GetAllUserSubscriptionsAsync(string userId)
        {
            return await _subscriptions.Find(s => s.UserId == userId).ToListAsync();
        }
    }
}
