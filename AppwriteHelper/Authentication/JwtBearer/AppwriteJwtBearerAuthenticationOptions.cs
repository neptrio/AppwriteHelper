namespace AppwriteHelper.Authentication.JwtBearer
{
    public class AppwriteJwtBearerAuthenticationOptions
    {
        public string AppwriteEndpoint { get; set; } = string.Empty;

        public string AppwriteProject { get; set; } = string.Empty;

        public string Authority { get; set; } = string.Empty;

        public bool RequireHttpsMetadata { get; set; }

        public bool StoreJwtInAuthenticationProperties { get; set; } = true;

        public bool ValidateUser { get; set; } = true;
        public bool ValidateUserIdClaim { get; set; } = true;

        public bool ValidateUserStatus { get; set; } = true;

        public string UserIdClaimType { get; set; } = "userId";
    }
}