using Appwrite;
using Appwrite.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace AppwriteHelper.Authentication.AppwriteServer
{
    public class AppwriteAuthenticationOptions : RemoteAuthenticationOptions
    {
        public string RemoteLoginPath { get; set; }
        public string RemoteTokenValidationPath { get; set; }
        public string AppwriteEndpoint { get; set; }
        public string AppwriteProject { get; set; }
        public string AppwriteKey { get; set; }
        public bool UseAppwriteSession { get; set; } = false;

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

            var userClient = new Client()
                    .SetEndpoint(Options.AppwriteEndpoint + "/v1")
                    .SetProject(Options.AppwriteProject);

            Account account = new(userClient);

            List<AuthenticationToken> authenticationTokens = [];
            if (Options.UseAppwriteSession)
            {
                if (string.IsNullOrEmpty(Options.AppwriteKey))
                    throw new InvalidOperationException("When using the Session option we need a key.");

                try
                {
                    //to create a session with secret we need to use a key in the request.
                    var adminClient = new Client()
                        .SetEndpoint(Options.AppwriteEndpoint + "/v1")
                        .SetProject(Options.AppwriteProject)
                        .SetKey(Options.AppwriteKey);

                    Account _account = new(adminClient);
                    var session = await _account.CreateSession(userId, secret);

                    if (string.IsNullOrEmpty(session.Secret))
                    {
                        return HandleRequestResult.Fail("Invalid session");
                    }

                    authenticationTokens.Add(new AuthenticationToken { Name = AppwriteAuthenticationDefaults.AuthenticationTokenAppwriteSession, Value = JsonSerializer.Serialize(session.ToMap()) });

                    //add session to client to get account information later.
                    userClient.SetSession(session.Secret);
                }
                catch (Exception exception)
                {
                    //Logger.LogError(exception);
                    return HandleRequestResult.Fail(exception);
                }
            }
            else
            {
                try
                { 
                    //session has here no secret. only login.
                    await account.CreateSession(userId, secret);

                    //jwt is added to the client.
                    var jwt = await account.CreateJWT();
                    if (jwt == null)
                    {
                        return HandleRequestResult.Fail("Invalid jwt");
                    }

                    var jwtToken = new JwtSecurityToken(jwt.Jwt);

                    authenticationTokens.Add(new AuthenticationToken { Name = AppwriteAuthenticationDefaults.AuthenticationTokenAppwriteJwt, Value = jwt.Jwt });
                    authenticationTokens.Add(new AuthenticationToken { Name = AppwriteAuthenticationDefaults.AuthenticationTokenAppwriteJwtExpires, Value = jwtToken.ValidTo.ToString("O") });
                }
                catch (Exception exception)
                {
                    //Logger.LogError(exception);
                    return HandleRequestResult.Fail(exception);
                }
            }

            List<Claim> claims = [];
            try
            {
                // Get authenticated user identity
                var user = await account.Get();
                if (user == null)
                {
                    return HandleRequestResult.Fail("Invalid user");
                }

                claims.Add(new Claim(ClaimTypes.Name, user?.Name ?? ""));
                claims.Add(new Claim(ClaimTypes.Email, user?.Email ?? ""));
                claims.Add(new Claim(ClaimTypes.NameIdentifier, user?.Id ?? ""));

                // add prefs as claims
                if (user?.Prefs.Data != null)
                {
                    foreach (var p in user?.Prefs.Data)
                    {
                        if (!string.IsNullOrEmpty(p.Key))
                            claims.Add(new Claim(AppwriteClaimTypes.Pref(p.Key), p.Value.ToString()));
                    }
                }
            }
            catch (Exception exception)
            {
                //Logger.LogError(exception);
                return HandleRequestResult.Fail(exception);
            }

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);

            var authenticationProperties = new AuthenticationProperties();
            authenticationProperties.StoreTokens(authenticationTokens);

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
