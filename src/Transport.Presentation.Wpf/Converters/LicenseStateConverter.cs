using System.Globalization;
using System.Windows.Data;
using Transport.Domain.Entities;
using Transport.Domain.Enums;

namespace Transport.Presentation.Wpf.Converters;

// Driver -> LicenseState theo ngày hôm nay, dùng để tô màu dòng
public sealed class LicenseStateConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is Driver driver ? driver.GetLicenseState(DateTime.Today) : LicenseState.Valid;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
