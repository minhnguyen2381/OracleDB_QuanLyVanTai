### Task 2: Gỡ luồng duyệt đăng ký khỏi app

Đây là thay đổi chỉ xóa code. Kết quả phải là: build sạch và toàn bộ test hiện có pass. Form đăng ký vẫn gọi `SubmitAsync(username, fullName, phone)` cho tới Task 3.

**Files:**
- Delete: `src/Transport.Presentation.Wpf/ViewModels/Pages/RegistrationRequestsViewModel.cs`
- Delete: `src/Transport.Presentation.Wpf/Views/Pages/RegistrationRequestsPage.xaml`, `RegistrationRequestsPage.xaml.cs`
- Delete: `src/Transport.Presentation.Wpf/ViewModels/Dialogs/ApproveRegistrationViewModel.cs`
- Delete: `src/Transport.Presentation.Wpf/Views/Dialogs/ApproveRegistrationDialog.xaml`, `ApproveRegistrationDialog.xaml.cs`
- Delete: `src/Transport.Application/Models/RegistrationRequest.cs`, `src/Transport.Domain/Enums/RegistrationStatus.cs`
- Delete: `tests/Transport.UnitTests/Presentation/RegistrationRequestsViewModelTests.cs`
- Modify: `src/Transport.Presentation.Wpf/PresentationServices.cs:54`, `Views/PageTemplates.xaml:16`, `Services/IDialogService.cs:27-28`, `Services/DialogService.cs:65-69`
- Modify: `src/Transport.Application/Abstractions/IRegistrationRepository.cs`, `Services/RegistrationService.cs`, `src/Transport.Infrastructure/Repositories/RegistrationRepository.cs`
- Modify: `src/Transport.Domain/Errors/BusinessErrorCode.cs:25-26`, `Resources/Strings.resx`, `Resources/Strings.vi.resx`
- Test: `tests/Transport.UnitTests/Fakes/FakeDialogService.cs`, `Fakes/FakeRegistrationRepository.cs`, `Application/RegistrationServiceTests.cs`, `Presentation/DialogViewModelTests.cs`, `Presentation/ViewInstantiationTests.cs`, `Infrastructure/OracleErrorMapperTests.cs:13`, `Infrastructure/OracleIntegrationTests.cs:174-190`

**Interfaces:**
- Produces: `RegistrationService(IRegistrationRepository registrations)`, chỉ còn `Task SubmitAsync(string? username, string? fullName, string? phone, CancellationToken ct = default)`. Không còn phụ thuộc `IUserSession`.
- Produces: `IRegistrationRepository`, chỉ còn `Task SubmitAsync(string username, string fullName, string? phone, CancellationToken ct = default)`.
- Produces: `BusinessErrorCode.TooManyRegistrations = 20061` (đổi tên từ `TooManyPendingRequests`). `RegistrationNotFound` bị xóa.

- [ ] **Step 1: Impact analysis**

```bash
node .gitnexus/run.cjs impact "RegistrationService" --direction upstream --repo .
node .gitnexus/run.cjs impact "IRegistrationRepository" --direction upstream --repo .
node .gitnexus/run.cjs impact "PromptApproval" --direction upstream --repo .
node .gitnexus/run.cjs impact "RegistrationRequest" --direction upstream --repo .
node .gitnexus/run.cjs impact "RegistrationStatus" --direction upstream --repo .
```

Báo người dùng danh sách caller và mức risk. Caller dự kiến chỉ gồm các file trong danh sách Files ở trên. Nếu có caller khác thì dừng lại và hỏi.

- [ ] **Step 2: Xóa file của luồng duyệt**

```bash
git rm src/Transport.Presentation.Wpf/ViewModels/Pages/RegistrationRequestsViewModel.cs \
  src/Transport.Presentation.Wpf/Views/Pages/RegistrationRequestsPage.xaml \
  src/Transport.Presentation.Wpf/Views/Pages/RegistrationRequestsPage.xaml.cs \
  src/Transport.Presentation.Wpf/ViewModels/Dialogs/ApproveRegistrationViewModel.cs \
  src/Transport.Presentation.Wpf/Views/Dialogs/ApproveRegistrationDialog.xaml \
  src/Transport.Presentation.Wpf/Views/Dialogs/ApproveRegistrationDialog.xaml.cs \
  src/Transport.Application/Models/RegistrationRequest.cs \
  src/Transport.Domain/Enums/RegistrationStatus.cs \
  tests/Transport.UnitTests/Presentation/RegistrationRequestsViewModelTests.cs
```

- [ ] **Step 3: Gỡ đăng ký trang và hộp thoại**

- `PresentationServices.cs`: xóa dòng `services.AddPage<RegistrationRequestsViewModel>("Menu_Registrations", "", 101, operatorOnly: true);`
- `Views/PageTemplates.xaml`: xóa dòng `<DataTemplate DataType="{x:Type vm:RegistrationRequestsViewModel}"><pages:RegistrationRequestsPage /></DataTemplate>`
- `Services/IDialogService.cs`: xóa

```csharp
    // Duyệt yêu cầu đăng ký: mật khẩu tạm + role; null nếu hủy
    ApprovalInput? PromptApproval(string title);
```

- `Services/DialogService.cs`: xóa method `PromptApproval` (5 dòng, từ `public ApprovalInput? PromptApproval(string title)` đến `}`).
- `tests/Transport.UnitTests/Fakes/FakeDialogService.cs`: xóa property `public ApprovalInput? ApprovalResult { get; set; }` và method `PromptApproval`.

- [ ] **Step 4: Thu gọn Application/Infrastructure**

`src/Transport.Application/Abstractions/IRegistrationRepository.cs`:

```csharp
namespace Transport.Application.Abstractions;

// Gọi PKG_REGISTRATION_PUBLIC bằng user TRANSPORT_REGISTRAR (chưa đăng nhập)
public interface IRegistrationRepository
{
    Task SubmitAsync(string username, string fullName, string? phone, CancellationToken ct = default);
}
```

`src/Transport.Application/Services/RegistrationService.cs`:

```csharp
using Transport.Application.Abstractions;

namespace Transport.Application.Services;

// Đăng ký tài khoản từ màn hình đăng nhập (chưa đăng nhập)
public sealed class RegistrationService(IRegistrationRepository registrations)
{
    // Repository gọi bằng user TRANSPORT_REGISTRAR
    public Task SubmitAsync(string? username, string? fullName, string? phone, CancellationToken ct = default)
    {
        var user = username?.Trim();
        Rules.Username(user);
        Rules.Required(fullName);
        Rules.OptionalPhone(phone);
        return registrations.SubmitAsync(user!, fullName!.Trim(), Rules.TrimToNull(phone), ct);
    }
}
```

`src/Transport.Infrastructure/Repositories/RegistrationRepository.cs`:

```csharp
using Transport.Application.Abstractions;
using Transport.Infrastructure.Persistence;

namespace Transport.Infrastructure.Repositories;

// Đăng ký bằng user REGISTRAR qua PKG_REGISTRATION_PUBLIC
internal sealed class RegistrationRepository(OracleConnectionFactory factory) : OracleRepository(factory), IRegistrationRepository
{
    public Task SubmitAsync(string username, string fullName, string? phone, CancellationToken ct = default) =>
        CallAsRegistrarAsync("pkg_registration_public.submit_request",
            new { p_username = username, p_full_name = fullName, p_phone = phone }, ct);
}
```

(Tên thủ tục ở đây chỉ dùng tạm cho đến Task 3. Unit test không gọi DB nên không bị ảnh hưởng.)

- [ ] **Step 5: Mã lỗi và chuỗi giao diện**

Trong `BusinessErrorCode.cs`, xóa dòng `RegistrationNotFound = 20060,`. Sau đó đổi tên `TooManyPendingRequests` thành `TooManyRegistrations` bằng GitNexus: gọi MCP `rename({symbol_name: "TooManyPendingRequests", new_name: "TooManyRegistrations", dry_run: true})`, xem lại kết quả rồi chạy với `dry_run: false`. Hiện chỉ có đúng dòng khai báo dùng tên này.

Xóa các khóa resx của luồng duyệt khỏi cả hai file:

```bash
sed -i -e '/name="Reg_/d' -e '/name="RegStatus_/d' -e '/name="Menu_Registrations"/d' -e '/name="Err_20060"/d' \
  src/Transport.Presentation.Wpf/Resources/Strings.resx src/Transport.Presentation.Wpf/Resources/Strings.vi.resx
```

Kiểm tra: `grep -rn "Reg_\|RegStatus_\|Menu_Registrations" src --include=*.xaml --include=*.cs --include=*.resx | grep -v /obj/` phải trả về rỗng.

- [ ] **Step 6: Sửa test theo API mới**

`tests/Transport.UnitTests/Fakes/FakeRegistrationRepository.cs`:

```csharp
using Transport.Application.Abstractions;

namespace Transport.UnitTests.Fakes;

internal sealed class FakeRegistrationRepository : IRegistrationRepository
{
    public List<string> Calls { get; } = [];

    public Task SubmitAsync(string username, string fullName, string? phone, CancellationToken ct = default)
    {
        Calls.Add($"submit:{username}:{fullName}:{phone}");
        return Task.CompletedTask;
    }
}
```

`tests/Transport.UnitTests/Application/RegistrationServiceTests.cs`:
- Xóa field `Anonymous`, `Pending` và các test `Approve_RequiresAStrongTemporaryPassword`, `Approve_And_Reject_ReachRepository`, `Reject_RequiresReason`, `Sellers_CannotReviewRequests`.
- Đổi `private RegistrationService Create(FakeUserSession session) => new(_repository, session);` thành `private RegistrationService Create() => new(_repository);`.
- Đổi mọi `Create(Anonymous)` thành `Create()`.
- Xóa `using Transport.Application.Models;` và `using Transport.Domain.Enums;` nếu không còn dùng.

`tests/Transport.UnitTests/Presentation/DialogViewModelTests.cs`:
- Xóa test `ApproveRegistration_DefaultsToTicketSellerAndNeedsPassword`.
- Đổi hai chỗ `new RegistrationService(repository, new FakeUserSession())` và `new RegistrationService(new FakeRegistrationRepository(), new FakeUserSession())` thành `new RegistrationService(repository)` và `new RegistrationService(new FakeRegistrationRepository())`.

`tests/Transport.UnitTests/Presentation/ViewInstantiationTests.cs`: xóa `_ = new RegistrationRequestsPage();` và `_ = new ApproveRegistrationDialog();`.

`tests/Transport.UnitTests/Infrastructure/OracleErrorMapperTests.cs:13`: đổi `[InlineData(20060)] [InlineData(20061)] [InlineData(20062)]` thành `[InlineData(20061)] [InlineData(20062)]`.

`tests/Transport.UnitTests/Infrastructure/OracleIntegrationTests.cs`: xóa test `Registration_IsSubmittedWithoutSignInAndReviewedByOperator` (Task 3 sẽ thêm test mới thay thế).

- [ ] **Step 7: Build và chạy test**

Run: `dotnet build` rồi `dotnet test --project tests/Transport.UnitTests`
Expected: build không lỗi, không cảnh báo; toàn bộ test PASS (integration test bị skip nếu chưa đặt `TRANSPORT_DB_TESTS=1`). Nếu báo using thừa (IDE0005) thì xóa using đó.

- [ ] **Step 8: Commit**

```bash
node .gitnexus/run.cjs detect-changes --scope all --repo .
git add -A src tests
git commit -m "refactor(ui): remove operator approval of registration requests"
```
