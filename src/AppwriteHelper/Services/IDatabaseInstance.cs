using Appwrite.Services;

namespace AppwriteHelper.Services
{
    public interface IDatabaseInstance
    {
        TablesDB GetDatabase(bool useServerClient);
    }
}