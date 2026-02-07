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
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

            if (!string.IsNullOrEmpty(authHeader) &&
                authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                context.Token = GetRawJwtFromAuthorizationHeader(context.Request);
            }

            if (!string.IsNullOrEmpty(context.Token))
            {
                context.HttpContext.Items[
                    AppwriteAuthenticationDefaults.AuthenticationTokenAppwriteJwt
                ] = context.Token;
            }

            return Task.CompletedTask;
        }

        public override Task TokenValidated(TokenValidatedContext context)
        {
            var options = _options.Get(context.Scheme.Name);

            if (string.IsNullOrWhiteSpace(options.AppwriteEndpoint) || string.IsNullOrWhiteSpace(options.AppwriteProject))
                return Task.CompletedTask;

            var rawJwt = context.HttpContext.Items[AppwriteAuthenticationDefaults.AuthenticationTokenAppwriteJwt] as string;

            if (string.IsNullOrEmpty(rawJwt))
            {
                context.Fail("Missing bearer token.");
                return Task.CompletedTask;
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
                context.Success();
            }

            return Task.CompletedTask;
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