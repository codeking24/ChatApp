namespace MongoDbTutorial.Models.Message
{
    public class MessageWithUserNames
    {
        public string Id { get; set; }
        public string From { get; set; }
        public string FromName { get; set; }
        public string To { get; set; }
        public string ToName { get; set; }
        public string Message { get; set; }
        public DateTime SentAt { get; set; }
        public bool Read { get; set; }
        public bool OneTime { get; set; }
    }
}
