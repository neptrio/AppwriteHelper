using AppwriteHelper.Authentication;
using AppwriteHelper.Authentication.AppwriteServer;
using AppwriteHelper.Authentication.Cookies;
using AppwriteHelper.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;


namespace AppwriteHelper
{
    public static class AuthenticationBuilderExtensions
    {
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

        public static AuthenticationBuilder AddAppwriteCookieAuthentication(this AuthenticationBuilder builder, Action<AppwriteCookieAuthenticationOptions> configureOptions)
        {
            return builder.AddAppwriteCookieAuthentication(AppwriteAuthenticationDefaults.CookieAuthenticationScheme, configureOptions);
        }

        public static AuthenticationBuilder AddAppwriteCookieAuthentication(this AuthenticationBuilder builder, string cookieScheme, Action<AppwriteCookieAuthenticationOptions> configureOptions)
        {
            builder.Services.Configure(cookieScheme, configureOptions);
            builder.Services.AddScoped<AppwriteCookieAuthenticationEvents>();

            builder.AddCookie(cookieScheme, options =>
            {
                options.EventsType = typeof(AppwriteCookieAuthenticationEvents);
            });

            return builder;
        }

        public static AuthenticationBuilder AddAppwriteJwtBearerAuthentication(this AuthenticationBuilder builder, Action<AppwriteJwtBearerAuthenticationOptions> configureOptions)
        {
            return builder.AddAppwriteJwtBearerAuthentication(AppwriteAuthenticationDefaults.JwtAuthenticationScheme, configureOptions);
        }

        public static AuthenticationBuilder AddAppwriteJwtBearerAuthentication(this AuthenticationBuilder builder, string jwtBearerScheme, Action<AppwriteJwtBearerAuthenticationOptions> configureOptions)
        {
            builder.Services.Configure(jwtBearerScheme, configureOptions);
            builder.Services.AddScoped<AppwriteJwtBearerAuthenticationEvents>();

            builder.Services.AddOptions<JwtBearerOptions>(jwtBearerScheme)
                .Configure<IOptionsMonitor<AppwriteJwtBearerAuthenticationOptions>>((jwtOptions, appwriteOptions) =>
                {
                    var options = appwriteOptions.Get(jwtBearerScheme);

                    jwtOptions.EventsType = typeof(AppwriteJwtBearerAuthenticationEvents);
                    jwtOptions.RequireHttpsMetadata = options.RequireHttpsMetadata;

                    if (!string.IsNullOrWhiteSpace(options.Authority))
                        jwtOptions.Authority = options.Authority;

                    jwtOptions.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = false,
                        SignatureValidator = static (token, validationParameters) => new JsonWebToken(token)
                    };
                });

            builder.AddJwtBearer(jwtBearerScheme);

            return builder;
        }
    }
}
