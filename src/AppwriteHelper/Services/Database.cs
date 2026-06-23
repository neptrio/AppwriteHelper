using Appwrite.Services;

namespace AppwriteHelper.Services
{
    public abstract class Database
    {
        private TablesDB? UserTables;
        private TablesDB? ServerTables;

        private IAppwriteClientFactory? _userAppwriteClient;
        private IAppwriteClientFactory? _serverAppwriteClient;

        public void SetUserClientFactory(IAppwriteClientFactory client)
        {
            _userAppwriteClient = client;
        }

        public void SetServerClientFactory(IAppwriteClientFactory client)
        {
            _serverAppwriteClient = client;
        }

        private TablesDB GetOrInitUserTables()
        {
            if (UserTables == null)
            {
                if (_userAppwriteClient?.Client == null)
                    throw new InvalidOperationException();

                UserTables = new(_userAppwriteClient.Client);
            }

            return UserTables;
        }

        private TablesDB GetOrInitServerTables()
        {
            if (ServerTables == null)
            {
                if (_serverAppwriteClient?.Client == null)
                    throw new InvalidOperationException();

                ServerTables = new(_serverAppwriteClient.Client);
            }

            return ServerTables;
        }

        protected TablesDB GetTables(bool userServerClient)
        {
            if (userServerClient)
                return GetOrInitServerTables();
            return GetOrInitUserTables();
        }
    }
}
