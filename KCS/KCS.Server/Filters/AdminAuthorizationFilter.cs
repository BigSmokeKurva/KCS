using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using KCS.Server.Database;

namespace KCS.Server.Filters
{
    public class AdminAuthorizationFilter(DatabaseContext db) : IAsyncAuthorizationFilter
    {
        private readonly DatabaseContext db = db;
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {

            string auth_token;
            if (context.HttpContext.Request.Path.StartsWithSegments("/api") && !context.HttpContext.Request.Path.StartsWithSegments("/api/auth/logout"))
            {
                context.HttpContext.Request.Headers.Remove("Cookie");
                auth_token = context.HttpContext.Request.Headers["Authorization"];
            }
            else
            {
                auth_token = context.HttpContext.Request.Cookies["auth_token"];
            }

            if (!Guid.TryParse(auth_token, out Guid auth_token_uid) || !await db.Sessions.AnyAsync(x => x.AuthToken == auth_token_uid))
            {
                context.Result = new RedirectResult("/signin");
                foreach (var cookie in context.HttpContext.Request.Cookies.Keys)
                {
                    context.HttpContext.Response.Cookies.Delete(cookie);
                }
                return;
            }
            var user = await db.Users.FirstAsync(x => db.Sessions.Any(y => y.Id == x.Id && y.AuthToken == auth_token_uid));
            if (user.Paused)
            {
                context.Result = new RedirectResult("/pause");
                return;
            }
            if (!user.Admin)
            {
                context.Result = new RedirectResult("/app");
            }
            user.LastOnline = TimeHelper.GetUnspecifiedUtc();
            await db.SaveChangesAsync();
        }
    }
}
