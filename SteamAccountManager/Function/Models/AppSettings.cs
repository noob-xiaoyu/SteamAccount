// Models/AppSettings.cs
using Newtonsoft.Json;

namespace SteamAccountManager.Models
{
    public class AppSettings
    {
        [JsonProperty("steamApiKey")]
        public string SteamApiKey { get; set; }
    }
}