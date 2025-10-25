using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace SteamAccountManager.Services
{
    public class SteamApiService
    {
        private readonly HttpClient _httpClient = new HttpClient();

        public SteamApiService() { }

        public async Task<Dictionary<string, string>> GetPlayerNicknamesAsync(IEnumerable<string> steamIds, string apiKey)
        {
            var validSteamIds = steamIds.Where(id => !string.IsNullOrEmpty(id)).ToList();
            if (!validSteamIds.Any()) return new Dictionary<string, string>();

            var steamIdsString = string.Join(",", validSteamIds);
            var url = $"https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/?key={apiKey}&steamids={steamIdsString}";

            try
            {
                var responseMessage = await _httpClient.GetAsync(url);
                if (!responseMessage.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Steam API 请求失败，状态码: {responseMessage.StatusCode}");
                }
                var jsonContent = await responseMessage.Content.ReadAsStringAsync();
                var apiResponse = JsonConvert.DeserializeObject<SteamApiResponse>(jsonContent);
                return apiResponse?.Response?.Players?.ToDictionary(p => p.SteamId, p => p.PersonaName)
                       ?? new Dictionary<string, string>();
            }
            catch (Exception ex)
            {
                throw new Exception("无法连接到 Steam API 获取昵称。请检查网络连接或 API Key。", ex);
            }
        }

        public async Task<Dictionary<string, BanData>> GetPlayerBansAsync(IEnumerable<string> steamIds, string apiKey)
        {
            var validSteamIds = steamIds.Where(id => !string.IsNullOrEmpty(id)).ToList();
            if (!validSteamIds.Any()) return new Dictionary<string, BanData>();

            var steamIdsString = string.Join(",", validSteamIds);
            var url = $"https://api.steampowered.com/ISteamUser/GetPlayerBans/v1/?key={apiKey}&steamids={steamIdsString}";

            try
            {
                var responseMessage = await _httpClient.GetAsync(url);
                if (!responseMessage.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Steam API 请求失败，状态码: {responseMessage.StatusCode}");
                }
                var jsonContent = await responseMessage.Content.ReadAsStringAsync();
                var apiResponse = JsonConvert.DeserializeObject<BanApiResponse>(jsonContent);
                return apiResponse?.Players?.ToDictionary(p => p.SteamId, p => new BanData { VACBanned = p.VACBanned, NumberOfGameBans = p.NumberOfGameBans })
                       ?? new Dictionary<string, BanData>();
            }
            catch (Exception ex)
            {
                throw new Exception("无法连接到 Steam API 获取封禁状态。请检查网络连接或 API Key。", ex);
            }
        }
    }

    public class BanData
    {
        public bool VACBanned { get; set; }
        public int NumberOfGameBans { get; set; }
    }

    public class BanApiResponse
    {
        [JsonProperty("players")]
        public List<PlayerBanInfo> Players { get; set; }
    }

    public class PlayerBanInfo
    {
        [JsonProperty("SteamId")]
        public string SteamId { get; set; }

        [JsonProperty("VACBanned")]
        public bool VACBanned { get; set; }

        [JsonProperty("NumberOfGameBans")]
        public int NumberOfGameBans { get; set; }
    }

    public class SteamApiResponse
    {
        [JsonProperty("response")]
        public PlayerSummaryResponse Response { get; set; }
    }

    public class PlayerSummaryResponse
    {
        [JsonProperty("players")]
        public List<Player> Players { get; set; }
    }

    public class Player
    {
        [JsonProperty("steamid")]
        public string SteamId { get; set; }

        [JsonProperty("personaname")]
        public string PersonaName { get; set; }
    }
}