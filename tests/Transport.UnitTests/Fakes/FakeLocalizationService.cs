using CommunityToolkit.Mvvm.ComponentModel;
using Transport.Domain.Errors;
using Transport.Presentation.Wpf.Localization;

namespace Transport.UnitTests.Fakes;

// Trả về chính khóa để test kiểm tra "đã dùng đúng khóa"
internal sealed class FakeLocalizationService : ObservableObject, ILocalizationService
{
    public string CurrentLanguage { get; private set; } = "vi";

    public string this[string key] => key;

    public void SetLanguage(string languageCode)
    {
        CurrentLanguage = languageCode;
        OnPropertyChanged(nameof(CurrentLanguage));
    }

    public string Format(string key, params object[] args) =>
        args.Length == 0 ? key : $"{key}|{string.Join(",", args)}";

    public string Translate(BusinessRuleException exception, params object[] args) =>
        Format($"Err_{(int)exception.Code}", args);
}
