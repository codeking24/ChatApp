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
                // simple cookie-based session (for demo). Use proper auth in prod.
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

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
