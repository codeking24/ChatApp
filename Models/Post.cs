using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDbTutorial.Models.Users;

namespace MongoDbTutorial.Models
{
    public class Post
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string UserId { get; set; }
        public string ImageUrl { get; set; }
        public string Caption { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public List<string> Tags { get; set; } = new List<string>();

        [BsonIgnore]
        public int LikesCount { get; set; }
        [BsonIgnore]
        public int CommentsCount { get; set; }
        [BsonIgnore]
        public User User { get; set; }
    }
}
