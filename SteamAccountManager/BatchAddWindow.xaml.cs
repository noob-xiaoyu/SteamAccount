using System.Windows;

namespace SteamAccountManager
{
    public partial class BatchAddWindow : Window
    {
        // 公共属性，用于让主窗口获取文本框内容
        public string AccountsText { get; private set; }

        public BatchAddWindow()
        {
            InitializeComponent();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            AccountsText = AccountsTextBox.Text;
            this.DialogResult = true; // 设置对话框结果为 True，表示用户点击了“确定”
        }
    }
}