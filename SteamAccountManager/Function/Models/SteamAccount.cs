// Models/SteamAccount.cs
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SteamAccountManager.Models
{
    public class SteamAccount : INotifyPropertyChanged
    {
        // --- Backing fields for properties that need notification ---
        private string _nickname;
        private string _username;
        private string _password;
        private string _steamId64;
        private bool _isPrime;
        private bool _isBanned;
        private int _banReason;
        private DateTime _cooldownExpiry;
        private string _email;
        private string _emailPassword;

        // ... Id, Nickname, Username, Password, SteamId64, ProfileUrl, IsPrime properties remain the same ...

        [JsonProperty("IsBanned")]
        public bool IsBanned
        {
            get => _isBanned;
            set
            {
                _isBanned = value;
                OnPropertyChanged();
                // When IsBanned changes, Status and SortKey also change.
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(StatusSortKey));
            }
        }

        [JsonProperty("BanReason")]
        public int BanReason
        {
            get => _banReason;
            set
            {
                _banReason = value;
                OnPropertyChanged();
                // When BanReason changes, Status and SortKey also change.
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(StatusSortKey));
            }
        }

        [JsonProperty("CooldownExpiry")]
        public DateTime CooldownExpiry
        {
            get => _cooldownExpiry;
            set
            {
                _cooldownExpiry = value;
                OnPropertyChanged();
                // When CooldownExpiry changes, Status also changes.
                OnPropertyChanged(nameof(Status));
            }
        }

        [JsonProperty("Email")]
        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(); }
        }

        [JsonProperty("EmailPassword")]
        public string EmailPassword
        {
            get => _emailPassword;
            set { _emailPassword = value; OnPropertyChanged(); }
        }
        // ... IsPrimeText, Status, StatusSortKey, PrimeSortKey properties remain the same ...
        // ... INotifyPropertyChanged implementation remains the same ...

        #region Boilerplate Properties (Copy from previous version if needed)
        [JsonProperty("Id")]
        public string Id { get; set; }
        [JsonProperty("Nickname")]
        public string Nickname { get => _nickname; set { _nickname = value; OnPropertyChanged(); } }
        [JsonProperty("Username")]
        public string Username { get => _username; set { _username = value; OnPropertyChanged(); } }
        [JsonProperty("Password")]
        public string Password { get => _password; set { _password = value; OnPropertyChanged(); } }
        [JsonProperty("SteamId64")]
        public string SteamId64 { get => _steamId64; set { _steamId64 = value; OnPropertyChanged(); OnPropertyChanged(nameof(ProfileUrl)); } }
        [JsonProperty("ProfileUrl")]
        public string ProfileUrl { get => string.IsNullOrEmpty(SteamId64) ? null : $"https://steamcommunity.com/profiles/{SteamId64}"; set { } }
        [JsonProperty("IsPrime")]
        public bool IsPrime { get => _isPrime; set { _isPrime = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsPrimeText)); OnPropertyChanged(nameof(PrimeSortKey)); } }
        [JsonIgnore]
        public string IsPrimeText => IsPrime ? "是" : "否";
        [JsonIgnore]
        public string Status
        {
            get
            {
                // 1. 最优先的“正常路径”检查
                if (!IsBanned)
                {
                    return "正常";
                }

                // 2. 如果被封禁，则根据原因进行分支处理
                switch (BanReason)
                {
                    case 1:
                        return "VAC 永久封禁";

                    case 2:
                        // 2a. 冷却状态需要进一步判断时间
                        if (CooldownExpiry > DateTime.Now)
                        {
                            TimeSpan timeLeft = CooldownExpiry - DateTime.Now;
                            // 格式化输出，非常用户友好
                            return $"竞技冷却中 ({timeLeft.Days}天 {timeLeft.Hours}小时)";
                        }
                        else
                        {
                            // 2b. 冷却已过期，状态回归正常
                            return "正常";
                        }

                    default: // 3. ★ 关键的健壮性设计
                        return "未知封禁";
                }
            }
        }

        [JsonIgnore]
        public int StatusSortKey
        {
            get
            {
                if (!IsBanned)
                {
                    return 1; // 正常
                }

                // ★ 使用 if-else if-else 确保逻辑覆盖所有情况
                if (BanReason == 2)
                {
                    if (CooldownExpiry > DateTime.Now)
                    {
                        return 2; // 冷却中
                    }
                    else
                    {
                        return 1; // 冷却过期，视为正常
                    }
                }
                else if (BanReason == 1)
                {
                    return 3; // VAC 永久封禁
                }
                else
                {
                    return 99; // 其他未知封禁类型
                }
            }
        }
        [JsonIgnore]
        public int PrimeSortKey => IsPrime ? 1 : 2;
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}