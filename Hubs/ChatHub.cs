using Microsoft.AspNetCore.SignalR;
using MongoDbTutorial.Services;
using System.Collections.Concurrent;

namespace MongoDbTutorial.Hubs
{
    public class ChatHub : Hub
    {
        private readonly PushSubscriptionService _pushService;
        private readonly PushService _pushNotifier;
        private readonly ChatService _chatService;        
        private static readonly ConcurrentDictionary<string, string> ConnectedUsers = new();

        public ChatHub(ChatService chatService, PushSubscriptionService pushService, PushService pushNotifier)
        {
            _chatService = chatService;
            _pushService = pushService;
            _pushNotifier = pushNotifier;
        }
        public async Task SendMessage(string fromUserId, string toUserId, string message, bool? oneTime)
        {
            var msg = await _chatService.SendMessage(fromUserId, toUserId, message, oneTime);
            await Clients.User(toUserId).SendAsync("ReceiveMessage", msg);
            await Clients.Caller.SendAsync("MessageSent", msg);

            var unread = _chatService.GetUnreadCount(toUserId);
            await Clients.User(toUserId).SendAsync("UnreadCount", unread);

            var userIsConnected = IsUserConnected(toUserId);
            if (!userIsConnected)
            {
                var subscriptions = await _pushService.GetAllUserSubscriptionsAsync(toUserId);
                foreach (var sub in subscriptions)
                {
                    _pushNotifier.SendPushNotification(sub, message);
                }
            }
        }

        public override Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            ConnectedUsers[userId] = Context.ConnectionId;
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            ConnectedUsers.TryRemove(userId, out _);
            return base.OnDisconnectedAsync(exception);
        }

        public Task Register(string userId)
        {
            return Groups.AddToGroupAsync(Context.ConnectionId, userId);
        }

        public Task Unregister(string userId)
        {
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
        }

        public async Task Typing(string toUserId)
        {
            var fromUserId = Context.UserIdentifier;
            await Clients.User(toUserId).SendAsync("UserTyping", fromUserId, true);
            _ = StopTypingAfterDelay(fromUserId, toUserId, 2000);
        }

        private async Task StopTypingAfterDelay(string fromUserId, string toUserId, int delayMs)
        {
            await Task.Delay(delayMs);
            await StopTyping(toUserId);
        }
        public async Task StopTyping(string toUserId)
        {
            var fromUserId = Context.UserIdentifier;
            await Clients.User(toUserId).SendAsync("UserTyping", fromUserId, false);
        }
        private bool IsUserConnected(string userId) => ConnectedUsers.ContainsKey(userId);
    }
    public class CustomUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            return connection.GetHttpContext()?.Session.GetString("UserId");
        }
    }
}
