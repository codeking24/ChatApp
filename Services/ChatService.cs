using MongoDB.Driver;
using MongoDbTutorial.Models.Message;
using MongoDbTutorial.Models.Users;

namespace MongoDbTutorial.Services
{
    public class ChatService
    {
        private readonly IMongoCollection<ChatMessage> _messages;
        private readonly IMongoCollection<User> _users;
        public ChatService(IMongoDatabase database)
        {
            _messages = database.GetCollection<ChatMessage>("chatMessages");
            _users = database.GetCollection<User>("users");
        }

        public List<ChatMessage> GetConversation(string userA, string userB)
        {
            var filter = Builders<ChatMessage>.Filter.Or(
                    Builders<ChatMessage>.Filter.And(
                    Builders<ChatMessage>.Filter.Eq(m => m.From, userA),
                    Builders<ChatMessage>.Filter.Eq(m => m.To, userB),
                    Builders<ChatMessage>.Filter.Eq(m => m.OneTime, false)),
                    Builders<ChatMessage>.Filter.And(
                    Builders<ChatMessage>.Filter.Eq(m => m.From, userB),
                    Builders<ChatMessage>.Filter.Eq(m => m.To, userA),
                    Builders<ChatMessage>.Filter.Eq(m => m.OneTime, false))
            );
            return _messages.Find(filter).SortBy(m => m.SentAt).ToList();
        }

        public async Task<MessageWithUserNames> SendMessage(string from, string to, string text, bool? oneTime)
        {
            var msg = new ChatMessage
            {
                From = from,
                To = to,
                Message = text,
                SentAt = DateTime.UtcNow,
                Read = false,
                OneTime = oneTime,
            };

            await _messages.InsertOneAsync(msg);
            var fromUser = await _users.Find(u => u.Id == from).FirstOrDefaultAsync();
            var toUser = await _users.Find(u => u.Id == to).FirstOrDefaultAsync();

            return new MessageWithUserNames
            {
                Id = msg.Id.ToString(),
                From = msg.From,
                FromName = $"{fromUser?.FirstName} {fromUser?.LastName}".Trim(),
                To = msg.To,
                ToName = $"{toUser?.FirstName} {toUser?.LastName}".Trim(),
                Message = msg.Message,
                SentAt = msg.SentAt,
                Read = msg.Read,
                OneTime = (bool)msg.OneTime
            };
        }

        public void MarkAsRead(string from, string to)
        {
            var filter = Builders<ChatMessage>.Filter.Eq(m => m.From, from) &
             Builders<ChatMessage>.Filter.Eq(m => m.To, to) &
             Builders<ChatMessage>.Filter.Eq(m => m.Read, false);

            var update = Builders<ChatMessage>.Update.Set(m => m.Read, true);

            var result = _messages.UpdateMany(filter, update);
            var filterOneTime = Builders<ChatMessage>.Filter.Where(m => m.From == from && m.To == to && m.OneTime == true);
            _messages.DeleteMany(filterOneTime);

        }
        public long GetUnreadCount(string userId)
        {
            return _messages.CountDocuments(m => m.To == userId && m.Read == false);
        }
    }
}
