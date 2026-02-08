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
        /// Creates a sign-in result for the specified user with default cookie options.
        /// </summary>
        /// <param name="userId">The user ID to sign in.</param>
        /// <param name="secret">The authentication secret.</param>
        /// <param name="cookieLifetime">The cookie lifetime. If null, defaults to 15 minutes. Maximum is enforced by cookie options.</param>
        /// <param name="isPersistent">Whether the cookie should be persistent.</param>
        /// <param name="authenticationType">The authentication type. If null, defaults to cookie authentication scheme.</param>
        /// <returns>An AppwriteSignInResult containing the principal, authentication properties, session, and user information.</returns>
        public async Task<AppwriteSignInResult> CreateSignInAsync(string userId, string secret, TimeSpan? cookieLifetime = null, bool isPersistent = true, string? authenticationType = null)
        {
            return await CreateSignInAsync(userId, secret, cookieLifetime, isPersistent, authenticationType, null);
        }

        /// <summary>
        /// Creates a sign-in result for the specified user with specific cookie options for maximum lifetime enforcement.
        /// </summary>
        /// <param name="userId">The user ID to sign in.</param>
        /// <param name="secret">The authentication secret.</param>
        /// <param name="cookieLifetime">The cookie lifetime. If null, defaults to 15 minutes. Maximum is enforced by cookie options.</param>
        /// <param name="isPersistent">Whether the cookie should be persistent.</param>
        /// <param name="authenticationType">The authentication type. If null, defaults to cookie authentication scheme.</param>
        /// <param name="cookieOptions">Cookie options containing security settings like MaximumExpireTimeSpan. If null, uses default max of 24 hours.</param>
        /// <returns>An AppwriteSignInResult containing the principal, authentication properties, session, and user information.</returns>
        public async Task<AppwriteSignInResult> CreateSignInAsync(
            string userId, 
            string secret, 
            TimeSpan? cookieLifetime = null, 
            bool isPersistent = true, 
            string? authenticationType = null,
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

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, user?.Name ?? string.Empty),
                new(ClaimTypes.Email, user?.Email ?? string.Empty),
                new(ClaimTypes.NameIdentifier, user?.Id ?? string.Empty),
            };

            //if (user?.Prefs.Data != null)
            //{
            //    foreach (var p in user.Prefs.Data)
            //    {
            //        if (!string.IsNullOrEmpty(p.Key))
            //            claims.Add(new Claim(AppwriteClaimTypes.Pref(p.Key), p.Value.ToString()));
            //    }
            //}

            var identity = new ClaimsIdentity(claims, authenticationType ?? AppwriteAuthenticationDefaults.CookieAuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            // Enforce maximum cookie lifetime for security
            var defaultExpireTime = TimeSpan.FromMinutes(15);
            var maxExpireTime = cookieOptions?.CurrentValue?.MaximumExpireTimeSpan ?? TimeSpan.FromHours(24);
            
            var requestedExpireTime = cookieLifetime ?? defaultExpireTime;
            var expires = requestedExpireTime > maxExpireTime ? maxExpireTime : requestedExpireTime;

            var authenticationProperties = new AuthenticationProperties
            {
                IsPersistent = isPersistent,
                ExpiresUtc = DateTimeOffset.UtcNow.Add(expires),
                AllowRefresh = true
            };

            var appwriteSession = new AuthenticationToken
            {
                Name = AppwriteAuthenticationDefaults.AuthenticationTokenAppwriteSession,
                Value = JsonSerializer.Serialize(session.ToMap())
            };

            authenticationProperties.StoreTokens(new List<AuthenticationToken> { appwriteSession });

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