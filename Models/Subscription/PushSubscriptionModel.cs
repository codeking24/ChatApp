using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDbTutorial.Models.Subscription
{
    public class PushSubscriptionModel
    {
        [BsonId] // MongoDB document ID
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("userId")]
        public string UserId { get; set; }

        [BsonElement("endpoint")]
        public string Endpoint { get; set; }

        [BsonElement("p256dh")]
        public string P256DH { get; set; }

        [BsonElement("auth")]
        public string Auth { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
