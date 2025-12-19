using Appwrite;
using Appwrite.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace AppwriteHelper.Authentication
{
    public class AppwriteAuthenticationOptions : RemoteAuthenticationOptions
    {
        public string RemoteLoginPath { get; set; }
        public string RemoteTokenValidationPath { get; set; }
        public string AppwriteEndpoint { get; set; }
        public string AppwriteProject { get; set; }
        public ICollection<string> Scope { get; } = new HashSet<string>();

        public AppwriteAuthenticationOptions()
        {
            CallbackPath = new PathString("/signin-appwrite");

            Events = new AppwriteAuthenticationEvents();
            Scope.Add("openid");
            Scope.Add("profile");
        }

        public override void Validate()
        {
            base.Validate();

            ArgumentException.ThrowIfNullOrEmpty(AppwriteProject);

            if (!CallbackPath.HasValue)
            {
                throw new ArgumentException("Options.CallbackPath must be provided.", nameof(CallbackPath));
            }

        }
    }

    public class AppwriteAuthenticationHandler : RemoteAuthenticationHandler<AppwriteAuthenticationOptions>
    {
        public AppwriteAuthenticationHandler(IOptionsMonitor<AppwriteAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder)
        {
        }

        public override async Task<bool> HandleRequestAsync()
        {

            return await base.HandleRequestAsync();
        }

        protected override Task<object> CreateEventsAsync() => Task.FromResult<object>(new AppwriteAuthenticationEvents());

        /// <summary>
        /// Responds to a 401 Challenge. Sends an request to appwrite to obtain an identity.
        /// </summary>
        /// <param name="properties"></param>
        /// <returns></returns>
        protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
        {

            var returnPath = $"{Request.Scheme}://{Request.Host}{Request.PathBase}{Options.CallbackPath}";
            var scopes = ScopesToUrl(Options.Scope);

            Response.Redirect(Options.AppwriteEndpoint + "/v1/account/tokens/oauth2/oidc?project=" + Options.AppwriteProject + "&" + scopes + "&success=" + returnPath);
        }

        private string ScopesToUrl(ICollection<string> scopes)
        {
            var scopeParams = scopes.Select((scope, index) =>
                $"scopes[{index}]={Uri.EscapeDataString(scope)}");

            string queryString = string.Join("&", scopeParams);
            return queryString;
        }


        /// <summary>
        /// Invoked to process incoming authentication requests. When Appwrite calls the return (success) url. 
        /// </summary>
        /// <returns></returns>
        protected override async Task<HandleRequestResult> HandleRemoteAuthenticateAsync()
        {

            //Get Data from authentication result.
            string? secret = Request.Query["secret"];
            if (string.IsNullOrEmpty(secret))
            {
                return HandleRequestResult.Fail("Invalid secret");
            }

            string? userId = Request.Query["userId"];
            if (string.IsNullOrEmpty(userId))
            {
                return HandleRequestResult.Fail("Invalid userId");
            }

            //Create Session
            var client = new Client().SetEndpoint(Options.AppwriteEndpoint + "/v1")
                .SetProject(Options.AppwriteProject);

            Appwrite.Services.Account account = new(client);

            Session? session;
            User? user;
            JWT? jwt;
            try
            {
                //hier keine secret enthalten. CreateSession muss mit API-Key ausgeführt werden um ein secret zu bekommen. Das später für setsession verwendet werden kann.
                session = await account.CreateSession(userId, secret);
                if (session == null)
                {
                    return HandleRequestResult.Fail("Invalid session");
                }

                jwt = await account.CreateJWT();
                user = await account.Get();
            }
            catch (Exception exception)
            {
                //Logger.LogError(exception);

                return HandleRequestResult.Fail(exception);
            }

            var jwtToken = new JwtSecurityToken(jwt.Jwt);

            // Create authenticated user identity
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user?.Name ?? ""),
                new Claim(ClaimTypes.Email, user?.Email?? ""),
                new Claim(ClaimTypes.NameIdentifier, user?.Id?? ""),
            };

            // add prefs as claims
            if (user?.Prefs.Data != null)
            {
                foreach (var p in user?.Prefs.Data)
                {
                    if (!string.IsNullOrEmpty(p.Key))
                        claims.Add(new Claim(AppwriteClaimTypes.Pref(p.Key), p.Value.ToString()));
                }
            }

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);

            var authenticationProperties = new AuthenticationProperties();

            //vielleicht müssen wir dann das hin und her im cookie nicht machen.
            //authenticationProperties.ExpiresUtc = jwtToken.ValidTo.AddMinutes(-14);

            var token = new AuthenticationToken { Name = AppwriteAuthenticationDefaults.AuthenticationTokenAppwriteJwt, Value = jwt.Jwt };
            var tokenExpiration = new AuthenticationToken { Name = AppwriteAuthenticationDefaults.AuthenticationTokenAppwriteJwtExpires, Value = jwtToken.ValidTo.ToString() };
            var appwriteSession = new AuthenticationToken { Name = AppwriteAuthenticationDefaults.AuthenticationTokenAppwriteSession, Value = JsonConvert.SerializeObject(session.ToMap()) };

            authenticationProperties.StoreTokens(new List<AuthenticationToken>() { token, appwriteSession, tokenExpiration });
            var ticket = new AuthenticationTicket(principal, authenticationProperties, Scheme.Name);

            return HandleRequestResult.Success(ticket);
        }
    }

    public static class AppwriteClaimTypes
    {
        public const string PrefPrefix = "AppwritePref";

        public static string Pref(string pref)
        {
            return PrefPrefix + "_" + pref;
        }

    }
}
