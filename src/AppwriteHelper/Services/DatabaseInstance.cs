using Appwrite.Models;
using Appwrite.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppwriteHelper.Services
{
    public class DatabaseInstance : Database, IDatabaseInstance
    {
        public DatabaseInstance(
          [FromKeyedServices(Constants.APPWRITE_CLIENT_USER)] IAppwriteClientFactory? userAppwriteClient = null,
          [FromKeyedServices(Constants.APPWRITE_CLIENT_SERVER)] IAppwriteClientFactory? serverAppwriteClient = null)
        {
            if (userAppwriteClient != null)
                SetUserClientFactory(userAppwriteClient);

            if (serverAppwriteClient != null)
                SetServerClientFactory(serverAppwriteClient);
        }

        public TablesDB GetDatabase(bool useServerClient)
        {
            return GetTables(useServerClient);
        }
    }
}
