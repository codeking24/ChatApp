using Microsoft.AspNetCore.SignalR;
using MongoDbTutorial.Models;
using MongoDbTutorial.Models.Users;
using MongoDbTutorial.Services;
using System.Collections.Concurrent;

namespace MongoDbTutorial.Hubs
{
    public class ChatHub : Hub
    {
        private readonly PushSubscriptionService _pushService;
        private readonly PushService _pushNotifier;
        private readonly ChatService _chatService;
        private readonly UserService _userService;
        private static readonly ConcurrentDictionary<string, string> ConnectedUsers = new();

        public ChatHub(ChatService chatService, PushSubscriptionService pushService, PushService pushNotifier, UserService userService)
        {
            _chatService = chatService;
            _pushService = pushService;
            _pushNotifier = pushNotifier;
            _userService = userService;
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

        public async Task SendFollowRequest(string followerId, string followingId)
        {
           
            await _userService.FollowUserAsync(followerId, followingId);
            var pendingRequests = await _userService.GetPendingFollowRequests(followingId);
            // Send to specific user
            await Clients.User(followingId).SendAsync("ReceiveFollowRequest", pendingRequests);
        }

        public async Task SendFollowRequestNotification(string targetUserId)
        {
            var pendingRequests = await _userService.GetPendingFollowRequests(targetUserId);
            await Clients.User(targetUserId).SendAsync("ReceiveFollowRequest", pendingRequests);
        }

        public async Task NotifyPendingFollowRequests(string userId)
        {
            var pendingRequests = await _userService.GetPendingFollowRequests(userId);
            await Clients.User(userId).SendAsync("ReceiveFollowRequest", pendingRequests);
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
