using Appwrite.Enums;
using Microsoft.AspNetCore.Http;

namespace AppwriteHelper.Authentication.Cookies
{
    public class AppwriteCookieAuthenticationOptions
    {
        public AppwriteCookieAuthenticationOptions()
        {
            Cookie.Name = "auth_token";
            Cookie.HttpOnly = true;
            Cookie.SecurePolicy = CookieSecurePolicy.Always;
            Cookie.SameSite = SameSiteMode.None;
            SlidingExpiration = true;
        }

        public CookieBuilder Cookie { get; } = new CookieBuilder();

        public TimeSpan ExpireTimeSpan { get; set; } = TimeSpan.FromMinutes(15);

        public bool SlidingExpiration { get; set; }

        public TimeSpan JwtRenewalThreshold { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// If enabled, the middleware refreshes the JWT before it expires and stores the new token in the cookie.
        /// </summary>
        public bool RefreshAndStoreJwtTokenInCookie { get; set; }

        public string AppwriteEndpoint { get; set; } = string.Empty;
        public string AppwriteProject { get; set; } = string.Empty;
    }
}