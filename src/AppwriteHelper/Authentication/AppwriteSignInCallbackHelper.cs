using Appwrite.Models;
using AppwriteHelper.Authentication.AppwriteServer;
using AppwriteHelper.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Json;

namespace AppwriteHelper.Authentication
{
    public class AppwriteSignInCallbackHelper([FromKeyedServices(Constants.APPWRITE_CLIENT_SERVER)] IAppwriteClientFactory appwriteClientFactory)
    {
        private readonly IAppwriteClientFactory _appwriteClientFactory = appwriteClientFactory;

        /// <summary>
        /// Creates a sign-in result for the specified user with specific cookie options for lifetime enforcement.
        /// </summary>
        /// <param name="userId">The user ID to sign in.</param>
        /// <param name="secret">The authentication secret.</param>
        /// <param name="cookieOptions">Cookie options containing security settings like ExpireTimeSpan.</param>
        /// <returns>An AppwriteSignInResult containing the principal, authentication properties, session, and user information.</returns>
        public async Task<AppwriteSignInResult> CreateAppwriteCookieSignInAsync(
            string userId,
            string secret,
            IOptionsMonitor<AppwriteCookieAuthenticationOptions>? cookieOptions = null)
        {
            ArgumentException.ThrowIfNullOrEmpty(userId);
            ArgumentException.ThrowIfNullOrEmpty(secret);

            var serverClient = _appwriteClientFactory.Client ?? _appwriteClientFactory.CreateServerClientFromConfig();
            var serverAccount = new Appwrite.Services.Account(serverClient);

            var session = await serverAccount.CreateSession(userId, secret);
            if (session == null || string.IsNullOrEmpty(session.Secret))
                throw new InvalidOperationException("Invalid session or session secret");

            var userClient = _appwriteClientFactory.CreateUserClientFromSession(session.Secret);
            var userAccount = new Appwrite.Services.Account(userClient);

            var user = await userAccount.Get();
            if (user == null)
                throw new InvalidOperationException("User not given");

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, user.Name ?? string.Empty),
                new(ClaimTypes.Email, user.Email ?? string.Empty),
                new(ClaimTypes.NameIdentifier, user.Id ?? string.Empty),
            };

            var identity = new ClaimsIdentity(claims, AppwriteAuthenticationDefaults.CookieAuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            // Calculate session expiration time
            if (!long.TryParse(session.Expire?.ToString(), out var expireUnixTimestamp))
                throw new InvalidOperationException("Invalid session expiration timestamp");

            var sessionExpireTime = DateTimeOffset.FromUnixTimeSeconds(expireUnixTimestamp);
            var sessionExpireTimeSpan = sessionExpireTime - DateTimeOffset.UtcNow;

            // Ensure session expiration is positive
            if (sessionExpireTimeSpan <= TimeSpan.Zero)
                throw new InvalidOperationException("Session has already expired");

            // Determine the cookie expiration time
            TimeSpan cookieExpireTime;
            if (cookieOptions?.CurrentValue?.ExpireTimeSpan.HasValue == true)
            {
                // Use ExpireTimeSpan from options, but cap it to session expiration
                var configuredExpire = cookieOptions.CurrentValue.ExpireTimeSpan.Value;
                cookieExpireTime = configuredExpire > sessionExpireTimeSpan 
                    ? sessionExpireTimeSpan 
                    : configuredExpire;
            }
            else
            {
                // Use session expiration time
                cookieExpireTime = sessionExpireTimeSpan;
            }

            var authenticationProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.Add(cookieExpireTime),
                AllowRefresh = false
            };

            var appwriteSession = new AuthenticationToken
            {
                Name = AppwriteAuthenticationDefaults.AuthenticationTokenAppwriteSession,
                Value = JsonSerializer.Serialize(session.ToMap())
            };

            authenticationProperties.StoreTokens([appwriteSession]);

            return new AppwriteSignInResult(principal, authenticationProperties, session, user);
        }
    }

    public class AppwriteSignInResult(ClaimsPrincipal principal, AuthenticationProperties authenticationProperties, Session session, User user)
    {
        public ClaimsPrincipal Principal { get; } = principal;
        public AuthenticationProperties AuthenticationProperties { get; } = authenticationProperties;
        public Session Session { get; } = session;
        public User User { get; } = user;
    }
}