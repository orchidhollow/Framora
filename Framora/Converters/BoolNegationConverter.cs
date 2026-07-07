using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Framora.Converters;

/// <summary>
/// 布尔值取反转换器，用于 XAML 中切换可见性等场景。
/// </summary>
public class BoolNegationConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b ? !b : value;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b ? !b : value;
}
