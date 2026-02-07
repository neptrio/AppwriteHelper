using AppwriteHelper.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace AppwriteHelper.Middelwares
{
    public class AppwriteUserClientCollectionMiddelware([FromKeyedServices(Constants.APPWRITE_CLIENT_USER)] IAppwriteClientFactory client) : IMiddleware
    {
        private readonly IAppwriteClientFactory? _client = client;

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var authenticateResultFeature = context.Features.Get<IAuthenticateResultFeature>();
            var authenticationProperties = authenticateResultFeature?.AuthenticateResult?.Properties;
            var token = authenticationProperties?.GetTokenValue(AppwriteAuthenticationDefaults.AuthenticationTokenAppwriteJwt);
            var session = authenticationProperties?.GetTokenValue(AppwriteAuthenticationDefaults.AuthenticationTokenAppwriteSession);

            if (authenticateResultFeature?.AuthenticateResult?.Succeeded == true)
            {
                if (_client != null)
                {
                    if (!string.IsNullOrEmpty(session))
                    {
                        _client.SetAppwriteClient(_client.CreateUserClientFromSession(session));
                        await  next(context);
                        return;
                    }

                    if (!string.IsNullOrEmpty(token))
                    {
                        _client.SetAppwriteClient(_client.CreateUserClientFromToken(token));
                    }
                }
            }
            else
            {
                _client?.SetAppwriteClient(_client.CreateBaseUserClient());
            }

            await next(context);
            return;
        }
    }
}
