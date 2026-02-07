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
                // Only reject when we actually need the session later.
                if (options.CheckForRevokedSessions || options.RefreshAndStoreJwtTokenInCookie)
                {
                    await RejectAsync(context);
                }
                return;
            }

            // Optional: revoked session check
            if (options.CheckForRevokedSessions)
            {
                if (!await IsSessionValidAsync(options, session.Secret))
                {
                    await RejectAsync(context);
                    return;
                }
            }

            // Optional: refresh JWT
            if (!options.RefreshAndStoreJwtTokenInCookie)
                return;

            var now = DateTimeOffset.UtcNow;
            if (!ShouldRenewByJwt(context, now, options.JwtRenewalThreshold))
                return;

            var account = CreateAccount(options, session.Secret);

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
            }

            var newJwt = await TryCreateJwtAsync(account);
            if (newJwt == null)
            {
                await RejectAsync(context);
                return;
            }

            StoreJwtTokens(context, newJwt.Jwt);
            context.ShouldRenew = true;
        }

        private static Account CreateAccount(AppwriteCookieAuthenticationOptions options, string sessionSecret)
        {
            var client = new Client()
                .SetEndpoint(options.AppwriteEndpoint)
                .SetProject(options.AppwriteProject)
                .SetSession(sessionSecret);

            return new Account(client);
        }

        private static async Task<bool> IsSessionValidAsync(AppwriteCookieAuthenticationOptions options, string sessionSecret)
        {
            try
            {
                var account = CreateAccount(options, sessionSecret);
                var user = await account.Get();
                return user != null;
            }
            catch
            {
                return false;
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

        private static async Task<JWT?> TryCreateJwtAsync(Account account)
        {
            try
            {
                return await account.CreateJWT();
            }
            catch
            {
                return null;
            }
        }

        private static void StoreJwtTokens(CookieValidatePrincipalContext context, string jwt)
        {
            var jwtToken = new JwtSecurityToken(jwt);

            var tokens = context.Properties.GetTokens().ToList();
            tokens.RemoveAll(t => t.Name is
                AppwriteAuthenticationDefaults.AuthenticationTokenAppwriteJwt or
                AppwriteAuthenticationDefaults.AuthenticationTokenAppwriteJwtExpires);

            tokens.Add(new AuthenticationToken
            {
                Name = AppwriteAuthenticationDefaults.AuthenticationTokenAppwriteJwt,
                Value = jwt
            });
            tokens.Add(new AuthenticationToken
            {
                Name = AppwriteAuthenticationDefaults.AuthenticationTokenAppwriteJwtExpires,
                Value = jwtToken.ValidTo.ToString("O")
            });

            context.Properties.StoreTokens(tokens);
        }

        private static bool ShouldRenewByJwt(CookieValidatePrincipalContext context, DateTimeOffset now, TimeSpan threshold)
        {
            var jwtExpiresValue = context.Properties.GetTokenValue(AppwriteAuthenticationDefaults.AuthenticationTokenAppwriteJwtExpires);
            if (string.IsNullOrEmpty(jwtExpiresValue))
                return false;

            if (!DateTimeOffset.TryParse(jwtExpiresValue, out var jwtExpires))
                return false;

            return jwtExpires - now <= threshold;
        }

        private static bool TryGetSession(CookieValidatePrincipalContext context, out AppwriteSession session)
        {
            var json = context.Properties.GetTokenValue(AppwriteAuthenticationDefaults.AuthenticationTokenAppwriteSession);
            if (string.IsNullOrEmpty(json))
            {
                session = default;
                return false;
            }

            return AppwriteSession.TryParse(json, out session);
        }

        private static async Task RejectAsync(CookieValidatePrincipalContext context)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(context.Scheme.Name);
        }


        private readonly record struct AppwriteSession(string Secret, string? Id)
        {
            public static bool TryParse(string sessionJson, out AppwriteSession session)
            {
                try
                {
                    using var doc = JsonDocument.Parse(sessionJson);
                    var root = doc.RootElement;

                    var secret = root.TryGetProperty("secret", out var s) ? s.GetString() : null;
                    if (string.IsNullOrEmpty(secret))
                    {
                        session = default;
                        return false;
                    }

                    string? id = null;
                    if (root.TryGetProperty("$id", out var idEl) || root.TryGetProperty("id", out idEl))
                        id = idEl.GetString();

                    session = new AppwriteSession(secret, string.IsNullOrEmpty(id) ? null : id);
                    return true;
                }
                catch
                {
                    session = default;
                    return false;
                }
            }
        }
    }

    internal static class AppwriteCookieAuthenticationOptionsExtensions
    {
        public static bool HasEndpointAndProject(this AppwriteCookieAuthenticationOptions options)
            => !string.IsNullOrWhiteSpace(options.AppwriteEndpoint)
               && !string.IsNullOrWhiteSpace(options.AppwriteProject);
    }
}