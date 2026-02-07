namespace AppwriteHelper.Collections
{
    public interface IGenericCollection<T>
    {
        Task<object?> DeleteDocument(string documentId, bool useServerClient = false);
        Task<T?> GetDocument(string id, bool useServerClient = false);
        Task<IEnumerable<T>> GetDocuments(List<string>? queries = null, bool useServerClient = false);
        void SetServerClientFactory(IAppwriteClientFactory client);
        void SetUserClientFactory(IAppwriteClientFactory client);
        Task<T?> UpdateDocument(T document, List<string>? permissions = null, bool useServerClient = false);
        Task<T?> UpdateDocument(string documentId, object? document, List<string>? permissions = null, bool useServerClient = false);
        Task<T?> CreateDocument(T document, List<string>? permissions = null, bool useServerClient = false);
    }
}