using Transport.Domain.Enums;
using Transport.Presentation.Wpf.ViewModels.Dialogs;

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

    // Form đăng ký ở màn hình đăng nhập; true nếu đã gửi yêu cầu thành công
    bool ShowRegistration();

    // Đổi mật khẩu; requireCurrent = false khi bị bắt đổi lúc đăng nhập. null nếu hủy
    PasswordChange? PromptPasswordChange(bool requireCurrent);

    // Duyệt yêu cầu đăng ký: mật khẩu tạm + role; null nếu hủy
    ApprovalInput? PromptApproval(string title);
}

