using Appwrite;
using Appwrite.Models;
using Appwrite.Services;

namespace AppwriteHelper.Collections
{
    public interface IStorageBucket
    {
        Storage? AppwriteStorage { get; }

        void SetServerClientFactory(IAppwriteClientFactory client);
        void SetUserClientFactory(IAppwriteClientFactory client);

        Task<Appwrite.Models.File> GetFile(string fileId, bool useServerClient = false);
        Task<byte[]> GetFileDownload(string fileId, bool useServerClient = false);        
        Task<Appwrite.Models.File?> UploadFile(InputFile file, List<string>? permissions = null, bool useServerClient = false);
	}
}