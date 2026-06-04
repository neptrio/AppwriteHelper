using Appwrite;
using Appwrite.Models;
using Appwrite.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AppwriteHelper.Collections
{
	public class StorageBucket : IStorageBucket
	{
		private readonly IConfiguration _configuration;

		protected readonly string BUCKET_ID;

		public Storage? AppwriteStorage { get; private set; }

        private IAppwriteClientFactory? _userAppwriteClient;
        private IAppwriteClientFactory? _serverAppwriteClient;

        public StorageBucket(IConfiguration configuration, string bucketId)
		{
			_configuration = configuration ?? throw new InvalidOperationException("Configuration is missing.");
			BUCKET_ID = _configuration["Appwrite:Buckets:" + bucketId] ?? throw new ArgumentException("BucketId is missing in configuration.");
		}

        public StorageBucket(
            IConfiguration configuration, 
			string bucketId,
            [FromKeyedServices(Constants.APPWRITE_CLIENT_USER)] IAppwriteClientFactory? userAppwriteClient = null,
            [FromKeyedServices(Constants.APPWRITE_CLIENT_SERVER)] IAppwriteClientFactory? serverAppwriteClient = null)
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

        private Storage GetOrInitUserStorage()
        {
            if (AppwriteStorage == null)
            {
                if (_userAppwriteClient?.Client == null)
                    throw new InvalidOperationException();

                AppwriteStorage = new(_userAppwriteClient.Client);
            }

            return AppwriteStorage;
        }

        private Storage GetOrInitServerStorage()
        {
            if (AppwriteStorage == null)
            {
                if (_serverAppwriteClient?.Client == null)
                    throw new InvalidOperationException();

                AppwriteStorage = new(_serverAppwriteClient.Client);
            }

            return AppwriteStorage;
        }

        private Storage GetStorage(bool userServerClient)
        {
            if (userServerClient)
                return GetOrInitServerStorage();
            return GetOrInitUserStorage();
        }

        public async Task<Appwrite.Models.File> GetFile(string fileId, bool useServerClient = false)
        {
            var file = await GetStorage(useServerClient).GetFile(
                bucketId: BUCKET_ID,
                fileId: fileId);

            return file;
        }

        public async Task<byte[]> GetFileDownload(string fileId, bool useServerClient = false)
		{
			var file = await GetStorage(useServerClient).GetFileDownload(
				bucketId: BUCKET_ID,
				fileId: fileId);

			return file;
		}

		public async Task<Appwrite.Models.File?> UploadFile(InputFile file, List<string>? permissions = null, bool useServerClient = false)
		{
			return await GetStorage(useServerClient).CreateFile(
				bucketId: BUCKET_ID,
				fileId: ID.Unique(),
				file: file,
				permissions: permissions
			);
		}


    }
}