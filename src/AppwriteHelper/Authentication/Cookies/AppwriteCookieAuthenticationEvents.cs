using Appwrite;
using Appwrite.Models;
using Appwrite.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

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

            if (!TryGetSession(context, out var session) && !TryGetJWT(context, out var jwt))
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
                _logger.LogDebug("No Appwrite session found in authentication properties");
                return false;
            }

            Session? sessionObj = JsonSerializer.Deserialize<Session>(session);
            if (sessionObj == null)
            {
                _logger.LogDebug("No Appwrite session found in authentication properties");
                return false;
            }

            if (DateTime.Parse(sessionObj.Expire) < DateTime.UtcNow)
            {
                _logger.LogDebug("Appwrite session expired.");
                return false;
            }

            if (string.IsNullOrEmpty(sessionObj.Secret))
            {
                _logger.LogDebug("Appwrite session not valid.");
                return false;
            }

            return true;
        }

        private bool TryGetJWT(CookieValidatePrincipalContext context, out string jwt)
        {
            jwt = "";

            if (context?.Properties == null)
            {
                _logger.LogWarning("Cookie validation context or properties is null");
                return false;
            }

            jwt = context.Properties.GetTokenValue(AppwriteAuthenticationDefaults.AuthenticationTokenAppwriteJwt) ?? "";
            if (string.IsNullOrEmpty(jwt))
            {
                _logger.LogDebug("No Appwrite session token found in authentication properties");
                return false;
            }

            var jwtExpiration = context.Properties.GetTokenValue(AppwriteAuthenticationDefaults.AuthenticationTokenAppwriteJwtExpires) ?? "";
            if (string.IsNullOrEmpty(jwtExpiration))
            {
                _logger.LogDebug("No expire date found in authentication properties for jwt");
                return false;
            }
            else
            {
                var expiresUtc = DateTimeOffset.Parse(
                    jwtExpiration,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

                if (expiresUtc < DateTime.UtcNow)
                {
                    _logger.LogDebug("Jwt is expired.");
                    return false;
                }
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