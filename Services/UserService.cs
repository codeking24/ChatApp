using MongoDB.Bson;
using MongoDB.Driver;
using MongoDbTutorial.Models;
using MongoDbTutorial.Models.Users;
using System.Security.Cryptography;
using System.Text;

namespace MongoDbTutorial.Services
{
    public class UserService
    {
        private readonly IMongoCollection<User> _users;
        private readonly IMongoCollection<Follow> _follow;

        public UserService(IMongoDatabase database)
        {
            _users = database.GetCollection<User>("users");
            _follow = database.GetCollection<Follow>("follows");
        }

        public async Task FollowUserAsync(string followerId, string followeeId)
        {
            if (followerId == followeeId) return; // Prevent self-follow

            var exists = await _follow.Find(f =>
                f.FollowerId == followerId && f.FollowingId == followeeId).AnyAsync();

            if (!exists)
            {
                var follow = new Follow
                {
                    FollowerId = followerId,
                    FollowingId = followeeId                   
                };
                await _follow.InsertOneAsync(follow);
            }
        }

        public async Task UnfollowUserAsync(string followerId, string followeeId)
        {
            await _follow.DeleteOneAsync(f =>
                f.FollowerId == followerId && f.FollowingId == followeeId);
        }

        public async Task<List<string>> GetFollowersAsync(string userId)
        {
            return await _follow.Find(f => f.FollowingId == userId && f.IsFollowing)
                                .Project(f => f.FollowerId)
                                .ToListAsync();
        }

        public async Task<List<string>> GetFollowingAsync(string userId)
        {
            return await _follow.Find(f => f.FollowerId == userId && f.IsFollowing)
                                .Project(f => f.FollowingId)
                                .ToListAsync();
        }

        public async Task<long> GetFollowersCountAsync(string userId)
        {
            return await _follow.CountDocumentsAsync(f => f.FollowingId == userId && f.IsFollowing);
        }
        public async Task<long> GetFollowingCountAsync(string userId)
        {
            return await _follow.CountDocumentsAsync(f => f.FollowerId == userId && f.IsFollowing);
        }
        public async Task<bool> IsFollowingAsync(string followerId, string followeeId)
        {
            return await _follow.Find(f =>
                f.FollowerId == followerId && f.FollowingId == followeeId).AnyAsync();
        }

        public async Task<List<UserResponse>> GetPendingFollowRequests(string userId)
        {
            var pendingFollows = await _follow
                                    .Find(f => f.FollowingId == userId && !f.IsFollowing)
                                    .ToListAsync();

            if (!pendingFollows.Any()) return new List<UserResponse>();

            var followerIds = pendingFollows.Select(f => f.FollowerId).ToList();

            var filter = Builders<User>.Filter.In(u => u.Id, followerIds);
            var users = await _users.Find(filter).ToListAsync();

            var pendingRequests = users.Select(u => new UserResponse
            {
                Id = u.Id,
                FullName = u.FirstName + ' ' + u.LastName,
                Username = u.Username,
                Email = u.Email
            }).ToList();

            return pendingRequests;
        }


        public async Task<UserInfoDto?> GetUserInfoAsync(string userId)
        {
            var user = await _users.Find(u => u.Id == userId).FirstOrDefaultAsync();
            if (user == null) return null;

            // Get follower and following IDs
            var followerIds = await _follow.Find(f => f.FollowingId == userId && f.IsFollowing)
                                           .Project(f => f.FollowerId)
                                           .ToListAsync();

            var followingIds = await _follow.Find(f => f.FollowerId == userId && f.IsFollowing)
                                            .Project(f => f.FollowingId)
                                            .ToListAsync();

            var followers = await _users.Find(u => followerIds.Contains(u.Id)).ToListAsync();
            var following = await _users.Find(u => followingIds.Contains(u.Id)).ToListAsync();

            return new UserInfoDto
            {
                User = MapToUserResponse(user),
                Followers = [.. followers.Select(MapToUserResponse)],
                Following = [.. following.Select(MapToUserResponse)]
            };
        }
        public List<UserResponse> GetAllExcept(string? userId)
        {
            var filter = userId == null ? Builders<User>.Filter.Empty : Builders<User>.Filter.Ne(u => u.Id, userId);
            var users = _users.Find(filter).ToList();
            var response = users.Select(u => new UserResponse
            {
                Id = u.Id,
                Username = u.Username,
                FullName = u.FirstName + ' ' + u.LastName,
                Email = u.Email
            }).ToList();

            return response;
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
        private UserResponse MapToUserResponse(User user)
        {
            return new UserResponse
            {
                Id = user.Id,
                Username = user.Username,
                FullName = $"{user.FirstName} {user.LastName}".Trim(),
                Email = user.Email
            };
        }
    }
}
