using Newtonsoft.Json;

namespace Dalamud.RichPresence.Models
{
    internal class LocalizationEntry
    {
        [JsonProperty("message")]
        public required string Message { get; set; }
        [JsonProperty("description")]
        public required string Description { get; set; }
    }
}
