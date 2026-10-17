using Transport.Domain.Enums;

namespace Transport.Presentation.Wpf.Services;

public interface IDialogService
{
    // Hộp thoại Có/Không
    bool Confirm(string message);

    // Chọn nơi lưu file .xlsx; null nếu người dùng hủy
    string? ShowSaveFile(string suggestedName);

    // Nhập một dòng chữ (VD lý do hủy); null nếu hủy
    string? PromptText(string title, string label);

    string? PromptPassword(string title);

    PaymentMethod? ChoosePaymentMethod(decimal amount);
}
