using Hangfire.Dashboard;
using System.Text;

namespace NewsAPI.Infrastructure;

public class HangfireBasicAuthFilter(string user, string password) : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var header = httpContext.Request.Headers["Authorization"].ToString();

        if (string.IsNullOrWhiteSpace(header) || !header.StartsWith("Basic "))
        {
            Challenge(httpContext);
            return false;
        }

        var encoded = header["Basic ".Length..].Trim();
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
        var parts = decoded.Split(':', 2);

        if (parts.Length == 2 && parts[0] == user && parts[1] == password)
            return true;

        Challenge(httpContext);
        return false;
    }

    private static void Challenge(HttpContext ctx)
    {
        ctx.Response.StatusCode = 401;
        ctx.Response.Headers["WWW-Authenticate"] = "Basic realm=\"Hangfire Dashboard\"";
    }
}
