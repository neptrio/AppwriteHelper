using Appwrite;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AppwriteHelper.Authentication.JwtBearer
{
    public class AppwriteJwtBearerAuthenticationEvents : JwtBearerEvents
    {
        private readonly IOptionsMonitor<AppwriteJwtBearerAuthenticationOptions> _options;

        public AppwriteJwtBearerAuthenticationEvents(IOptionsMonitor<AppwriteJwtBearerAuthenticationOptions> options)
        {
            _options = options;
        }


        public override Task MessageReceived(MessageReceivedContext context)
        {
            var token = context.Token;

            if (!string.IsNullOrEmpty(token))
            {
                context.HttpContext.Items[
                    AppwriteAuthenticationDefaults.AuthenticationTokenAppwriteJwt
                ] = token;
            }

            return Task.CompletedTask;
        }

        public override async Task TokenValidated(TokenValidatedContext context)
        {
            var options = _options.Get(context.Scheme.Name);

            if (string.IsNullOrWhiteSpace(options.AppwriteEndpoint) || string.IsNullOrWhiteSpace(options.AppwriteProject))
                return;

            var rawJwt = context.HttpContext.Items[AppwriteAuthenticationDefaults.AuthenticationTokenAppwriteJwt] as string;

            if (string.IsNullOrEmpty(rawJwt))
            {
                context.Fail("Missing bearer token.");
                return;
            }

            if (options.StoreJwtInAuthenticationProperties)
            {
                var tokens = context.Properties.GetTokens().ToList();
                tokens.RemoveAll(t => t.Name == AppwriteAuthenticationDefaults.AuthenticationTokenAppwriteJwt);
                tokens.Add(new AuthenticationToken
                {
                    Name = AppwriteAuthenticationDefaults.AuthenticationTokenAppwriteJwt,
                    Value = rawJwt
                });

                context.Properties.StoreTokens(tokens);
            }

            if (!options.ValidateUser)
                return;

            string? tokenUserId = null;
            if (options.ValidateUserIdClaim)
            {
                tokenUserId = context.Principal?.Claims.SingleOrDefault(c => c.Type == options.UserIdClaimType)?.Value;
                if (string.IsNullOrEmpty(tokenUserId))
                {
                    context.Fail("Missing user id claim.");
                    return;
                }
            }

            try
            {
                var client = new Client()
                    .SetEndpoint(options.AppwriteEndpoint)
                    .SetProject(options.AppwriteProject)
                    .SetJWT(rawJwt);

                var account = new Appwrite.Services.Account(client);
                var user = await account.Get();

                if (user == null)
                {
                    context.Fail("Invalid user.");
                    return;
                }

                if (options.ValidateUserIdClaim && !string.Equals(user.Id, tokenUserId, StringComparison.Ordinal))
                {
                    context.Fail("Token user id does not match.");
                    return;
                }

                if (options.ValidateUserStatus && !user.Status)
                {
                    context.Fail("User is disabled.");
                    return;
                }
            }
            catch
            {
                context.Fail("Token validation failed.");
            }
        }

        private static string? GetRawJwtFromAuthorizationHeader(HttpRequest request)
        {
            var authHeader = request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return null;

            return authHeader["Bearer ".Length..].Trim();
        }
    }
}