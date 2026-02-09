using Appwrite;
using Appwrite.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AppwriteHelper.Authentication.Cookies
{
    public sealed class AppwriteCookieAuthenticationEvents : CookieAuthenticationEvents
    {
        private readonly IOptionsMonitor<AppwriteCookieAuthenticationOptions> _options;
        private readonly ILogger<AppwriteCookieAuthenticationEvents> _logger;

        public AppwriteCookieAuthenticationEvents(
            IOptionsMonitor<AppwriteCookieAuthenticationOptions> options,
            ILogger<AppwriteCookieAuthenticationEvents> logger)
        {
            _options = options;
            _logger = logger;
        }

        public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }

        public override Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> context)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }

        public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
        {
            var options = _options.Get(context.Scheme.Name);

            var expiresUtc = context.Properties.ExpiresUtc;
            if (expiresUtc.HasValue && expiresUtc.Value <= DateTimeOffset.UtcNow)
            {
                await RejectAsync(context);
                return;
            }

            if (!TryGetSession(context, out var session))
            {
                await RejectAsync(context);
                return;
            }
        }

        private bool TryGetSession(CookieValidatePrincipalContext context, out string session)
        {
            session = "";

            if (context?.Properties == null)
            {
                _logger.LogWarning("Cookie validation context or properties is null");
                return false;
            }

            session = context.Properties.GetTokenValue(AppwriteAuthenticationDefaults.AuthenticationTokenAppwriteSession) ?? "";
            if (string.IsNullOrEmpty(session))
            {
                _logger.LogDebug("No Appwrite session token found in authentication properties");
                return false;
            }

            return true;
        }

        private static async Task RejectAsync(CookieValidatePrincipalContext context)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(context.Scheme.Name);
        }
    }

    internal static class AppwriteCookieAuthenticationOptionsExtensions
    {
        public static bool HasEndpointAndProject(this AppwriteCookieAuthenticationOptions options)
            => !string.IsNullOrWhiteSpace(options.AppwriteEndpoint)
               && !string.IsNullOrWhiteSpace(options.AppwriteProject);
    }
}