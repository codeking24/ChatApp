using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDbTutorial.Models.Users
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("firstName")]
        public string FirstName { get; set; } = null!;

        [BsonElement("lastName")]
        public string? LastName { get; set; }

        [BsonElement("email")]
        public string Email { get; set; } = null!;

        [BsonElement("username")]
        public string Username { get; set; } = null!;


        [BsonElement("passwordHash")]
        public string PasswordHash { get; set; } = null!;


        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


        [BsonElement("lastSeen")]
        public DateTime? LastSeen { get; set; }
    }
}
