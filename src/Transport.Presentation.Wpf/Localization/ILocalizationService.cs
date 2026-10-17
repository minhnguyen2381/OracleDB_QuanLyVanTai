using System.ComponentModel;
using Transport.Domain.Errors;

namespace Transport.Presentation.Wpf.Localization;

public interface ILocalizationService : INotifyPropertyChanged
{
    // Chuỗi đã dịch; thiếu khóa thì trả về chính khóa
    string this[string key] { get; }

    // "vi" hoặc "en"
    string CurrentLanguage { get; }

    void SetLanguage(string languageCode);

    string Format(string key, params object[] args);

    // Dịch lỗi theo khóa Err_<mã>; không có bản dịch thì dùng message tiếng Anh
    string Translate(BusinessRuleException exception, params object[] args);
}
