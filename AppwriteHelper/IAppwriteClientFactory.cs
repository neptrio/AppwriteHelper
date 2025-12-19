using Appwrite;

namespace AppwriteHelper
{
    public interface IAppwriteClientFactory
    {
        Client? Client { get; }

        Client CreateServerClientFromConfig(string? apiKey = null);
        Client CreateUserClient();
        Client CreateUserClientFromSession(string session);
        Client CreateUserClientFromToken(string appwriteJwt);
        
        void SetAppwriteClient(Client client);
    }
}