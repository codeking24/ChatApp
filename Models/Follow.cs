using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDbTutorial.Models
{
    public class Follow
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string FollowerId { get; set; } // Who is following
        public string FollowingId { get; set; } // Who is being followed
        public bool IsFollowing { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
