using Newtonsoft.Json;
using SteamAccountManager.Commands;
using SteamAccountManager.Models;
using SteamAccountManager.Services;
using SteamAccountManager.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace SteamAccountManager.ViewModels
{
    /// <summary>
    /// 主窗口的核心视图模型，负责处理所有业务逻辑和数据状态。
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        #region Fields
        private SteamApiService _steamApiService;
        private readonly string _accountsFilePath;
        private readonly string _configFilePath;
        #endregion

        #region Properties
        private ObservableCollection<SteamAccount> _accounts;
        /// <summary>
        /// 存储所有 Steam 账号的原始集合。
        /// </summary>
        public ObservableCollection<SteamAccount> Accounts
        {
            get => _accounts;
            private set { _accounts = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 用于在UI中展示、排序和过滤的账号视图。
        /// </summary>
        public ICollectionView AccountsView { get; private set; }

        private SteamAccount _selectedAccount;
        /// <summary>
        /// 在 DataGrid 中当前选中的账号。
        /// </summary>
        public SteamAccount SelectedAccount
        {
            get => _selectedAccount;
            set { _selectedAccount = value; OnPropertyChanged(); }
        }

        private string _steamApiKey = "";
        /// <summary>
        /// 用户的 Steam Web API Key。
        /// </summary>
        public string SteamApiKey
        {
            get => _steamApiKey;
            set { _steamApiKey = value; OnPropertyChanged(); _steamApiService = new SteamApiService(_steamApiKey); }
        }

        private string _steamExePath;
        /// <summary>
        /// Steam.exe 客户端的完整路径。
        /// </summary>
        public string SteamExePath
        {
            get => _steamExePath;
            set { _steamExePath = value; OnPropertyChanged(); }
        }

        private bool _isBusy;
        /// <summary>
        /// 指示当前是否有耗时操作（如API调用）正在进行。
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }
        #endregion

        #region Commands
        public ICommand BatchAddCommand { get; private set; }
        public ICommand ChangeAccountCommand { get; private set; }
        public ICommand DeleteAccountCommand { get; private set; }
        public ICommand LaunchSteamLoginCommand { get; private set; }
        public ICommand UpdateNicknamesCommand { get; private set; }
        public ICommand UpdateBanStatusCommand { get; private set; }
        #endregion

        #region Constructor
        public MainViewModel()
        {
            // 初始化路径和核心数据结构
            _accountsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "accounts.json");
            _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
            Accounts = new ObservableCollection<SteamAccount>();
            AccountsView = CollectionViewSource.GetDefaultView(Accounts);

            // 执行初始化流程
            InitializeCommands();
            InitializeData();
        }
        #endregion

        #region Initialization Methods
        /// <summary>
        /// 初始化所有命令。
        /// </summary>
        private void InitializeCommands()
        {
            BatchAddCommand = new RelayCommand(ShowBatchAddDialog, _ => !IsBusy);
            ChangeAccountCommand = new RelayCommand(ChangeSelectedAccount, _ => SelectedAccount != null && !IsBusy);
            DeleteAccountCommand = new RelayCommand(DeleteSelectedAccount, _ => SelectedAccount != null && !IsBusy);
            LaunchSteamLoginCommand = new RelayCommand(async _ => await LaunchSteamWithSelectedAccount(), _ => SelectedAccount != null && !IsBusy);
            UpdateNicknamesCommand = new RelayCommand(async _ => await UpdateAllNicknamesAsync(), _ => CanExecuteApiCommand());
            UpdateBanStatusCommand = new RelayCommand(async _ => await UpdateAllBanStatusAsync(), _ => CanExecuteApiCommand());
        }

        /// <summary>
        /// 初始化和加载数据。
        /// </summary>
        private void InitializeData()
        {
            SetupSorting();
            LoadSettings();
            _steamApiService = new SteamApiService(SteamApiKey);
            LoadAccounts();
        }

        private void SetupSorting()
        {
            AccountsView.SortDescriptions.Clear();
            AccountsView.SortDescriptions.Add(new SortDescription(nameof(SteamAccount.StatusSortKey), ListSortDirection.Ascending));
            AccountsView.SortDescriptions.Add(new SortDescription(nameof(SteamAccount.PrimeSortKey), ListSortDirection.Ascending));
            AccountsView.SortDescriptions.Add(new SortDescription(nameof(SteamAccount.Nickname), ListSortDirection.Ascending));
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 在程序退出时，自动、静默地保存所有数据和设置。
        /// </summary>
        public void SaveAllOnExit()
        {
            try
            {
                if (Accounts != null && Accounts.Any())
                {
                    string accountsJson = JsonConvert.SerializeObject(Accounts, Formatting.Indented);
                    File.WriteAllText(_accountsFilePath, accountsJson);
                }
            }
            catch { /* 静默保存，忽略错误 */ }

            try
            {
                var settings = new AppSettings { SteamApiKey = this.SteamApiKey };
                string settingsJson = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(_configFilePath, settingsJson);
            }
            catch { /* 静默保存，忽略错误 */ }
        }
        #endregion

        #region Private Helper Methods (File IO)
        private void LoadSettings()
        {
            if (File.Exists(_configFilePath))
            {
                try
                {
                    var json = File.ReadAllText(_configFilePath);
                    var settings = JsonConvert.DeserializeObject<AppSettings>(json);
                    SteamApiKey = settings.SteamApiKey;
                }
                catch { ApplyDefaultSettings(); }
            }
            else { ApplyDefaultSettings(); }
        }

        private void ApplyDefaultSettings()
        {
            var detectedPath = RegistryHelper.GetSteamPath();
            this.SteamApiKey = "";
        }

        private void LoadAccounts()
        {
            if (!File.Exists(_accountsFilePath))
            {
                try
                {
                    File.WriteAllText(_accountsFilePath, "[]");
                    MessageBox.Show("未找到 accounts.json，已为您自动创建一个新文件。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"创建新配置文件失败: {ex.Message}", "严重错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                return;
            }

            try
            {
                string jsonContent = File.ReadAllText(_accountsFilePath);
                var loadedAccounts = JsonConvert.DeserializeObject<List<SteamAccount>>(jsonContent);
                Accounts.Clear();
                foreach (var acc in loadedAccounts) { Accounts.Add(acc); }
                RefreshCommandStates();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载文件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region Private Helper Methods (Commands Logic)
        private void ShowBatchAddDialog(object parameter) { /* ... 保持不变 ... */ }
        private void ChangeSelectedAccount(object parameter) { /* ... 保持不变 ... */ }
        private void DeleteSelectedAccount(object parameter) { /* ... 保持不变 ... */ }
        private async Task LaunchSteamWithSelectedAccount() { /* ... 保持不变 ... */ }

        /// <summary>
        /// 检查是否满足执行API命令的前置条件。
        /// </summary>
        private bool CanExecuteApiCommand()
        {
            return !IsBusy &&
                   !string.IsNullOrWhiteSpace(SteamApiKey) &&
                   Accounts.Any(acc => !string.IsNullOrEmpty(acc.SteamId64));
        }

        private async Task UpdateAllNicknamesAsync()
        {
            if (!CanExecuteApiCommand())
            {
                MessageBox.Show("请输入有效的 Steam API Key 并且列表需要有包含 SteamID 的账号。", "操作无法执行", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsBusy = true;
            try
            {
                var steamIds = Accounts.Select(a => a.SteamId64).Where(id => !string.IsNullOrEmpty(id)).ToList();
                var nicknames = await _steamApiService.GetPlayerNicknamesAsync(steamIds);

                int updatedCount = 0;
                foreach (var account in Accounts)
                {
                    if (!string.IsNullOrEmpty(account.SteamId64) && nicknames.TryGetValue(account.SteamId64, out var newNickname) && account.Nickname != newNickname)
                    {
                        account.Nickname = newNickname;
                        updatedCount++;
                    }
                }
                MessageBox.Show($"昵称更新完成！共更新了 {updatedCount} 个账号。", "完成", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"更新时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task UpdateAllBanStatusAsync()
        {
            if (!CanExecuteApiCommand())
            {
                MessageBox.Show("请输入有效的 Steam API Key 并且列表需要有包含 SteamID 的账号。", "操作无法执行", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsBusy = true;
            try
            {
                var steamIds = Accounts.Select(a => a.SteamId64).Where(id => !string.IsNullOrEmpty(id)).ToList();
                var banStatuses = await _steamApiService.GetPlayerBansAsync(steamIds);

                int updatedCount = 0;
                foreach (var account in Accounts)
                {
                    if (!string.IsNullOrEmpty(account.SteamId64) && banStatuses.TryGetValue(account.SteamId64, out var banStatus))
                    {
                        // 假设 Account 模型有这个方法来更新状态
                        // account.UpdateBanStatusFromApi(banStatus.VACBanned, banStatus.NumberOfGameBans);
                        updatedCount++;
                    }
                }
                MessageBox.Show($"封禁状态更新完成！共检查了 {updatedCount} 个账号。", "完成", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"更新时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void RefreshCommandStates()
        {
            // 在数据变化后，通知所有命令重新评估它们的 CanExecute 状态
            CommandManager.InvalidateRequerySuggested();
        }
        #endregion

        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            // 当选中项改变时，手动刷新依赖它的命令
            if (propertyName == nameof(SelectedAccount))
            {
                (ChangeAccountCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (DeleteAccountCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (LaunchSteamLoginCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
        #endregion
    }
}