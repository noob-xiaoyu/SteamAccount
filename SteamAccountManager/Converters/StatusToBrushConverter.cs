// SteamAccountManager/Converters/StatusToBrushConverter.cs

using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SteamAccountManager.Converters
{
    /// <summary>
    /// 将账号的状态字符串（如“正常”、“冷却中”、“封禁”）转换为对应的背景色画刷。
    /// </summary>
    public class StatusToBrushConverter : IValueConverter
    {
        // 预定义颜色画刷，提升性能并方便统一修改
        // 使用您定义的颜色，但去掉了透明度，让颜色更实，或者可以根据喜好调整 Alpha 值 (第一个参数)
        private static readonly Brush NormalBrush = new SolidColorBrush(Color.FromArgb(60, 144, 238, 144));
        private static readonly Brush CooldownBrush = new SolidColorBrush(Color.FromRgb(255, 243, 205)); // 淡黄色 #FFF3CD
        private static readonly Brush BannedBrush = new SolidColorBrush(Color.FromRgb(248, 215, 218));   // 淡红色 #F8D7DA
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 使用 is 模式匹配，更安全
            if (value is not string status)
            {
                return NormalBrush; // 如果值不是字符串，返回默认颜色
            }

            // ★ 关键修复：使用 Contains 进行模糊匹配 ★
            if (status.Contains("冷却中"))
            {
                return CooldownBrush;
            }
            if (status.Contains("封禁")) // "VAC 永久封禁" 和 "游戏封禁" 都能匹配
            {
                return BannedBrush;
            }
            
            // 默认情况（例如 "正常"）返回透明
            return NormalBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 此方法不需要实现
            throw new NotImplementedException();
        }
    }
}