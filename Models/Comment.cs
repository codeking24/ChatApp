using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDbTutorial.Models.Users;

namespace MongoDbTutorial.Models
{
    public class Comment
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string PostId { get; set; }
        public string UserId { get; set; }
        public string Text { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonIgnore]
        public User User { get; set; }
    }
}
