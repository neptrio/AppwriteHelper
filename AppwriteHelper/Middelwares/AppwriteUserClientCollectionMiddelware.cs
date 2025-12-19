using AppwriteHelper.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace AppwriteHelper.Middelwares
{
    public class AppwriteUserClientCollectionMiddelware([FromKeyedServices(Constants.APPWRITE_CLIENT_USER)] IAppwriteClientFactory client) : IMiddleware
    {
        private readonly IAppwriteClientFactory? _client = client;

        public Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var authenticateResultFeature = context.Features.Get<IAuthenticateResultFeature>();
            var authenticationProperties = authenticateResultFeature?.AuthenticateResult?.Properties;
            var token = authenticationProperties?.GetTokenValue(AppwriteAuthenticationDefaults.AuthenticationTokenAppwriteJwt);

            if (authenticateResultFeature?.AuthenticateResult?.Succeeded == true)
            {
                if (_client != null)
                    if (!string.IsNullOrEmpty(token))
                        _client.SetAppwriteClient(_client.CreateUserClientFromToken(token));
            }
            else
            {
                _client?.SetAppwriteClient(_client.CreateUserClient());
            }

            return next(context);
        }
    }
}
