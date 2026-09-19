using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MapleWindow.App.Converters;

/// <summary>Visible when the bound enum value's ToString() matches ConverterParameter, Collapsed otherwise — used to switch between wizard-style steps in XAML without extra computed ViewModel properties.</summary>
public sealed class EnumEqualsToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value?.ToString() == parameter?.ToString() ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
