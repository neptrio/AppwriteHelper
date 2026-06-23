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
        Task<T?> GetRow(string id, bool useServerClient = false, string? transactionId = null);
        Task<IEnumerable<T>> GetRows(List<string>? queries = null, bool useServerClient = false, string? transactionId = null);
        Task<T?> UpsertRow(T row, List<string>? permissions = null, bool useServerClient = false, string? transactionId = null);
        Task<T?> UpsertRow(string rowId, object? data, List<string>? permissions = null, bool useServerClient = false, string? transactionId = null);
        Task<T?> UpdateRow(T row, List<string>? permissions = null, bool useServerClient = false, string? transactionId = null);
        Task<T?> UpdateRow(string rowId, object? data, List<string>? permissions = null, bool useServerClient = false, string? transactionId = null);
        Task<T?> CreateRow(T document, List<string>? permissions = null, bool useServerClient = false, string? transactionId = null);
        Task<object?> DeleteDocument(string rowId, bool useServerClient = false, string? transactionId = null);
    }
}