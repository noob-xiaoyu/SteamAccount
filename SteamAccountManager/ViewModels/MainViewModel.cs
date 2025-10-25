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
        private readonly SteamApiService _steamApiService;
        private readonly string _accountsFilePath;
        private readonly string _configFilePath;
        #endregion

        #region Properties
        private ObservableCollection<SteamAccount> _accounts;
        public ObservableCollection<SteamAccount> Accounts { get => _accounts; private set { _accounts = value; OnPropertyChanged(); } }
        public ICollectionView AccountsView { get; private set; }

        private SteamAccount _selectedAccount;
        public SteamAccount SelectedAccount { get => _selectedAccount; set { _selectedAccount = value; OnPropertyChanged(); } }

        private string _steamApiKey;
        public string SteamApiKey { get => _steamApiKey; set { _steamApiKey = value; OnPropertyChanged(); } }

        private string _steamExePath;
        public string SteamExePath { get => _steamExePath; set { _steamExePath = value; OnPropertyChanged(); } }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (_isBusy == value) return;
                _isBusy = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private string _statusMessage;
        public string StatusMessage { get => _statusMessage; private set { _statusMessage = value; OnPropertyChanged(); } }
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
            _accountsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "accounts.json");
            _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
            _steamApiService = new SteamApiService();

            Accounts = new ObservableCollection<SteamAccount>();
            AccountsView = CollectionViewSource.GetDefaultView(Accounts);

            InitializeCommands();
            InitializeData();
        }
        #endregion

        #region Initialization
        private void InitializeCommands()
        {
            BatchAddCommand = new RelayCommand(ShowBatchAddDialog, _ => !IsBusy);
            ChangeAccountCommand = new RelayCommand(ChangeSelectedAccount, _ => SelectedAccount != null && !IsBusy);
            DeleteAccountCommand = new RelayCommand(DeleteSelectedAccount, _ => SelectedAccount != null && !IsBusy);
            LaunchSteamLoginCommand = new RelayCommand(async _ => await LaunchSteamWithSelectedAccount(), _ => SelectedAccount != null && !IsBusy);
            UpdateNicknamesCommand = new RelayCommand(async _ => await UpdateAllNicknamesAsync(), _ => CanExecuteApiCommand());
            UpdateBanStatusCommand = new RelayCommand(async _ => await UpdateAllBanStatusAsync(), _ => CanExecuteApiCommand());
        }

        private void InitializeData()
        {
            StatusMessage = "正在初始化...";
            SetupSorting();
            LoadSettings();
            LoadAccounts();
            StatusMessage = "就绪";
        }
        #endregion

        #region Public Methods
        public void SaveAllOnExit()
        {
            SaveAccounts();
            SaveSettings();
        }
        #endregion

        #region File Operations
        private void LoadSettings()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    var json = File.ReadAllText(_configFilePath);
                    var settings = JsonConvert.DeserializeObject<AppSettings>(json);
                    SteamApiKey = settings.SteamApiKey;
                }
                else
                {
                    SteamApiKey = "";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] 加载设置失败: {ex.Message}");
                SteamApiKey = "";
            }
            finally
            {
                SteamExePath = RegistryHelper.GetSteamPath() ?? @"C:\Program Files (x86)\Steam\steam.exe";
            }
        }

        private void LoadAccounts()
        {
            try
            {
                if (!File.Exists(_accountsFilePath))
                {
                    File.WriteAllText(_accountsFilePath, "[]");
                    StatusMessage = "未找到账号文件，已自动创建。";
                    return;
                }

                var jsonContent = File.ReadAllText(_accountsFilePath);
                var loadedAccounts = JsonConvert.DeserializeObject<List<SteamAccount>>(jsonContent);

                Accounts.Clear();
                foreach (var acc in loadedAccounts)
                {
                    Accounts.Add(acc);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载账号文件失败: {ex.Message}", "加载错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveSettings()
        {
            try
            {
                var settings = new AppSettings { SteamApiKey = this.SteamApiKey };
                var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(_configFilePath, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] 保存设置失败: {ex.Message}");
            }
        }

        private void SaveAccounts()
        {
            try
            {
                if (Accounts != null && Accounts.Any())
                {
                    var json = JsonConvert.SerializeObject(Accounts, Formatting.Indented);
                    File.WriteAllText(_accountsFilePath, json);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] 保存账号失败: {ex.Message}");
            }
        }

        private void SetupSorting()
        {
            AccountsView.SortDescriptions.Clear();
            AccountsView.SortDescriptions.Add(new SortDescription(nameof(SteamAccount.StatusSortKey), ListSortDirection.Ascending));
            AccountsView.SortDescriptions.Add(new SortDescription(nameof(SteamAccount.PrimeSortKey), ListSortDirection.Ascending));
            AccountsView.SortDescriptions.Add(new SortDescription(nameof(SteamAccount.Nickname), ListSortDirection.Ascending));
        }
        #endregion

        #region Command Implementations
        private void ShowBatchAddDialog(object parameter)
        {
            var dialog = new BatchAddWindow();
            dialog.Owner = Application.Current.MainWindow;

            if (dialog.ShowDialog() == true)
            {
                var text = dialog.AccountsText;
                if (string.IsNullOrWhiteSpace(text)) return;

                var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                int successCount = 0;
                int errorCount = 0;

                foreach (var line in lines)
                {
                    try
                    {
                        SteamAccount newAccount = null;

                        if (line.Contains("----"))
                        {
                            var parts = line.Split(new[] { "----" }, StringSplitOptions.None);
                            if (parts.Length >= 2)
                            {
                                var usernamePart = parts[0].Trim();
                                if (usernamePart.Contains(":")) usernamePart = usernamePart.Split(':')[1].Trim();
                                if (usernamePart.Contains("：")) usernamePart = usernamePart.Split('：')[1].Trim();

                                newAccount = new SteamAccount
                                {
                                    Id = Guid.NewGuid().ToString(),
                                    Username = usernamePart,
                                    Password = parts[1].Trim(),
                                    Nickname = usernamePart,
                                    Email = parts.Length > 2 ? parts[2].Trim() : "",
                                    EmailPassword = parts.Length > 3 ? parts[3].Trim() : "",
                                    IsPrime = false,
                                    IsBanned = false
                                };
                            }
                        }
                        else if (line.Contains(","))
                        {
                            var parts = line.Split(',');
                            if (parts.Length == 3)
                            {
                                newAccount = new SteamAccount
                                {
                                    Id = Guid.NewGuid().ToString(),
                                    Username = parts[0].Trim(),
                                    Password = parts[1].Trim(),
                                    Nickname = parts[2].Trim(),
                                    IsPrime = false,
                                    IsBanned = false
                                };
                            }
                        }

                        if (newAccount != null)
                        {
                            Accounts.Add(newAccount);
                            successCount++;
                        }
                        else
                        {
                            errorCount++;
                        }
                    }
                    catch
                    {
                        errorCount++;
                    }
                }
                MessageBox.Show($"批量添加完成！\n成功添加: {successCount} 个\n格式错误: {errorCount} 个", "操作结果", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ChangeSelectedAccount(object parameter)
        {
            if (SelectedAccount == null) return;

            var editWindow = new EditAccountWindow(SelectedAccount);
            editWindow.Owner = Application.Current.MainWindow;
            editWindow.ShowDialog();
        }

        private void DeleteSelectedAccount(object parameter)
        {
            if (SelectedAccount == null) return;

            var result = MessageBox.Show($"确定要删除账号 '{SelectedAccount.Nickname}' ({SelectedAccount.Username}) 吗？\n此操作不可撤销！", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                Accounts.Remove(SelectedAccount);
            }
        }

        private async Task LaunchSteamWithSelectedAccount()
        {
            if (SelectedAccount == null) return;

            if (string.IsNullOrEmpty(SteamExePath) || !File.Exists(SteamExePath))
            {
                MessageBox.Show("Steam.exe 路径无效或未找到！", "路径错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (ProcessHelper.IsSteamRunning())
            {
                var result = MessageBox.Show(
                    "Steam 正在运行中。\n为了使用新账号登录，需要先关闭当前的 Steam 进程。\n\n是否继续？",
                    "确认操作",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    return;
                }

                ProcessHelper.KillSteamProcesses();
                await Task.Delay(2000);
            }

            string username = SelectedAccount.Username;
            string password = SelectedAccount.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("选中账号的用户名或密码为空，无法登录。", "信息不完整", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = SteamExePath,
                    Arguments = $"-login \"{username}\" \"{password}\""
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启动 Steam 失败: {ex.Message}", "启动失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task UpdateAllNicknamesAsync()
        {
            await ExecuteApiTask(async (ids, apiKey) =>
            {
                StatusMessage = $"正在更新 {ids.Count} 个账号的昵称...";
                var nicknames = await _steamApiService.GetPlayerNicknamesAsync(ids, apiKey);
                int updatedCount = 0;
                foreach (var account in Accounts.Where(a => !string.IsNullOrEmpty(a.SteamId64)))
                {
                    if (nicknames.TryGetValue(account.SteamId64, out var newNickname) && account.Nickname != newNickname)
                    {
                        account.Nickname = newNickname;
                        updatedCount++;
                    }
                }
                StatusMessage = $"昵称更新完成！共更新了 {updatedCount} 个账号。";
            }, "昵称更新");
        }

        private async Task UpdateAllBanStatusAsync()
        {
            var result = MessageBox.Show("获取不了时间是否继续？", "确认操作", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.No) { return; } 
            await ExecuteApiTask(async (ids, apiKey) =>
            {
                StatusMessage = $"正在更新 {ids.Count} 个账号的封禁状态...";
                var banStatuses = await _steamApiService.GetPlayerBansAsync(ids, apiKey);
                int updatedCount = 0;
                foreach (var account in Accounts.Where(a => !string.IsNullOrEmpty(a.SteamId64)))
                {
                    if (banStatuses.TryGetValue(account.SteamId64, out var banStatus))
                    {
                        // account.UpdateBanStatusFromApi(banStatus.VACBanned, banStatus.NumberOfGameBans);
                        updatedCount++;
                    }
                }
                StatusMessage = $"封禁状态更新完成！共检查了 {updatedCount} 个账号。";
            }, "封禁状态更新");
        }
        #endregion

        #region Private Helper Methods
        private bool CanExecuteApiCommand()
        {
            return !IsBusy &&
                   !string.IsNullOrWhiteSpace(SteamApiKey) &&
                   Accounts.Any(acc => !string.IsNullOrEmpty(acc.SteamId64));
        }

        private async Task ExecuteApiTask(Func<List<string>, string, Task> apiAction, string taskName)
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
                if (steamIds.Any())
                {
                    await apiAction(steamIds, this.SteamApiKey);
                }
                else
                {
                    StatusMessage = "列表中没有找到有效的 SteamID。";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"{taskName}失败: {ex.Message}";
                MessageBox.Show($"在执行“{taskName}”时发生错误:\n\n{ex.Message}", "API 错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }
        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
