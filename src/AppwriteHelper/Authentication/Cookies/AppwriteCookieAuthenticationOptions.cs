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

        public string AppwriteEndpoint { get; set; } = string.Empty;
        public string AppwriteProject { get; set; } = string.Empty;
    }
}