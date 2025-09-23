using Microsoft.AspNetCore.Mvc;
using MongoDbTutorial.Services;

namespace MongoDbTutorial.Controllers
{
    public class ChatController : Controller
    {
        private readonly UserService _users;
        private readonly ChatService _chat;


        public ChatController(UserService users, ChatService chat)
        {
            _users = users;
            _chat = chat;
        }
        public IActionResult Index()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");


            ViewBag.UserId = userId;
            ViewBag.Username = HttpContext.Session.GetString("Username");
            ViewBag.Users = _users.GetAllExcept(userId);
            return View();
        }


        [HttpGet]
        public IActionResult Conversation(string otherUserId)
        {
            var currentUser = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(currentUser)) return Unauthorized();
            var messages = _chat.GetConversation(currentUser, otherUserId);
            return Json(messages);
        }


        [HttpPost]
        public IActionResult MarkRead(string fromUserId)
        {
            var currentUser = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(currentUser)) return Unauthorized();
            _chat.MarkAsRead(fromUserId, currentUser);
            return Ok();
        }

        [HttpGet]
        public IActionResult UnreadCount()
        {
            var currentUser = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(currentUser)) return Unauthorized();
            var count = _chat.GetUnreadCount(currentUser);
            return Json(new { count });
        }
    }
}
