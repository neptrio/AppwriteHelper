using Appwrite;
using Appwrite.Models;
using Appwrite.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;

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
            if (!options.HasEndpointAndProject())
                return;

            if (!TryGetSession(context, out var session))
            {
                await RejectAsync(context);
                return;
            }

            // Check if session is expired
            if (session.Expire != null)
            {
                var expireDate = DateTimeOffset.Parse(session.Expire);
                if (expireDate <= DateTimeOffset.UtcNow)
                {
                    await RejectAsync(context);
                    return;
                }
            }

            // Optional: additional online revoked session check
            if (options.CheckForRevokedSessions)
            {
                var isSessionValid = await IsSessionRevokedAsync(options, session.Secret);
                if (!isSessionValid)
                {
                    _logger.LogWarning("Session has been revoked");
                    await RejectAsync(context);
                    return;
                }
            }

            var account = CreateAccountClient(options, session.Secret);
            if (options.ExtendSessionOnRenewal)
            {
                if (string.IsNullOrEmpty(session.Id))
                {
                    await RejectAsync(context);
                    return;
                }

                if (!await TryUpdateSessionAsync(account, session.Id))
                {
                    await RejectAsync(context);
                    return;
                }

                context.ShouldRenew = true;
            }
        }

        private static Account CreateAccountClient(AppwriteCookieAuthenticationOptions options, string sessionSecret)
        {
            var client = new Client()
                .SetEndpoint(options.AppwriteEndpoint)
                .SetProject(options.AppwriteProject)
                .SetSession(sessionSecret);

            return new Account(client);
        }

        private async Task<bool> IsSessionRevokedAsync(AppwriteCookieAuthenticationOptions options, string sessionSecret)
        {
            try
            {
                var account = CreateAccountClient(options, sessionSecret);
                var user = await account.Get();
                // Return false if session is revoked (user is null), true if session is still valid
                return user != null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to validate session with Appwrite. Session will be rejected for security.");
                // Fail secure - treat exception as revoked session
                return false;
            }
        }

        private async Task<bool> TryUpdateSessionAsync(Account account, string sessionId)
        {
            try
            {
                await account.UpdateSession(sessionId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update session {SessionId} with Appwrite", sessionId);
                return false;
            }
        }

        private bool TryGetSession(CookieValidatePrincipalContext context, out Session session)
        {
            session = null!;

            if (context?.Properties == null)
            {
                _logger.LogWarning("Cookie validation context or properties is null");
                return false;
            }

            var json = context.Properties.GetTokenValue(AppwriteAuthenticationDefaults.AuthenticationTokenAppwriteSession);
            if (string.IsNullOrEmpty(json))
            {
                _logger.LogDebug("No Appwrite session token found in authentication properties");
                return false;
            }

            try
            {
                var deserializedSession = JsonSerializer.Deserialize<Session>(json);
                if (deserializedSession == null || string.IsNullOrEmpty(deserializedSession.Secret))
                {
                    _logger.LogWarning("Deserialized session is null or has empty secret");
                    return false;
                }

                session = deserializedSession;
                return true;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize session from authentication token");
                return false;
            }
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