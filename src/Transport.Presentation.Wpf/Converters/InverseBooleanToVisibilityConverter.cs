using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Transport.Presentation.Wpf.Converters;

// true -> Collapsed, false -> Visible
public sealed class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? Visibility.Collapsed : Visibility.Visible;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
