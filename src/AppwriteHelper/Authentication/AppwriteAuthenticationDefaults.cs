namespace AppwriteHelper.Authentication
{
    public class AppwriteAuthenticationDefaults
    {
        public const string AuthenticationScheme = "AppwriteAuth";

        public static readonly string DisplayName = "AppwriteAuth";

        public const string AuthenticationTokenAppwriteJwt = "AppwriteHelper.AuthenticationToken.Jwt";
        public const string AuthenticationTokenAppwriteJwtExpires = "AppwriteHelper.AuthenticationToken.Jwt.ExpiresAt";
        public const string AuthenticationTokenAppwriteSession = "AppwriteHelper.AuthenticationToken.Session";

        public const string AppwriteHelperCookieName = "AppwriteHelperAuthCookie";

        public const string CookieAuthenticationScheme = "AppwriteCookies";
        public const string JwtAuthenticationScheme = "AppwriteJwt";
    }
}
