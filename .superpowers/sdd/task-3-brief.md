### Task 3: Form đăng ký có mật khẩu, đăng ký xong đăng nhập được ngay

**Files:**
- Modify: `src/Transport.Application/Abstractions/IRegistrationRepository.cs`, `src/Transport.Application/Services/RegistrationService.cs`, `src/Transport.Infrastructure/Repositories/RegistrationRepository.cs`
- Modify: `src/Transport.Presentation.Wpf/ViewModels/Dialogs/RegisterViewModel.cs`, `Views/Dialogs/RegisterDialog.xaml`
- Modify: `src/Transport.Presentation.Wpf/Services/IDialogService.cs:21-22`, `Services/DialogService.cs:56`, `ViewModels/LoginViewModel.cs:70-77`
- Modify: `src/Transport.Presentation.Wpf/Resources/Strings.resx`, `Strings.vi.resx`
- Test: `tests/Transport.UnitTests/Fakes/FakeRegistrationRepository.cs`, `Fakes/FakeDialogService.cs`, `Application/RegistrationServiceTests.cs`, `Presentation/DialogViewModelTests.cs`, `Presentation/LoginViewModelTests.cs:111-123`, `Infrastructure/OracleIntegrationTests.cs`

**Interfaces:**
- Consumes: `pkg_registration_public.register_account(p_username, p_password, p_full_name, p_phone)` (Task 1); `RegistrationService(IRegistrationRepository)` (Task 2).
- Produces: `IRegistrationRepository.RegisterAsync(string username, string password, string fullName, string? phone, CancellationToken ct = default)`.
- Produces: `RegistrationService.RegisterAsync(string? username, string? password, string? fullName, string? phone, CancellationToken ct = default)`.
- Produces: `IDialogService.ShowRegistration()` trả `string?` (username vừa tạo đã trim; `null` nếu hủy).
- Produces: `RegisterViewModel.Password`, `ConfirmPassword`, `IsMismatch`.

- [ ] **Step 1: Impact analysis**

```bash
node .gitnexus/run.cjs impact "SubmitAsync" --direction upstream --repo .
node .gitnexus/run.cjs impact "ShowRegistration" --direction upstream --repo .
node .gitnexus/run.cjs impact "RegisterViewModel" --direction upstream --repo .
```

Caller dự kiến: `RegisterViewModel`, `DialogService`, `LoginViewModel` và các test, fake.

- [ ] **Step 2: Viết test thất bại – service**

`tests/Transport.UnitTests/Fakes/FakeRegistrationRepository.cs`:

```csharp
using Transport.Application.Abstractions;

namespace Transport.UnitTests.Fakes;

internal sealed class FakeRegistrationRepository : IRegistrationRepository
{
    public List<string> Calls { get; } = [];

    public Task RegisterAsync(string username, string password, string fullName, string? phone, CancellationToken ct = default)
    {
        Calls.Add($"register:{username}:{password}:{fullName}:{phone}");
        return Task.CompletedTask;
    }
}
```

`tests/Transport.UnitTests/Application/RegistrationServiceTests.cs` (thay toàn bộ):

```csharp
using Transport.Application.Services;
using Transport.Domain.Errors;
using Transport.UnitTests.Fakes;

namespace Transport.UnitTests.Application;

public class RegistrationServiceTests
{
    private readonly FakeRegistrationRepository _repository = new();

    private RegistrationService Create() => new(_repository);

    [Fact]
    public async Task Register_WorksWithoutSignInAndTrimsInput()
    {
        await Create().RegisterAsync(" newuser01 ", "Secret#123", " Người Mới ", " ");

        Assert.Equal(new[] { "register:newuser01:Secret#123:Người Mới:" }, _repository.Calls);
    }

    [Theory]
    [InlineData(null, "Secret#123", "Tên", null, BusinessErrorCode.InvalidUsername)]
    [InlineData("ab", "Secret#123", "Tên", null, BusinessErrorCode.InvalidUsername)]
    [InlineData("x; drop", "Secret#123", "Tên", null, BusinessErrorCode.InvalidUsername)]
    [InlineData("newuser01", null, "Tên", null, BusinessErrorCode.InvalidPassword)]
    [InlineData("newuser01", "weakpass", "Tên", null, BusinessErrorCode.InvalidPassword)]
    [InlineData("newuser01", "Newuser01#", "Tên", null, BusinessErrorCode.InvalidPassword)]
    [InlineData("newuser01", "Secret#123", " ", null, BusinessErrorCode.RequiredField)]
    [InlineData("newuser01", "Secret#123", "Tên", "123", BusinessErrorCode.InvalidPhone)]
    public async Task Register_ValidatesBeforeCallingOracle(
        string? username, string? password, string fullName, string? phone, BusinessErrorCode expected)
    {
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => Create().RegisterAsync(username, password, fullName, phone));

        Assert.Equal(expected, ex.Code);
        Assert.Empty(_repository.Calls);
    }
}
```

- [ ] **Step 3: Chạy test để thấy thất bại**

Run: `dotnet test --project tests/Transport.UnitTests --filter-class "*RegistrationServiceTests"`
Expected: FAIL khi biên dịch (`RegistrationService` chưa có `RegisterAsync`, `IRegistrationRepository` chưa có `RegisterAsync`).

- [ ] **Step 4: Cài đặt service và repository**

`src/Transport.Application/Abstractions/IRegistrationRepository.cs`:

```csharp
namespace Transport.Application.Abstractions;

// Gọi PKG_REGISTRATION_PUBLIC bằng user TRANSPORT_REGISTRAR (chưa đăng nhập)
public interface IRegistrationRepository
{
    Task RegisterAsync(string username, string password, string fullName, string? phone, CancellationToken ct = default);
}
```

`src/Transport.Application/Services/RegistrationService.cs`:

```csharp
using Transport.Application.Abstractions;

namespace Transport.Application.Services;

// Tự đăng ký tài khoản bán vé: Oracle tạo user ngay, không cần điều hành duyệt
public sealed class RegistrationService(IRegistrationRepository registrations)
{
    // Không cần đăng nhập; repository gọi bằng user TRANSPORT_REGISTRAR.
    // Mật khẩu phải đạt luật PROFILE_STAFF (báo sớm ở đây, Oracle kiểm tra lại bằng verify function)
    public Task RegisterAsync(string? username, string? password, string? fullName, string? phone, CancellationToken ct = default)
    {
        var user = username?.Trim();
        Rules.Username(user);
        Rules.Password(password, user);
        Rules.Required(fullName);
        Rules.OptionalPhone(phone);
        return registrations.RegisterAsync(user!, password!, fullName!.Trim(), Rules.TrimToNull(phone), ct);
    }
}
```

`src/Transport.Infrastructure/Repositories/RegistrationRepository.cs`:

```csharp
using Transport.Application.Abstractions;
using Transport.Infrastructure.Persistence;

namespace Transport.Infrastructure.Repositories;

// Tự đăng ký bằng user REGISTRAR qua PKG_REGISTRATION_PUBLIC
internal sealed class RegistrationRepository(OracleConnectionFactory factory) : OracleRepository(factory), IRegistrationRepository
{
    public Task RegisterAsync(string username, string password, string fullName, string? phone, CancellationToken ct = default) =>
        CallAsRegistrarAsync("pkg_registration_public.register_account",
            new { p_username = username, p_password = password, p_full_name = fullName, p_phone = phone }, ct);
}
```

- [ ] **Step 5: Viết test thất bại – hộp thoại và màn hình đăng nhập**

`tests/Transport.UnitTests/Presentation/DialogViewModelTests.cs`: thay hai test `Register_SubmitsAndCloses` và `Register_ShowsTranslatedErrorAndStaysOpen` bằng:

```csharp
    [Fact]
    public async Task Register_NeedsMatchingPasswordsThenCloses()
    {
        var repository = new FakeRegistrationRepository();
        var dialog = new RegisterViewModel(new RegistrationService(repository), new FakeLocalizationService());
        bool? result = null;
        dialog.CloseRequested += r => result = r;

        dialog.Username = "newuser01";
        dialog.FullName = "Người Mới";
        dialog.Password = "Secret#123";
        dialog.ConfirmPassword = "Secret#12";
        Assert.True(dialog.IsMismatch);
        Assert.False(dialog.SubmitCommand.CanExecute(null));

        dialog.ConfirmPassword = "Secret#123";
        Assert.False(dialog.IsMismatch);
        await dialog.SubmitCommand.ExecuteAsync(null);

        Assert.True(result);
        Assert.Equal(new[] { "register:newuser01:Secret#123:Người Mới:" }, repository.Calls);
    }

    [Fact]
    public async Task Register_ShowsTranslatedErrorAndStaysOpen()
    {
        var dialog = new RegisterViewModel(new RegistrationService(new FakeRegistrationRepository()), new FakeLocalizationService())
        {
            Username = "newuser01",
            FullName = "Người Mới",
            Password = "weakpass",
            ConfirmPassword = "weakpass",
        };
        bool? result = null;
        dialog.CloseRequested += r => result = r;

        await dialog.SubmitCommand.ExecuteAsync(null);

        Assert.Null(result);
        Assert.Equal($"Err_{(int)BusinessErrorCode.InvalidPassword}", dialog.ErrorMessage);
    }
```

`tests/Transport.UnitTests/Fakes/FakeDialogService.cs`:
- Đổi `public bool RegistrationResult { get; set; }` thành `public string? RegistrationResult { get; set; }`.
- Đổi method thành:

```csharp
    public string? ShowRegistration()
    {
        RegistrationCount++;
        return RegistrationResult;
    }
```

`tests/Transport.UnitTests/Presentation/LoginViewModelTests.cs`: thay test `Register_ShowsConfirmationOnlyWhenSubmitted` bằng:

```csharp
    [Theory]
    [InlineData("newuser01", "Register_Submitted")]
    [InlineData(null, null)]
    public void Register_PrefillsUsernameOnlyWhenRegistered(string? registered, string? expected)
    {
        _dialogs.RegistrationResult = registered;
        var login = CreateViewModel();

        login.RegisterCommand.Execute(null);

        Assert.Equal(1, _dialogs.RegistrationCount);
        Assert.Equal(expected, login.InfoMessage);
        Assert.Equal(registered ?? string.Empty, login.Username);
    }
```

- [ ] **Step 6: Chạy test để thấy thất bại**

Run: `dotnet test --project tests/Transport.UnitTests`
Expected: FAIL khi biên dịch (`RegisterViewModel` chưa có `Password`/`ConfirmPassword`/`IsMismatch`, `IDialogService.ShowRegistration` vẫn trả `bool`).

- [ ] **Step 7: Cài đặt `RegisterViewModel`**

`src/Transport.Presentation.Wpf/ViewModels/Dialogs/RegisterViewModel.cs` (thay toàn bộ):

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Transport.Application.Services;
using Transport.Domain.Errors;
using Transport.Presentation.Wpf.Localization;

namespace Transport.Presentation.Wpf.ViewModels.Dialogs;

// Form tự đăng ký ở màn hình đăng nhập; lỗi hiện ngay trong hộp thoại để không phải nhập lại
public sealed partial class RegisterViewModel : ObservableObject, IDialogViewModel
{
    private readonly RegistrationService _registrations;
    private readonly ILocalizationService _localization;

    public RegisterViewModel(RegistrationService registrations, ILocalizationService localization)
    {
        _registrations = registrations;
        _localization = localization;
        Username = string.Empty;
        Password = string.Empty;
        ConfirmPassword = string.Empty;
        FullName = string.Empty;
        Phone = string.Empty;
    }

    public event Action<bool>? CloseRequested;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    public partial string Username { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    [NotifyPropertyChangedFor(nameof(IsMismatch))]
    public partial string Password { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    [NotifyPropertyChangedFor(nameof(IsMismatch))]
    public partial string ConfirmPassword { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    public partial string FullName { get; set; }

    [ObservableProperty]
    public partial string Phone { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    // Đã nhập lại nhưng chưa khớp: hiện cảnh báo
    public bool IsMismatch => ConfirmPassword.Length > 0 && ConfirmPassword != Password;

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private async Task SubmitAsync()
    {
        ErrorMessage = null;
        try
        {
            await _registrations.RegisterAsync(Username, Password, FullName, Phone);
            CloseRequested?.Invoke(true);
        }
        catch (BusinessRuleException ex)
        {
            ErrorMessage = _localization.Translate(ex);
        }
    }

    private bool CanSubmit() =>
        !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(FullName)
        && Password.Length > 0 && Password == ConfirmPassword;
}
```

- [ ] **Step 8: Sửa `RegisterDialog.xaml`**

Thêm namespace `xmlns:c="clr-namespace:Transport.Presentation.Wpf.Controls"` vào thẻ `<Window>`, ngay sau dòng `xmlns:l=...`. Chèn đoạn sau ngay sau `TextBox` username (`x:Name="UsernameInput"`) và trước label `Staff_FullName`:

```xml
        <TextBlock Text="{l:Loc Register_Password}" Style="{StaticResource FieldLabelStyle}" />
        <PasswordBox c:PasswordBoxAssistant.BoundPassword="{Binding Password}"
                     AutomationProperties.Name="{l:Loc Register_Password}" />
        <TextBlock Text="{l:Loc Register_ConfirmPassword}" Style="{StaticResource FieldLabelStyle}" />
        <PasswordBox c:PasswordBoxAssistant.BoundPassword="{Binding ConfirmPassword}"
                     AutomationProperties.Name="{l:Loc Register_ConfirmPassword}" />
        <TextBlock Text="{l:Loc ChangePassword_Mismatch}" Foreground="{StaticResource ErrorBrush}" Margin="0,8,0,0"
                   Visibility="{Binding IsMismatch, Converter={StaticResource BoolToVisibility}}" />
        <TextBlock Text="{l:Loc ChangePassword_Rules}" Style="{StaticResource StatLabelStyle}" TextWrapping="Wrap" Margin="0,8,0,0" />
```

- [ ] **Step 9: `ShowRegistration` trả username, đăng nhập điền sẵn**

`Services/IDialogService.cs`:

```csharp
    // Form tự đăng ký ở màn hình đăng nhập; trả về username vừa tạo, null nếu hủy
    string? ShowRegistration();
```

`Services/DialogService.cs`: thay dòng `public bool ShowRegistration() => ...` bằng:

```csharp
    public string? ShowRegistration()
    {
        var viewModel = new RegisterViewModel(registrations, localization);
        return ShowModal(new RegisterDialog(), viewModel) ? viewModel.Username.Trim() : null;
    }
```

`ViewModels/LoginViewModel.cs`: thay method `Register` bằng:

```csharp
    [RelayCommand]
    private void Register()
    {
        if (_dialogs.ShowRegistration() is { } username)
        {
            Username = username;
            Password = string.Empty;
            ErrorMessage = null;
            InfoMessage = _localization["Register_Submitted"];
        }
    }
```

- [ ] **Step 10: Chuỗi giao diện (cả hai file resx, cùng khóa)**

Sửa giá trị các khóa sau, và thêm hai khóa mới `Register_Password`, `Register_ConfirmPassword` ngay sau `Register_Submit`:

| Khóa | `Strings.resx` (en) | `Strings.vi.resx` (vi) |
|---|---|---|
| `Register_Title` | Create an account | Đăng ký tài khoản |
| `Register_Hint` | The new account can sell tickets right away. An operator can grant more permissions later. | Tài khoản mới dùng được ngay với quyền bán vé. Điều hành có thể cấp thêm quyền sau. |
| `Register_Submit` | Register | Đăng ký |
| `Register_Password` (mới) | Password | Mật khẩu |
| `Register_ConfirmPassword` (mới) | Confirm password | Nhập lại mật khẩu |
| `Register_Submitted` | Account created. You can sign in now. | Đăng ký thành công. Bạn có thể đăng nhập ngay. |
| `Err_20061` | Too many accounts were registered recently. Please try again later. | Có quá nhiều tài khoản vừa được đăng ký. Vui lòng thử lại sau. |

Mẫu một dòng: `<data name="Register_Password" xml:space="preserve"><value>Mật khẩu</value></data>`

- [ ] **Step 11: Integration test thay thế**

`tests/Transport.UnitTests/Infrastructure/OracleIntegrationTests.cs`: thêm vào cuối class:

```csharp
    [Fact]
    public async Task SelfRegistration_CreatesTicketSellerWhoCanSignInRightAway()
    {
        RequireDatabase();
        // User mới mỗi lần chạy; app không xóa được user nên cuối test khóa lại
        var username = $"sr_{DateTime.Now:MMddHHmmss}";
        using (var anonymous = CreateServices())
        {
            await anonymous.GetRequiredService<RegistrationService>().RegisterAsync(username, "SelfReg#1", "Integration Test", null);
        }

        // Không bị bắt đổi mật khẩu (không PASSWORD EXPIRE) và chỉ có quyền bán vé
        using (var services = await SignInAsync(username, "SelfReg#1"))
        {
            Assert.False(services.GetRequiredService<IUserSession>().IsOperator);
        }

        using var cleanup = await SignInAsync("operator01", OperatorPassword);
        await cleanup.GetRequiredService<StaffService>().SetLockedAsync(username, true);
    }
```

- [ ] **Step 12: Chạy toàn bộ test**

Run: `dotnet build` rồi `dotnet test --project tests/Transport.UnitTests`
Expected: không lỗi, không cảnh báo, toàn bộ PASS (có `ResourceParityTests` và `ViewInstantiationTests` với `RegisterDialog`).

Nếu DB đã cài lại ở Task 1, nhờ người dùng chạy thêm (PowerShell):

```powershell
$env:TRANSPORT_DB_TESTS='1'; $env:TRANSPORT_DB_OPERATOR_PASSWORD='<mật khẩu operator01 hiện tại>'; $env:TRANSPORT_DB_SELLER_PASSWORD='<mật khẩu seller01 hiện tại>'
dotnet test --project tests/Transport.UnitTests --filter-class "*OracleIntegrationTests"
```

Expected: `SelfRegistration_CreatesTicketSellerWhoCanSignInRightAway` PASS.

- [ ] **Step 13: Chạy thật trong app**

`dotnet run --project src/Transport.Presentation.Wpf`, rồi kiểm tra:
1. Ở màn hình đăng nhập, bấm **Đăng ký tài khoản**. Mật khẩu nhập lại không khớp thì có cảnh báo và nút Đăng ký bị khóa.
2. Đăng ký với mật khẩu yếu thì hiện lỗi trong hộp thoại và hộp thoại không đóng.
3. Đăng ký hợp lệ thì hộp thoại đóng, ô username được điền sẵn, hiện "Đăng ký thành công…". Nhập mật khẩu và đăng nhập thì vào thẳng app, **không** bị bắt đổi mật khẩu, và chỉ thấy menu của người bán vé.
4. Đăng ký lại đúng username đó thì báo "Tên đăng nhập đã tồn tại."
5. Đăng nhập operator: menu không còn mục "Yêu cầu đăng ký". Trang Nhân viên có tài khoản mới. Chạy `SELECT username, created_by FROM transport.staff` bằng TRANSPORT thì `created_by = TRANSPORT_REGISTRAR`. Trang Nhật ký đăng nhập có dòng `PKG_REGISTRATION_PUBLIC`.

- [ ] **Step 14: Commit**

```bash
node .gitnexus/run.cjs detect-changes --scope all --repo .
git add -A src tests
git commit -m "feat(ui): self-registration with password, sign in right away"
```
