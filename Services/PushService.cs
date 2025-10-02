using MongoDB.Driver;
using MongoDbTutorial.Models.Subscription;
using WebPush;

namespace MongoDbTutorial.Services
{
    public class PushService
    {
        //Public Key: BCuxfTxtbORt1GqvRvfvt994raDxJOPHLDOeeQFfaOGdhl1mj4zYIymUSUcN8lvSP_Yw2-PhSRXrjg0LTdAIyl4
        //Private Key: uWvdb7LJrqNqrBayEK93lisFOZokphI42nb2GEPRuHQ
        private readonly string publicKey = "BCuxfTxtbORt1GqvRvfvt994raDxJOPHLDOeeQFfaOGdhl1mj4zYIymUSUcN8lvSP_Yw2-PhSRXrjg0LTdAIyl4";
        private readonly string privateKey = "uWvdb7LJrqNqrBayEK93lisFOZokphI42nb2GEPRuHQ";
       
        public void SendPushNotification(PushSubscriptionModel subscription, string message)
        {
            var pushSubscription = new WebPush.PushSubscription(
                subscription.Endpoint,
                subscription.P256DH,
                subscription.Auth
            );

            var vapidDetails = new VapidDetails(
                "mailto:m.rao@lpcloudlab.com",
                publicKey,
                privateKey
            );

            var webPushClient = new WebPushClient();
            webPushClient.SendNotification(pushSubscription, message, vapidDetails);
        }

    }
}
