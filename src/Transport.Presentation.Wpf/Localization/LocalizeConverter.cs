using System.Globalization;
using System.Windows.Data;

namespace Transport.Presentation.Wpf.Localization;

// Dịch giá trị động: khóa = ConverterParameter (tiền tố) + giá trị, VD "TicketStatus_" + Paid
public sealed class LocalizeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is null ? string.Empty : LocalizationService.Instance[$"{parameter}{value}"];

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
