// MainWindow.xaml.cs
using SteamAccountManager.Utils;
using SteamAccountManager.ViewModels;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
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
            // 使用默认浏览器打开链接
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 1. 获取 DataContext，也就是我们的 MainViewModel 实例
            if (this.DataContext is MainViewModel viewModel)
            {
                // 2. 调用 ViewModel 中的公共保存方法
                viewModel.SaveAllOnExit();
            }
        }

        private void ApiKeyTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // 获取当前文本框对象
            var textBox = sender as System.Windows.Controls.TextBox;
            if (textBox == null) return;

            // Steam API Key 的格式是 32 个十六进制字符 (0-9, A-F)
            // 我们使用正则表达式来匹配这个格式
            string pattern = "[A-F0-9]{32}";
            var match = Regex.Match(textBox.Text, pattern, RegexOptions.IgnoreCase);

            // 如果成功找到了匹配项
            if (match.Success)
            {
                // 提取出纯净的 Key
                string extractedKey = match.Value.ToUpper(); // 统一转为大写

                // 只有当文本框的当前内容不等于纯净 Key 时才更新它
                // 这是为了防止在更新文本后再次触发 TextChanged 事件，从而导致死循环
                if (textBox.Text != extractedKey)
                {
                    // 用纯净的 Key 替换整个文本框的内容
                    textBox.Text = extractedKey;

                    // 将光标移动到文本的末尾，方便用户继续操作
                    textBox.CaretIndex = extractedKey.Length;
                }
            }
        }
    }
}