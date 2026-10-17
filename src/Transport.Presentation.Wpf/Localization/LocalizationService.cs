using System.Globalization;
using System.Resources;
using CommunityToolkit.Mvvm.ComponentModel;
using Transport.Domain.Errors;

namespace Transport.Presentation.Wpf.Localization;

public sealed class LocalizationService : ObservableObject, ILocalizationService
{
    private static readonly ResourceManager Strings =
        new("Transport.Presentation.Wpf.Resources.Strings", typeof(LocalizationService).Assembly);

    private CultureInfo _culture = CultureInfo.GetCultureInfo("vi");

    // Dùng chung cho XAML ({l:Loc ...}) và DI
    public static LocalizationService Instance { get; } = new();

    public string CurrentLanguage => _culture.TwoLetterISOLanguageName;

    public string this[string key] => Strings.GetString(key, _culture) ?? key;

    public void SetLanguage(string languageCode)
    {
        _culture = CultureInfo.GetCultureInfo(languageCode);
        // "Item[]" báo cho mọi binding dạng [Key] cập nhật lại
        OnPropertyChanged("Item[]");
        OnPropertyChanged(nameof(CurrentLanguage));
    }

    public string Format(string key, params object[] args) => SafeFormat(this[key], args);

    public string Translate(BusinessRuleException exception, params object[] args)
    {
        var template = Strings.GetString($"Err_{(int)exception.Code}", _culture);
        return template is null ? exception.Message : SafeFormat(template, args);
    }

    private string SafeFormat(string template, object[] args)
    {
        try
        {
            return string.Format(_culture, template, args);
        }
        catch (FormatException)
        {
            return template;
        }
    }
}
