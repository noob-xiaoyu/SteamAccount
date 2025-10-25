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

            // 将 DataContext 设置为账号对象，以便使用数据绑定
            this.DataContext = _accountToEdit;

            InitializeControls();
            LoadAccountData();
        }

        // 初始化控件的选项
        private void InitializeControls()
        {
            BanStatusComboBox.Items.Add("正常");
            BanStatusComboBox.Items.Add("竞技冷却中");
            BanStatusComboBox.Items.Add("VAC 永久封禁");
        }

        // 将账号数据显示在控件中
        private void LoadAccountData()
        {
            if (!_accountToEdit.IsBanned)
            {
                BanStatusComboBox.SelectedItem = "正常";
            }
            else
            {
                BanStatusComboBox.SelectedItem = _accountToEdit.BanReason == 1 ? "VAC 永久封禁" : "竞技冷却中";
            }

            // 如果 CooldownExpiry 是一个有效的日期，则设置它
            if (_accountToEdit.CooldownExpiry > DateTime.MinValue)
            {
                CooldownDatePicker.SelectedDate = _accountToEdit.CooldownExpiry;
            }

            // 根据初始选择，更新日期选择器的可见性
            UpdateDatePickerVisibility();
        }

        // 点击保存按钮时，将控件中的数据写回账号对象
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // 数据绑定会自动更新大部分属性，我们只需处理封禁逻辑
            switch (BanStatusComboBox.SelectedItem.ToString())
            {
                case "正常":
                    _accountToEdit.IsBanned = false;
                    _accountToEdit.BanReason = 0;
                    break;
                case "VAC 永久封禁":
                    _accountToEdit.IsBanned = true;
                    _accountToEdit.BanReason = 1;
                    _accountToEdit.CooldownExpiry = DateTime.MinValue; // 永久封禁没有到期日
                    break;
                case "竞技冷却中":
                    _accountToEdit.IsBanned = true;
                    _accountToEdit.BanReason = 2;
                    // 如果用户没有选择日期，给一个默认值（例如，7天后）
                    _accountToEdit.CooldownExpiry = CooldownDatePicker.SelectedDate ?? DateTime.Now.AddDays(7);
                    break;
            }

            this.DialogResult = true;
        }

        // 当下拉框选项改变时，决定是否显示日期选择器
        private void BanStatusComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateDatePickerVisibility();
        }

        private void UpdateDatePickerVisibility()
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