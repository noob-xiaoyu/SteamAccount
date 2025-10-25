using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media; // 确保引用的是 WPF 的 Media 命名空间

// 命名空间要和你的项目结构完全一致
namespace SteamAccountManager.Converters
{
    public class StatusToBrushConverter : IValueConverter
    {
        // 预定义颜色画刷，提升性能
        private static readonly Brush NormalBrush = new SolidColorBrush(Color.FromArgb(60, 144, 238, 144)); // 淡绿色
        private static readonly Brush CooldownBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 224)); // 淡黄色
        private static readonly Brush VacBannedBrush = new SolidColorBrush(Color.FromArgb(90, 255, 99, 71));   // 淡红色

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string status)
            {
                return Brushes.Transparent;
            }

            return status.Trim() switch
            {
                "正常" => NormalBrush,
                "冷却" => CooldownBrush,
                "VAC" => VacBannedBrush,
                _ => Brushes.Transparent,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}