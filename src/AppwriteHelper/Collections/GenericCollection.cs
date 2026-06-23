using Appwrite;
using Appwrite.Services;
using AppwriteHelper.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace AppwriteHelper.Collections
{
    public class GenericCollection<T> : IGenericCollection<T> where T : DocumentData
    {
        private readonly IConfiguration _configuration;

        protected readonly string DATABASE_ID;
        protected readonly string COLLECTION_ID;

        private Databases? UserDatabases;
        private Databases? ServerDatabases;

        private TablesDB? UserTables;
        private TablesDB? ServerTables;

        private IAppwriteClientFactory? _userAppwriteClient;
        private IAppwriteClientFactory? _serverAppwriteClient;

        public GenericCollection(IConfiguration configuration, string databaseId, string? collectionId = null)
        {
            _configuration = configuration ?? throw new InvalidOperationException("Configuration is missing.");

            var databaseConfigKey = databaseId;
            var collectionConfigKey = collectionId;

            if (string.IsNullOrEmpty(collectionConfigKey))
                collectionConfigKey = typeof(T).Name;

            DATABASE_ID = _configuration["Appwrite:Databases:" + databaseConfigKey] ?? throw new ArgumentException("DatabaseId is missing in configuration.");
            COLLECTION_ID = _configuration["Appwrite:Collections:" + collectionConfigKey] ?? throw new ArgumentException("CollectionId is missing in configuration.");
        }

        public GenericCollection(
            IConfiguration configuration,
            string databaseId,
            string? collectionId,
            [FromKeyedServices(Constants.APPWRITE_CLIENT_USER)] IAppwriteClientFactory? userAppwriteClient = null,
            [FromKeyedServices(Constants.APPWRITE_CLIENT_SERVER)] IAppwriteClientFactory? serverAppwriteClient = null)
            : this(configuration, databaseId, collectionId)
        {
            _userAppwriteClient = userAppwriteClient;
            _serverAppwriteClient = serverAppwriteClient;
        }

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


        [Obsolete("This method has been deprecated. Please use `GetTables` instead.")]
        private Databases GetDatabases(bool userServerClient)
        {
            if (userServerClient)
                return GetOrInitServerDatabases();
            return GetOrInitUserDatabases();
        }

        
        private TablesDB GetTables(bool userServerClient)
        {
            if (userServerClient)
                return GetOrInitServerTables();
            return GetOrInitUserTables();
        }

        public async Task<T?> UpsertRow(T row, List<string>? permissions = null, bool useServerClient = false)
        {
            var updatedDocument = await GetTables(useServerClient).UpsertRow(databaseId: DATABASE_ID,
                                   tableId: COLLECTION_ID,
                                   rowId: row.Id,
                                   data: row,
                                   permissions: permissions);

            return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(updatedDocument.Data));
        }

        public async Task<T?> UpdateRow(T row, List<string>? permissions = null, bool useServerClient = false)
        {
            var updatedDocument = await GetTables(useServerClient).UpdateRow(databaseId: DATABASE_ID,
                                   tableId: COLLECTION_ID,
                                   rowId: row.Id,
                                   data: row,
                                   permissions: permissions);

            return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(updatedDocument.Data));
        }

        [Obsolete("This method has been deprecated. Please use `UpdateRow` instead.")]
        public async Task<T?> UpdateDocument(T document, List<string>? permissions = null, bool useServerClient = false)
        {
            var updatedDocument = await GetDatabases(useServerClient).UpdateDocument(databaseId: DATABASE_ID,
                                   collectionId: COLLECTION_ID,
                                   documentId: document.Id,
                                   data: document,
                                   permissions: permissions);

            return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(updatedDocument.Data));
        }

        public async Task<T?> UpdateDocument(string documentId, object? document, List<string>? permissions = null, bool useServerClient = false)
        {
            var updatedDocument = await GetDatabases(useServerClient).UpdateDocument(databaseId: DATABASE_ID,
                                   collectionId: COLLECTION_ID,
                                   documentId: documentId,
                                   data: document,
                                   permissions: permissions);

            return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(updatedDocument.Data));
        }

        public async Task<T?> CreateDocument(T document, List<string>? permissions = null, bool useServerClient = false)
        {

            var newDocument = await GetDatabases(useServerClient).CreateDocument(
                                   databaseId: DATABASE_ID,
                                   collectionId: COLLECTION_ID,
                                   documentId: ID.Unique(),
                                   data: document,
                                   permissions: permissions

                               );
            return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(newDocument.Data));

        }

        public async Task<object?> DeleteDocument(string documentId, bool useServerClient = false)
        {

            var newDocument = await GetDatabases(useServerClient).DeleteDocument(
                                   databaseId: DATABASE_ID,
                                   collectionId: COLLECTION_ID,
                                   documentId: documentId
                               );

            return newDocument;
            //return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(newDocument));
        }

        public async Task<T?> GetDocument(string id, bool useServerClient = false)
        {

            var document = await GetDatabases(useServerClient).GetDocument(
                                   databaseId: DATABASE_ID,
                                   collectionId: COLLECTION_ID,
                                   documentId: id
                               );
            return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(document.Data));

        }

        public async Task<IEnumerable<T>> GetDocuments(List<string>? queries = null, bool useServerClient = false)
        {
            var documents = new List<T>();

            var documentsFromDatabase = await GetDatabases(useServerClient).ListDocuments(
                                   databaseId: DATABASE_ID,
                                   collectionId: COLLECTION_ID,
                                   queries: queries
                               );

            foreach (var document in documentsFromDatabase.Documents)
            {
                var d = JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(document.Data));
                if (d == null)
                    continue;

                documents.Add(d);
            }
            return documents;
        }
    }
}