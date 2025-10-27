using SteamAccountManager.Models;
using SteamAccountManager.ViewModels;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;

namespace SteamAccountManager
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
        private void Run_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://github.com/noob-xiaoyu/SteamAccount",
                UseShellExecute = true
            });
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.DataContext is MainViewModel viewModel)
            {
                viewModel.SaveAllOnExit();
            }
        }
        private void ApiKeyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                string pattern = "[A-F0-9]{32}";
                var match = Regex.Match(textBox.Text, pattern, RegexOptions.IgnoreCase);

                if (match.Success)
                {
                    string extractedKey = match.Value.ToUpper();
                    if (textBox.Text != extractedKey)
                    {
                        textBox.Text = extractedKey;
                        textBox.CaretIndex = extractedKey.Length;
                    }
                }
            }
        }
        private void DataGridRow_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 确保事件源是一个 DataGridRow
            if (sender is DataGridRow row)
            {
                // 如果这一行还没有被选中，就选中它
                if (!row.IsSelected)
                {
                    row.IsSelected = true;
                }
            }
        }
        private void LoginAccountMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem &&
                menuItem.Parent is ContextMenu contextMenu &&
                contextMenu.DataContext is SteamAccount account &&
                this.DataContext is MainViewModel viewModel)
            {
                // 注意：LaunchSteamWithAccount 是异步的，但 Click 事件处理器不能是 async void
                // 所以我们用一种特殊的方式来调用它，忽略 Task 的结果
                _ = viewModel.LaunchSteamWithAccount(account);
            }
        }
        private void ChangeAccountMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // 1. 获取点击的 MenuItem
            if (sender is MenuItem menuItem)
            {
                // 2. 获取该 MenuItem 所在的 ContextMenu
                if (menuItem.Parent is ContextMenu contextMenu)
                {
                    // 3. 获取 ContextMenu 所附加的控件 (DataGridRow) 的 DataContext (SteamAccount)
                    if (contextMenu.DataContext is SteamAccount account)
                    {
                        // 4. 获取 ViewModel 并调用相应的方法，将 account 作为参数传递
                        if (this.DataContext is MainViewModel viewModel)
                        {
                            viewModel.ChangeAccount(account);
                        }
                    }
                }
            }
        }
        private void DeleteAccountMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem &&
                menuItem.Parent is ContextMenu contextMenu &&
                contextMenu.DataContext is SteamAccount account &&
                this.DataContext is MainViewModel viewModel)
            {
                viewModel.DeleteAccount(account);
            }
        }
    }
}