using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDbTutorial.Models.Message
{
    public class ChatMessage
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }


        [BsonElement("from")]
        public string From { get; set; } = null!; // userId


        [BsonElement("to")]
        public string To { get; set; } = null!; // userId


        [BsonElement("message")]
        public string Message { get; set; } = null!;


        [BsonElement("sentAt")]
        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        [BsonElement("read")]
        public bool Read { get; set; } = false;

        [BsonElement("oneTime")]
        public bool? OneTime { get; set; } = false;

    }
}
