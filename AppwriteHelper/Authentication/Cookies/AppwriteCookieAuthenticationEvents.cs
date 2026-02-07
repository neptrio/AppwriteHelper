using Appwrite;
using Appwrite.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;

namespace AppwriteHelper.Authentication.Cookies
{
    public class AppwriteCookieAuthenticationEvents : CookieAuthenticationEvents
    {
        private readonly IOptionsMonitor<AppwriteCookieAuthenticationOptions> _options;

        public AppwriteCookieAuthenticationEvents(IOptionsMonitor<AppwriteCookieAuthenticationOptions> options)
        {
            _options = options;
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

            if (string.IsNullOrWhiteSpace(options.AppwriteEndpoint) || string.IsNullOrWhiteSpace(options.AppwriteProject))
                return;

            if (!options.RefreshAndStoreJwtTokenInCookie)
                return;

            var now = DateTimeOffset.UtcNow;
            if (!ShouldRenewByJwt(context, now, options.JwtRenewalThreshold))
                return;

            var appwriteSessionJson = context.Properties.GetTokenValue(AppwriteAuthenticationDefaults.AuthenticationTokenAppwriteSession);
            if (string.IsNullOrEmpty(appwriteSessionJson))
            {
                await RejectAsync(context);
                return;
            }

            if (!TryGetSessionSecret(appwriteSessionJson, out var secret))
            {
                await RejectAsync(context);
                return;
            }

            var client = new Client()
                .SetEndpoint(options.AppwriteEndpoint)
                .SetProject(options.AppwriteProject)
                .SetSession(secret);

            var account = new Appwrite.Services.Account(client);

            JWT newJwt;
            try
            {
                newJwt = await account.CreateJWT();
            }
            catch
            {
                await RejectAsync(context);
                return;
            }

            var jwtToken = new JwtSecurityToken(newJwt.Jwt);

            var tokens = context.Properties.GetTokens().ToList();
            tokens.RemoveAll(t => t.Name == AppwriteAuthenticationDefaults.AuthenticationTokenAppwriteJwt);
            tokens.RemoveAll(t => t.Name == AppwriteAuthenticationDefaults.AuthenticationTokenAppwriteJwtExpires);

            tokens.Add(new AuthenticationToken
            {
                Name = AppwriteAuthenticationDefaults.AuthenticationTokenAppwriteJwt,
                Value = newJwt.Jwt
            });
            tokens.Add(new AuthenticationToken
            {
                Name = AppwriteAuthenticationDefaults.AuthenticationTokenAppwriteJwtExpires,
                Value = jwtToken.ValidTo.ToString("O")
            });

            context.Properties.StoreTokens(tokens);
            context.ShouldRenew = true;
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

        private static bool TryGetSessionSecret(string sessionJson, out string secret)
        {
            using var doc = JsonDocument.Parse(sessionJson);

            if (doc.RootElement.TryGetProperty("secret", out var secretElement))
            {
                var value = secretElement.GetString();
                if (!string.IsNullOrEmpty(value))
                {
                    secret = value;
                    return true;
                }
            }

            secret = string.Empty;
            return false;
        }

        private static async Task RejectAsync(CookieValidatePrincipalContext context)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(context.Scheme.Name);
        }
    }
}