# Báo Cáo Triển Khai Task 3: Form đăng ký có mật khẩu, đăng ký xong đăng nhập được ngay

## 1. Tổng quan
- **Mục tiêu**: Người dùng tự đăng ký tài khoản với mật khẩu, tài khoản được tạo ngay trên Oracle với quyền bán vé (`ROLE_TICKET_SELLER`), đăng ký xong ô tài khoản được điền sẵn và có thể đăng nhập ngay mà không cần điều hành phê duyệt.
- **Trạng thái**: Hoàn thành toàn bộ các bước TDD (Step 1 -> 14).
- **Commit**: `d3525ec feat(ui): self-registration with password, sign in right away`
- **Kết quả kiểm thử**: 203 passed, 0 failed, 8 skipped (Oracle live DB skipped khi không có env flag), 0 warning, 0 error.

---

## 2. Chi tiết các bước thực hiện theo TDD

### Bước 1: Impact Analysis (GitNexus)
Đã chạy impact analysis cho các symbol:
- `SubmitAsync` (upstream): LOW risk trên các candidate thuộc Application, Infrastructure, Presentation, Fakes.
- `ShowRegistration` (upstream): LOW risk, callers là `DialogService` và `LoginViewModel`.
- `RegisterViewModel` (upstream): LOW risk, callers là `DialogService.ShowRegistration` và `LoginViewModel.Register`.

### Bước 2: Viết test thất bại (Service)
- Cập nhật `tests/Transport.UnitTests/Fakes/FakeRegistrationRepository.cs`: thay `SubmitAsync` bằng `RegisterAsync(username, password, fullName, phone)`.
- Cập nhật `tests/Transport.UnitTests/Application/RegistrationServiceTests.cs`:
  - `Register_WorksWithoutSignInAndTrimsInput`: kiểm tra trim username, fullName, phone rỗng thành null.
  - `Register_ValidatesBeforeCallingOracle`: kiểm tra username, password (yếu / chứa username), fullName, phone theo đúng các mã lỗi `BusinessErrorCode`.

### Bước 3: Quan sát biên dịch thất bại
- Chạy `dotnet test --project tests/Transport.UnitTests --filter-class "*RegistrationServiceTests"`
- Kết quả: Thất bại ở bước biên dịch với lỗi CS0535 do `FakeRegistrationRepository` chưa implement `SubmitAsync` cũ, và `IRegistrationRepository` / `RegistrationService` chưa có `RegisterAsync`.

### Bước 4: Triển khai Service và Repository
- `src/Transport.Application/Abstractions/IRegistrationRepository.cs`: Khai báo `Task RegisterAsync(string username, string password, string fullName, string? phone, CancellationToken ct = default);`.
- `src/Transport.Application/Services/RegistrationService.cs`: Triển khai `RegisterAsync` với các rule:
  - `Rules.Username(user)`
  - `Rules.Password(password, user)`
  - `Rules.Required(fullName)`
  - `Rules.OptionalPhone(phone)`
  - Gọi repository `RegisterAsync`.
- `src/Transport.Infrastructure/Repositories/RegistrationRepository.cs`: Gọi `pkg_registration_public.register_account` qua `CallAsRegistrarAsync`.

### Bước 5: Viết test thất bại (Dialog & LoginViewModel)
- `tests/Transport.UnitTests/Presentation/DialogViewModelTests.cs`:
  - Thay thế bằng `Register_NeedsMatchingPasswordsThenCloses`: kiểm tra `IsMismatch`, `SubmitCommand.CanExecute`, password matching và lời gọi repository.
  - `Register_ShowsTranslatedErrorAndStaysOpen`: kiểm tra hiển thị lỗi dịch được khi mật khẩu không hợp lệ (`Err_InvalidPassword`) và hộp thoại không đóng.
- `tests/Transport.UnitTests/Fakes/FakeDialogService.cs`:
  - Chuyển `RegistrationResult` thành `string?` (trả về username hoặc null).
  - Cập nhật `ShowRegistration()` trả về `RegistrationResult`.
- `tests/Transport.UnitTests/Presentation/LoginViewModelTests.cs`:
  - Thêm test theory `Register_PrefillsUsernameOnlyWhenRegistered` kiểm tra khi đăng ký thành công thì `Username` được điền sẵn, `InfoMessage` hiển thị "Register_Submitted", `RegistrationCount == 1`.

### Bước 6: Quan sát biên dịch thất bại
- Chạy `dotnet test --project tests/Transport.UnitTests`
- Kết quả: Thất bại ở bước biên dịch do `RegisterViewModel` chưa có các trường mật khẩu và `IDialogService.ShowRegistration` cũ trả về `bool`.

### Bước 7: Triển khai RegisterViewModel
- Cập nhật `src/Transport.Presentation.Wpf/ViewModels/Dialogs/RegisterViewModel.cs`:
  - Thêm observable properties: `Password`, `ConfirmPassword`, `IsMismatch` (`ConfirmPassword.Length > 0 && ConfirmPassword != Password`).
  - Cập nhật `CanSubmit`: kiểm tra username, full name không rỗng, password có độ dài > 0 và `Password == ConfirmPassword`.
  - Cập nhật `SubmitAsync`: gọi `_registrations.RegisterAsync` và đóng dialog với `true` khi thành công, bắt `BusinessRuleException` và hiển thị message dịch được.

### Bước 8: Cập nhật RegisterDialog.xaml
- Thêm namespace `xmlns:c="clr-namespace:Transport.Presentation.Wpf.Controls"`.
- Bổ sung ô nhập `Password` và `ConfirmPassword` sử dụng `PasswordBoxAssistant.BoundPassword`.
- Bổ sung text cảnh báo mật khẩu không khớp (`ChangePassword_Mismatch`) và mô tả quy tắc mật khẩu (`ChangePassword_Rules`).

### Bước 9: Cập nhật IDialogService, DialogService và LoginViewModel
- `src/Transport.Presentation.Wpf/Services/IDialogService.cs`: `string? ShowRegistration();`.
- `src/Transport.Presentation.Wpf/Services/DialogService.cs`: `ShowRegistration()` trả về `viewModel.Username.Trim()` khi người dùng nhấn Đăng ký thành công, ngược lại trả về `null`.
- `src/Transport.Presentation.Wpf/ViewModels/LoginViewModel.cs`: Khi `ShowRegistration()` trả về `username`:
  - Gán `Username = username`.
  - Xóa `Password = string.Empty`.
  - Xóa `ErrorMessage = null`.
  - Đặt `InfoMessage = _localization["Register_Submitted"]`.

### Bước 10: Đồng bộ tài nguyên ngôn ngữ (Strings.resx và Strings.vi.resx)
- Cập nhật giá trị và thêm các khóa mới đảm bảo 100% key parity:
  - `Register_Title`: "Create an account" / "Đăng ký tài khoản"
  - `Register_Hint`: "The new account can sell tickets right away. An operator can grant more permissions later." / "Tài khoản mới dùng được ngay với quyền bán vé. Điều hành có thể cấp thêm quyền sau."
  - `Register_Submit`: "Register" / "Đăng ký"
  - `Register_Password`: "Password" / "Mật khẩu"
  - `Register_ConfirmPassword`: "Confirm password" / "Nhập lại mật khẩu"
  - `Register_Submitted`: "Account created. You can sign in now." / "Đăng ký thành công. Bạn có thể đăng nhập ngay."
  - `Err_20061`: "Too many accounts were registered recently. Please try again later." / "Có quá nhiều tài khoản vừa được đăng ký. Vui lòng thử lại sau."

### Bước 11: Thêm Integration Test
- `tests/Transport.UnitTests/Infrastructure/OracleIntegrationTests.cs`:
  - Thêm `SelfRegistration_CreatesTicketSellerWhoCanSignInRightAway`: tạo user tự đăng ký với prefix `sr_`, kiểm tra đăng nhập ngay lập tức không bị bắt đổi mật khẩu (`PASSWORD EXPIRE`), kiểm tra quyền không phải là Operator (`!IsOperator`), sau đó khóa tài khoản bằng user operator để dọn dẹp.

### Bước 12: Build và chạy toàn bộ test
- `dotnet build`: Build succeeded, 0 Warning(s), 0 Error(s).
- `dotnet test --project tests/Transport.UnitTests`:
  - Passed: 203
  - Failed: 0
  - Skipped: 8 (Oracle live tests khi chưa bật env flag)
  - Toàn bộ ResourceParityTests và ViewInstantiationTests (RegisterDialog) đều PASS.

### Bước 14: Commit với GitNexus
- Chạy `node .gitnexus/run.cjs detect-changes --scope all --repo .`
- Đã git add và commit:
  - Commit hash: `d3525ec`
  - Message: `feat(ui): self-registration with password, sign in right away`
