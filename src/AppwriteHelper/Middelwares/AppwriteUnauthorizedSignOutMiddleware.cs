using Appwrite;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

public sealed class AppwriteUnauthorizedSignOutMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AppwriteUnauthorizedSignOutMiddleware> _logger;
    private readonly string _scheme;

    public AppwriteUnauthorizedSignOutMiddleware(
        RequestDelegate next,
        ILogger<AppwriteUnauthorizedSignOutMiddleware> logger,
        string cookieAuthScheme)
    {
        _next = next;
        _logger = logger;
        _scheme = cookieAuthScheme;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppwriteException ex) when (ex.Code == 401 || ex.Code == 403)
        {
            if (!context.Response.HasStarted)
            {
                await context.SignOutAsync(_scheme);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            }

            _logger.LogInformation(ex, "Signed out because Appwrite returned {Code}.", ex.Code);
            return;
        }
    }
}

public static class AppwriteSessionSyncMiddlewareExtensions
{
    public static IApplicationBuilder UseAppwriteSessionSync(
        this IApplicationBuilder app,
        string cookieAuthScheme)
    {
        return app.UseMiddleware<AppwriteSessionSyncMiddleware>(cookieAuthScheme);
    }
}
