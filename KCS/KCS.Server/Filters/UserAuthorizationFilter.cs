using KCS.Server.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace KCS.Server.Filters;

public class UserAuthorizationFilter(DatabaseContext db) : IAsyncAuthorizationFilter
{
    private readonly DatabaseContext _db = db;

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        string authToken;
        if (context.HttpContext.Request.Path.StartsWithSegments("/api") &&
            !context.HttpContext.Request.Path.StartsWithSegments("/api/auth/logout"))
        {
            context.HttpContext.Request.Headers.Remove("Cookie");
            authToken = context.HttpContext.Request.Headers["Authorization"];
        }
        else
        {
            authToken = context.HttpContext.Request.Cookies["auth_token"];
        }

        if (!Guid.TryParse(authToken, out var authTokenUid) ||
            !await _db.Sessions.AnyAsync(x => x.AuthToken == authTokenUid))
        {
            context.Result = new RedirectResult("/signin");

            // Очищаем все куки пользователя
            foreach (var cookie in context.HttpContext.Request.Cookies.Keys)
                context.HttpContext.Response.Cookies.Delete(cookie);
            return;
        }

        var user = await _db.Users.FirstAsync(x => _db.Sessions.Any(y => y.Id == x.Id && y.AuthToken == authTokenUid));
        if (user.Paused &&
            !context.HttpContext.Request.Path.StartsWithSegments("/api/app/getusername") &&
            !context.HttpContext.Request.Path.StartsWithSegments("/api/app/checkispause"))
        {
            context.Result = new RedirectResult("/pause");
            return;
        }

        user.LastOnline = TimeHelper.GetUnspecifiedUtc();
        await _db.SaveChangesAsync();
    }
}