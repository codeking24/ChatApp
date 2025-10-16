using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using MongoDbTutorial.Models.Users;
using MongoDbTutorial.Services;

namespace MongoDbTutorial.Controllers
{
    public class AccountController: Controller
    {
        private readonly UserService _users;

        public AccountController(UserService users)
        {
            _users = users;
        }
        public IActionResult Login() => View();
        public IActionResult Register() => View();

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            if (_users.ValidateCredentials(username, password))
            {
                var user = _users.GetByUsername(username)!;
                HttpContext.Session.SetString("UserId", user.Id ?? "");
                HttpContext.Session.SetString("Username", user.Username);
                return RedirectToAction("Index", "Chat");
            }
            ModelState.AddModelError("", "Invalid credentials");
            return View();
        }
        
        [HttpPost]
        public IActionResult Register([FromForm] User user )
        {
            try
            {
                if (string.IsNullOrWhiteSpace(user.Username) || string.IsNullOrWhiteSpace(user.PasswordHash))
                {
                    ModelState.AddModelError("", "Username and password required");
                    return View();
                }


                var existing = _users.GetByUsername(user.Username);
                if (existing != null)
                {
                    ModelState.AddModelError("", "Username already taken");
                    return View();
                }


                var newuser = _users.Create(user);
                HttpContext.Session.SetString("UserId", user.Id ?? "");
                HttpContext.Session.SetString("Username", user.Username);
                return RedirectToAction("Index", "Chat");
            }
            catch (Exception ex)
            {
                return RedirectToAction("Login");
            }
        }

        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return Ok();
        }

        [HttpGet]
        public JsonResult SearchUsers(string query)
        {
            var currentUserId = HttpContext.Session.GetString("UserId");
            var users = _users.GetAllExcept(currentUserId)
                .Where(u => u.FullName.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Select(async u => new
                {
                    id = u.Id,
                    fullName = u.FullName,
                    isFollowing = await _users.IsFollowingAsync(currentUserId, u.Id)
                })
                .ToList();

            return Json(users);
        }

        [HttpPost]
        public async Task<JsonResult> Follow(string id)
        {
            try
            {
                var currentUserId = HttpContext.Session.GetString("UserId");
                await _users.FollowUserAsync(currentUserId, id);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false });
            }
        }

        [HttpGet]
        public async Task<JsonResult> PendingFollowRequests()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false });

            var pending = await _users.GetPendingFollowRequests(userId);
            return Json(new { success = true, pending });
        }

    }
}
