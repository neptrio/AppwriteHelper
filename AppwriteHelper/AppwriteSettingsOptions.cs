namespace AppwriteHelper
{
    public class AppwriteSettingsOptions
    {
        public const string SectionName = "Appwrite:Settings";

        public string Project { get; set; } = string.Empty;

        public string Endpoint { get; set; } = string.Empty;

        public string Key { get; set; } = string.Empty;
    }
}