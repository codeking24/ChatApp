namespace MongoDbTutorial.Models.Subscription
{
    public class PushSubscriptionDto
    {
        public string UserId { get; set; }
        public string Endpoint { get; set; }
        public string P256DH { get; set; }
        public string Auth { get; set; }
    }
}
