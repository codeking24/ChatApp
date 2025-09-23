using Microsoft.AspNetCore.SignalR;
using MongoDbTutorial.Services;

namespace MongoDbTutorial.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ChatService _chatService;

        public ChatHub(ChatService chatService)
        {
            _chatService = chatService;
        }
        public async Task SendMessage(string fromUserId, string toUserId, string message, bool? oneTime)
        {
            var msg = await _chatService.SendMessage(fromUserId, toUserId, message, oneTime);
            await Clients.User(toUserId).SendAsync("ReceiveMessage", msg);
            await Clients.Caller.SendAsync("MessageSent", msg);

            var unread = _chatService.GetUnreadCount(toUserId);
            await Clients.User(toUserId).SendAsync("UnreadCount", unread);
        }

        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
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
    }
    public class CustomUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            return connection.GetHttpContext()?.Session.GetString("UserId");
        }
    }
}
