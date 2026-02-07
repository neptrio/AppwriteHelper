using Appwrite;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppwriteHelper.Authentication.JwtBearer
{
    public sealed class AppwriteJsonWebTokenHandler : JsonWebTokenHandler
    {
        private readonly IOptionsMonitor<AppwriteJwtBearerAuthenticationOptions> _options;
        private readonly string _scheme;

        public AppwriteJsonWebTokenHandler(
            IOptionsMonitor<AppwriteJwtBearerAuthenticationOptions> options,
            string scheme)
        {
            _options = options;
            _scheme = scheme;
        }

        public override async Task<TokenValidationResult> ValidateTokenAsync(
            string token,
            TokenValidationParameters validationParameters)
        {
            var result = await base.ValidateTokenAsync(token, validationParameters);

            if (!result.IsValid)
                return result;

            var opts = _options.Get(_scheme);

            if (!opts.ValidateUser ||
                string.IsNullOrWhiteSpace(opts.AppwriteEndpoint) ||
                string.IsNullOrWhiteSpace(opts.AppwriteProject))
            {
                return result;
            }

            var jwt = (JsonWebToken)result.SecurityToken;

            string? tokenUserId = null;
            if (opts.ValidateUserIdClaim)
                tokenUserId = jwt.Claims.FirstOrDefault(c => c.Type == opts.UserIdClaimType)?.Value;

            try
            {
                var client = new Client()
                    .SetEndpoint(opts.AppwriteEndpoint)
                    .SetProject(opts.AppwriteProject)
                    .SetJWT(token);

                var account = new Appwrite.Services.Account(client);
                var user = await account.Get();

                if (user == null)
                    return Fail("Invalid user");

                if (opts.ValidateUserIdClaim &&
                    !string.Equals(user.Id, tokenUserId, StringComparison.Ordinal))
                    return Fail("Token user id mismatch");

                if (opts.ValidateUserStatus && !user.Status)
                    return Fail("User disabled");
            }
            catch (Exception ex)
            {
                return new TokenValidationResult
                {
                    IsValid = false,
                    Exception = ex
                };
            }

            return result;
        }

        private static TokenValidationResult Fail(string reason)
            => new TokenValidationResult
            {
                IsValid = false,
                Exception = new SecurityTokenInvalidSignatureException(reason)
            };
    }
}
