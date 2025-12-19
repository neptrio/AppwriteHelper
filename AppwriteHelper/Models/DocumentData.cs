using Newtonsoft.Json;

namespace AppwriteHelper.Models
{
    public abstract class DocumentData
    {
        [JsonProperty("$id")]
        public string? Id { get; set; }

        [JsonProperty("$permissions")]
        public string[]? Permissions { get; set; }

        [JsonProperty("$createdAt")]
        public DateTimeOffset? CreatedAt { get; set; }

    }
}
