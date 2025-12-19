using Appwrite;
using AppwriteHelper.Collections;
using AppwriteHelper.Middelwares;
using AppwriteHelper.Models;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using AppwriteHelper.Authentication;

namespace AppwriteHelper
{
    public static class Extensions
    {
        public static IServiceCollection AddAppwriteUserClient(this IServiceCollection services)
        {
            //typicaly we will call SetAppwriteClient later from middelware with this approach.  
            services.AddAppwriteClient(Constants.APPWRITE_CLIENT_USER);

            //we need this when using UserClient
            services.AddScoped<AppwriteUserClientCollectionMiddelware>();

            return services;
        }

        public static IServiceCollection AddAppwriteServerClient(this IServiceCollection services)
            => AddAppwriteClient(services, Constants.APPWRITE_CLIENT_SERVER, true);

        public static IServiceCollection AddAppwriteClient(this IServiceCollection services, string clientKey, bool createServerClientFromConfig = false)
        {
            services.AddKeyedScoped<IAppwriteClientFactory>(clientKey, (sp, key) =>
            {
                var client = new AppwriteClientFactory(sp.GetRequiredService<IConfiguration>());

                //if server client we can finalize the client and set from config.
                //if not server client this can be set from middelware.
                if (createServerClientFromConfig)
                    client.SetAppwriteClient(client.CreateServerClientFromConfig());

                return client;
            });

            return services;
        }


        public static IServiceCollection AddGenericAppwriteCollection<T>(this IServiceCollection services, string databaseName) where T : DocumentData
        {
            services.AddScoped<IGenericCollection<T>>(x =>
            {
                var collection = new GenericCollection<T>(x.GetRequiredService<IConfiguration>(), databaseName);

                var appwriteServerClient = x.GetKeyedService<IAppwriteClientFactory>(Constants.APPWRITE_CLIENT_SERVER);
                if (appwriteServerClient != null)
                    collection.SetServerClientFactory(appwriteServerClient);

                var appwriteUserClient = x.GetKeyedService<IAppwriteClientFactory>(Constants.APPWRITE_CLIENT_USER);
                if (appwriteUserClient != null)
                    collection.SetUserClientFactory(appwriteUserClient);

                return collection;
            });

            return services;
        }

        public static IApplicationBuilder UseAppwriteCollectionMiddleware(this IApplicationBuilder app) => app.UseMiddleware<AppwriteUserClientCollectionMiddelware>();

        public static AuthenticationBuilder AddAppwriteAuthentication(this AuthenticationBuilder builder)
        => builder.AddAppwriteAuthentication(AppwriteAuthenticationDefaults.AuthenticationScheme, _ => { });

        public static AuthenticationBuilder AddAppwriteAuthentication(this AuthenticationBuilder builder, Action<AppwriteAuthenticationOptions> configureOptions)
        => builder.AddAppwriteAuthentication(AppwriteAuthenticationDefaults.AuthenticationScheme, configureOptions);

        public static AuthenticationBuilder AddAppwriteAuthentication(this AuthenticationBuilder builder, string authenticationScheme, Action<AppwriteAuthenticationOptions> configureOptions)
       => builder.AddAppwriteAuthentication(authenticationScheme, AppwriteAuthenticationDefaults.DisplayName, configureOptions);

        public static AuthenticationBuilder AddAppwriteAuthentication(this AuthenticationBuilder builder, string authenticationScheme, string? displayName, Action<AppwriteAuthenticationOptions> configureOptions)
        {
            return builder.AddRemoteScheme<AppwriteAuthenticationOptions, AppwriteAuthenticationHandler>(authenticationScheme, displayName, configureOptions);
        }
    }
}
