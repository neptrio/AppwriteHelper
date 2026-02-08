using Appwrite;
using Appwrite.Models;
using Appwrite.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;

namespace AppwriteHelper.Authentication.Cookies
{
    public sealed class AppwriteCookieAuthenticationEvents : CookieAuthenticationEvents
    {
        private readonly IOptionsMonitor<AppwriteCookieAuthenticationOptions> _options;

        public AppwriteCookieAuthenticationEvents(IOptionsMonitor<AppwriteCookieAuthenticationOptions> options)
            => _options = options;

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
                if (!await IsSessionRevokedAsync(options, session.Secret))
                {
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

        private static async Task<bool> IsSessionRevokedAsync(AppwriteCookieAuthenticationOptions options, string sessionSecret)
        {
            try
            {
                var account = CreateAccountClient(options, sessionSecret);
                var user = await account.Get();
                return user == null;
            }
            catch
            {
                return true;
            }
        }

        private static async Task<bool> TryUpdateSessionAsync(Account account, string sessionId)
        {
            try
            {
                await account.UpdateSession(sessionId);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryGetSession(CookieValidatePrincipalContext context, out Session session)
        {
            var json = context.Properties.GetTokenValue(AppwriteAuthenticationDefaults.AuthenticationTokenAppwriteSession);
            if (string.IsNullOrEmpty(json))
            {
                session = null!;
                return false;
            }

            try
            {
                var deserializedSession = JsonSerializer.Deserialize<Session>(json);
                if (deserializedSession == null || string.IsNullOrEmpty(deserializedSession.Secret))
                {
                    session = null!;
                    return false;
                }

                session = deserializedSession;
                return true;
            }
            catch
            {
                session = null!;
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