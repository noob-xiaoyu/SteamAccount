using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace SteamAccountManager.Converters
{
    /// <summary>
    /// 根据输入的状态字符串，将其转换为对应的背景画刷。
    /// 此转换器是可配置的，允许在 XAML 中自定义各种状态对应的颜色。
    /// </summary>
    public class StatusToBrushConverter : IValueConverter
    {
        // 提供公开的 Brush 属性，以便在 XAML 中进行设置，并设置默认值
        public Brush NormalBrush { get; set; } = new SolidColorBrush(Color.FromArgb(60, 144, 238, 144));
        public Brush CooldownBrush { get; set; } = new SolidColorBrush(Color.FromArgb(80, 255, 255, 224));
        public Brush VacBannedBrush { get; set; } = new SolidColorBrush(Color.FromArgb(90, 255, 99, 71));
        public Brush DefaultBrush { get; set; } = Brushes.Transparent;

        // 在构造函数中冻结画刷，这是一个WPF性能优化的最佳实践
        public StatusToBrushConverter()
        {
            // 冻结对象可以提高性能，因为它变得不可变
            NormalBrush.Freeze();
            CooldownBrush.Freeze();
            VacBannedBrush.Freeze();
            // DefaultBrush (Brushes.Transparent) 已经是冻结的
        }

        /// <summary>
        /// 将状态字符串转换为画刷。
        /// </summary>
        /// <param name="value">绑定的状态字符串，如 "正常", "冷却", "VAC"。</param>
        /// <param name="targetType">目标类型，应为 Brush。</param>
        /// <param name="parameter">未使用。</param>
        /// <param name="culture">区域性信息。</param>
        /// <returns>与状态对应的 Brush。</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string status || string.IsNullOrWhiteSpace(status))
            {
                return DefaultBrush;
            }

            string normalizedStatus = status.Trim().ToUpperInvariant();

            // 步骤 3: 使用 if-else if-else 结构进行判断
            if (normalizedStatus == "正常")
            {
                return NormalBrush;
            }
            else if (normalizedStatus == "冷却")
            {
                return CooldownBrush;
            }
            else if (normalizedStatus == "VAC")
            {
                return VacBannedBrush;
            }
            else // 如果以上条件都不满足，返回默认画刷
            {
                return DefaultBrush;
            }
        }

        /// <summary>
        /// 不支持反向转换。
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 对于单向转换器，这是标准做法
            throw new NotSupportedException($"{nameof(StatusToBrushConverter)} does not support ConvertBack.");
        }
    }
}