using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Transport.Presentation.Wpf.Converters;

// null hoặc chuỗi rỗng -> Collapsed
public sealed class NullToVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is null or string { Length: 0 } ? Visibility.Collapsed : Visibility.Visible;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
