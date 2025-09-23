using MongoDB.Driver;
using MongoDbTutorial.Models.Users;
using System.Security.Cryptography;
using System.Text;

namespace MongoDbTutorial.Services
{
    public class UserService
    {
        private readonly IMongoCollection<User> _users;

        public UserService(IMongoDatabase database)
        {
            _users = database.GetCollection<User>("users");
        }
        public List<User> GetAllExcept(string? userId)
        {
            var filter = userId == null ? Builders<User>.Filter.Empty : Builders<User>.Filter.Ne(u => u.Id, userId);
            return _users.Find(filter).ToList();
        }
        public User? GetById(string id) => _users.Find(u => u.Id == id).FirstOrDefault();
        public User? GetByUsername(string username) => _users.Find(u => u.Username == username).FirstOrDefault();
        public User Create(User user)
        {
            user.PasswordHash = Hash(user.PasswordHash);
            _users.InsertOne(user);
            return user;
        }
        public bool ValidateCredentials(string username, string password)
        {
            var user = GetByUsername(username);
            if (user == null) return false;
            return user.PasswordHash == Hash(password);
        }
        private static string Hash(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(bytes);
        }
    }
}
