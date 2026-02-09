using Appwrite.Enums;
using Microsoft.AspNetCore.Http;

namespace AppwriteHelper.Authentication.Cookies
{
    public class AppwriteCookieAuthenticationOptions
    {
        public AppwriteCookieAuthenticationOptions()
        {
            Cookie.Name = AppwriteAuthenticationDefaults.AppwriteHelperCookieName;
            Cookie.HttpOnly = true;
            Cookie.SecurePolicy = CookieSecurePolicy.Always;
            Cookie.SameSite = SameSiteMode.Lax;
            Cookie.Path = "/";
        }

        public CookieBuilder Cookie { get; } = new CookieBuilder();

        public TimeSpan? ExpireTimeSpan { get; set; }

        /// <summary>
        /// If enabled, the middleware checks if the session is still valid by calling the Appwrite account endpoint.
        /// This should only be enabled, if you call endpoints not using the Appwrite Client that would fail if the session is beeing revoked.
        /// </summary>
        public bool CheckForRevokedSessions { get; set; } = false;

        public string AppwriteEndpoint { get; set; } = string.Empty;
        public string AppwriteProject { get; set; } = string.Empty;
    }
}