using Appwrite.Models;
using AppwriteHelper.Authentication.AppwriteServer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using System.Text.Json;

namespace AppwriteHelper.Authentication
{
    public class AppwriteSignInCallbackHelper([FromKeyedServices(Constants.APPWRITE_CLIENT_SERVER)] IAppwriteClientFactory appwriteClientFactory)
    {
        private readonly IAppwriteClientFactory _appwriteClientFactory = appwriteClientFactory;

        public async Task<AppwriteSignInResult> CreateSignInAsync(string userId, string secret, TimeSpan? cookieLifetime = null, bool isPersistent = true, string? authenticationType = null)
        {
            ArgumentException.ThrowIfNullOrEmpty(userId);
            ArgumentException.ThrowIfNullOrEmpty(secret);

            var serverClient = _appwriteClientFactory.Client ?? _appwriteClientFactory.CreateServerClientFromConfig();
            var serverAccount = new Appwrite.Services.Account(serverClient);

            var session = await serverAccount.CreateSession(userId, secret);
            if (session == null)
                throw new InvalidOperationException("Invalid session");

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

            var expires = cookieLifetime ?? TimeSpan.FromMinutes(15);
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