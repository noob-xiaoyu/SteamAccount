using SteamAccountManager.Models;
using System;
using System.Windows;
using System.Windows.Controls;

namespace SteamAccountManager
{
    public partial class EditAccountWindow : Window
    {
        private readonly SteamAccount _accountToEdit;

        public EditAccountWindow(SteamAccount account)
        {
            InitializeComponent();
            _accountToEdit = account;
            this.DataContext = _accountToEdit;
            InitializeControls();
            LoadAccountData();
        }

        private void InitializeControls()
        {
            BanStatusComboBox.Items.Add("正常");
            BanStatusComboBox.Items.Add("竞技冷却中");
            BanStatusComboBox.Items.Add("VAC 永久封禁");
        }

        private void LoadAccountData()
        {
            // 设置封禁状态的初始选项
            if (!_accountToEdit.IsBanned)
            {
                BanStatusComboBox.SelectedItem = "正常";
            }
            else
            {
                BanStatusComboBox.SelectedItem = _accountToEdit.BanReason == 1 ? "VAC 永久封禁" : "竞技冷却中";
            }

            // ★ 关键修改：计算剩余天数并填充到 TextBox ★
            // 检查冷却到期时间是否在未来
            if (_accountToEdit.CooldownExpiry > DateTime.Now)
            {
                // 计算剩余的总天数（向上取整）
                var remainingDays = (int)Math.Ceiling((_accountToEdit.CooldownExpiry - DateTime.Now).TotalDays);
                CooldownDaysTextBox.Text = remainingDays.ToString();
            }
            else
            {
                // 如果已过期或无冷却，显示 0
                CooldownDaysTextBox.Text = "0";
            }

            // 根据初始选择，更新输入框的可见性
            UpdateCooldownPanelVisibility();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // 数据绑定会自动更新大部分属性，我们只需处理封禁逻辑
            switch (BanStatusComboBox.SelectedItem.ToString())
            {
                case "正常":
                    _accountToEdit.IsBanned = false;
                    _accountToEdit.BanReason = 0;
                    _accountToEdit.CooldownExpiry = DateTime.MinValue; // 清除冷却时间
                    break;
                case "VAC 永久封禁":
                    _accountToEdit.IsBanned = true;
                    _accountToEdit.BanReason = 1;
                    _accountToEdit.CooldownExpiry = DateTime.MinValue; // 永久封禁没有到期日
                    break;
                case "竞技冷却中":
                    _accountToEdit.IsBanned = true;
                    _accountToEdit.BanReason = 2;

                    // ★ 关键修改：从 TextBox 读取天数并计算新的到期日期 ★
                    int daysToAdd = 7; // 设置一个默认值，以防输入无效
                    if (int.TryParse(CooldownDaysTextBox.Text, out int parsedDays) && parsedDays >= 0)
                    {
                        daysToAdd = parsedDays;
                    }
                    // 计算新的到期时间
                    _accountToEdit.CooldownExpiry = DateTime.Now.AddDays(daysToAdd);
                    break;
            }

            this.DialogResult = true;
        }

        private void BanStatusComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateCooldownPanelVisibility();
        }

        // 方法名已更新，更准确
        private void UpdateCooldownPanelVisibility()
        {
            if (BanStatusComboBox.SelectedItem?.ToString() == "竞技冷却中")
            {
                CooldownPanel.Visibility = Visibility.Visible;
            }
            else
            {
                CooldownPanel.Visibility = Visibility.Collapsed;
            }
        }
    }
}