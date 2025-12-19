using Appwrite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace AppwriteHelper
{
    public class AppwriteClientFactory(IConfiguration configuration) : IAppwriteClientFactory
    {
        public Client? Client { get; private set; }

        protected readonly IConfiguration _configuration = configuration;

        #region Configuration

        private string GetProjectFromConfig()
        {
            return _configuration["Appwrite:Settings:Project"] ?? _configuration["appwrite_project_id"] ?? throw new OptionsValidationException(
                "Appwrite",
                typeof(string),
                ["Missing Appwrite:Settings:Project or appwrite_project_id (legacy)"]);
        }

        private string GetKeyFromConfig()
        {
            return _configuration["Appwrite:Settings:Key"] ?? _configuration["appwrite_api_key"] ?? throw new OptionsValidationException(
                "Appwrite",
                typeof(string),
                ["Missing Appwrite:Settings:Key or appwrite_api_key (legacy)"]);
        }

        private string GetEndpointFromConfig()
        {
            return _configuration["Appwrite:Settings:Endpoint"] ?? throw new OptionsValidationException(
                "Appwrite",
                typeof(string),
                ["Missing Appwrite:Settings:Endpoint"]);
        }

        #endregion

        public Client CreateServerClientFromConfig(string? apiKey = null)
        {
            var client = new Client();
            client
                .SetEndpoint(GetEndpointFromConfig())
                .SetProject(GetProjectFromConfig())
                .SetKey(GetKeyFromConfig());

            return client;
        }

        public Client CreateUserClientFromToken(string appwriteJwt)
        {
            return CreateUserClient().SetJWT(appwriteJwt);
        }

        public Client CreateUserClientFromSession(string session)
        {
            return CreateUserClient().SetSession(session);
        }

        public Client CreateUserClient()
        {
            var client = new Client();
            client
                .SetEndpoint(GetEndpointFromConfig())
                .SetProject(GetProjectFromConfig());

            return client;
        }

        public void SetAppwriteClient(Client client)
        {
            Client = client;
        }
    }
}
