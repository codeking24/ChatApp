namespace MongoDbTutorial.Middlewares
{
    public class SessionCheckMiddleware
    {
        private readonly RequestDelegate _next;

        public SessionCheckMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value;

            // Skip static files, login page, or API docs
            if (!path.StartsWith("/Account/Login", StringComparison.OrdinalIgnoreCase)
                && !path.StartsWith("/css") && !path.StartsWith("/js") && !path.StartsWith("/lib"))
            {
                var userId = context.Session.GetString("UserId");

                if (string.IsNullOrEmpty(userId))
                {
                    // Check if it's an AJAX/Fetch request
                    if (context.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                        context.Request.Headers["Accept"].ToString().Contains("application/json"))
                    {
                        context.Response.StatusCode = 401; // Unauthorized
                        await context.Response.WriteAsJsonAsync(new { redirectToLogin = true });
                        return;
                    }
                    else
                    {
                        // Regular browser request
                        context.Response.Redirect("/Account/Login");
                        return;
                    }
                }
            }

            await _next(context);
        }
    }

}
