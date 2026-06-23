using Appwrite.Services;

namespace AppwriteHelper.Services
{
    public abstract class Database
    {
        private TablesDB? UserTables;
        private TablesDB? ServerTables;

        private Databases? UserDatabases;
        private Databases? ServerDatabases;

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

        #region Obsolet Databases

        [Obsolete]
        private Databases GetOrInitUserDatabases()
        {
            if (UserDatabases == null)
            {
                if (_userAppwriteClient?.Client == null)
                    throw new InvalidOperationException();

                UserDatabases = new(_userAppwriteClient.Client);
            }

            return UserDatabases;
        }

        [Obsolete]
        private Databases GetOrInitServerDatabases()
        {
            if (ServerDatabases == null)
            {
                if (_serverAppwriteClient?.Client == null)
                    throw new InvalidOperationException();

                ServerDatabases = new(_serverAppwriteClient.Client);
            }

            return ServerDatabases;
        }

        [Obsolete("This method has been deprecated. Please use `GetTables` instead.")]
        protected Databases GetDatabases(bool userServerClient)
        {
            if (userServerClient)
                return GetOrInitServerDatabases();
            return GetOrInitUserDatabases();
        }

        #endregion

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
