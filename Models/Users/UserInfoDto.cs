using MongoDB.Bson;

namespace MongoDbTutorial.Models.Users
{
    public class UserInfoDto
    {
        public UserResponse User { get; set; }
        public List<UserResponse> Followers { get; set; }
        public List<UserResponse> Following { get; set; }
        public int FollowersCount => Followers?.Count ?? 0;
        public int FollowingCount => Following?.Count ?? 0;
    }

    public class UserResponse
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
    }
}
