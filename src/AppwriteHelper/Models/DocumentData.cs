using System.Text.Json.Serialization;

namespace AppwriteHelper.Models
{
    public abstract class DocumentData
    {
        [JsonPropertyName("$id")]
        public string? Id { get; set; }

        [JsonPropertyName("$permissions")]
        public string[]? Permissions { get; set; }

        [JsonPropertyName("$createdAt")]
        public DateTimeOffset? CreatedAt { get; set; }
    }
}
