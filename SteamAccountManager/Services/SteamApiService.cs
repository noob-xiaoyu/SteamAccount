using Newtonsoft.Json;
using SteamAccountManager.Models;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace SteamAccountManager.Services
{
    // --- 用于反序列化 封禁信息 的类 (现在是独立的公共类) ---
    public class PlayerBan
    {
        public string SteamId { get; set; }
        public bool CommunityBanned { get; set; }
        public bool VACBanned { get; set; }
        public int NumberOfVACBans { get; set; }
        public int DaysSinceLastBan { get; set; }
        public int NumberOfGameBans { get; set; }
        public string EconomyBan { get; set; }
    }

    public class PlayerBanResponse
    {
        public List<PlayerBan> players { get; set; }
    }

    // --- 用于反序列化 玩家昵称 的类 (你原来的代码中就有) ---
    public class Player
    {
        [JsonProperty("steamid")]
        public string SteamId { get; set; }

        [JsonProperty("personaname")]
        public string PersonaName { get; set; }
    }

    public class PlayerSummaryResponse
    {
        [JsonProperty("players")]
        public List<Player> Players { get; set; }
    }

    public class SteamApiResponse
    {
        [JsonProperty("response")]
        public PlayerSummaryResponse Response { get; set; }
    }


    // --- 主要的服务类 ---
    public class SteamApiService
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly string _apiKey;

        public SteamApiService(string apiKey)
        {
            _apiKey = apiKey;
        }

        // --- 恢复了获取玩家昵称的方法 ---
        public async Task<Dictionary<string, string>> GetPlayerNicknamesAsync(IEnumerable<string> steamIds)
        {
            var validSteamIds = steamIds.Where(id => !string.IsNullOrEmpty(id)).ToList();
            if (!validSteamIds.Any())
            {
                return new Dictionary<string, string>();
            }

            var steamIdsString = string.Join(",", validSteamIds);
            var url = $"https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/?key={_apiKey}&steamids={steamIdsString}";

            try
            {
                var response = await _httpClient.GetStringAsync(url);
                var apiResponse = JsonConvert.DeserializeObject<SteamApiResponse>(response);

                return apiResponse?.Response?.Players?
                    .ToDictionary(p => p.SteamId, p => p.PersonaName)
                    ?? new Dictionary<string, string>();
            }
            catch
            {
                return new Dictionary<string, string>();
            }
        }

        // --- 获取玩家封禁状态的方法 ---
        public async Task<Dictionary<string, PlayerBan>> GetPlayerBansAsync(IEnumerable<string> steamIds)
        {
            var validSteamIds = steamIds.Where(id => !string.IsNullOrEmpty(id)).ToList();
            if (!validSteamIds.Any())
            {
                return new Dictionary<string, PlayerBan>();
            }

            var steamIdsString = string.Join(",", validSteamIds);
            var url = $"https://api.steampowered.com/ISteamUser/GetPlayerBans/v1/?key={_apiKey}&steamids={steamIdsString}";

            try
            {
                var response = await _httpClient.GetStringAsync(url);
                var apiResponse = JsonConvert.DeserializeObject<PlayerBanResponse>(response);

                return apiResponse?.players?.ToDictionary(p => p.SteamId)
                       ?? new Dictionary<string, PlayerBan>();
            }
            catch
            {
                return new Dictionary<string, PlayerBan>();
            }
        }
    }
}