# Tự đăng ký tài khoản (bỏ bước duyệt) – Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Người chưa có tài khoản tự đăng ký (username, mật khẩu, họ tên, SĐT) và đăng nhập được ngay với quyền bán vé, không cần điều hành duyệt.

**Architecture:** `PKG_REGISTRATION_PUBLIC.register_account` (gọi bằng user `TRANSPORT_REGISTRAR`) kiểm tra dữ liệu, chống spam theo `STAFF.CREATED_BY`, rồi gọi `pkg_staff.create_staff(..., 'ROLE_TICKET_SELLER', p_expire => 0)`. Toàn bộ luồng duyệt (bảng `REGISTRATION_REQUESTS`, `PKG_REGISTRATION`, trang "Yêu cầu đăng ký", hộp thoại Duyệt) bị xóa. App chỉ đổi form đăng ký (thêm mật khẩu) và điền sẵn username sau khi đăng ký.

**Tech Stack:** Oracle 21c XE (XEPDB1), PL/SQL, SQL*Plus/batch; .NET 10 WPF, CommunityToolkit.Mvvm, Dapper, Oracle.ManagedDataAccess.Core, xUnit v3.

**Spec:** Thiết kế đã duyệt trong chat ngày 2026-10-18 (tóm tắt ở mục "Quyết định" bên dưới). Plan này sửa đổi `docs/superpowers/specs/2026-10-18-auth-security-design.md` §2.2–2.3, §2.5, §3, §4.1, §4.4. Task 4 ghi các thay đổi đó vào spec.

## Quyết định (đã chốt với người dùng)

- Tài khoản tự đăng ký **luôn** là `ROLE_TICKET_SELLER`. Role được ghi cứng trong PL/SQL, app không truyền vào. Muốn nâng quyền thì operator dùng trang Nhân viên (`change_role`).
- Người dùng tự đặt mật khẩu nên **không** gắn `PASSWORD EXPIRE`.
- **Xóa hẳn** luồng duyệt: không giữ bảng làm nhật ký. Ai tự đăng ký thì xem ở `STAFF.CREATED_BY = 'TRANSPORT_REGISTRAR'`; mỗi lần gọi package cũng nằm trong Unified Auditing.
- Chống spam: mỗi user gọi được tạo tối đa **5 tài khoản trong 1 giờ**, quá thì báo `-20061`.
- Chấp nhận: đăng ký trùng username báo "Tên đăng nhập đã tồn tại" (ORA-01920). Như vậy có thể dò được username, nhưng không tránh được khi tạo tài khoản ngay.

## Global Constraints

- Định danh SQL (bảng, cột, package, role, giá trị trạng thái) và thông điệp `RAISE_APPLICATION_ERROR` bằng **tiếng Anh**; comment bằng **tiếng Việt**.
- Logic nghiệp vụ nằm trong PL/SQL; app chỉ gọi package/view.
- Mọi chuỗi giao diện qua `Strings.resx` (en) **và** `Strings.vi.resx` (vi), hai file cùng bộ khóa (`ResourceParityTests` bắt buộc). Mỗi giá trị `BusinessErrorCode` phải có khóa `Err_<số>`.
- Build không được có cảnh báo (`TreatWarningsAsErrors=true`, `EnforceCodeStyleInBuild=true`).
- Lệnh test: `dotnet test --project tests/Transport.UnitTests` (lọc: thêm `--filter-class "*TenLop"`).
- CLAUDE.md:
  - **Trước khi sửa** một hàm/lớp C# đã có, chạy `node .gitnexus/run.cjs impact "<Symbol>" --direction upstream --repo .` và báo rủi ro (HIGH/CRITICAL phải cảnh báo; `UNKNOWN` thì xác nhận lại bằng grep).
  - **Trước mỗi commit**, chạy `node .gitnexus/run.cjs detect-changes --scope all --repo .`. Nếu index đã cũ, chạy `node .gitnexus/run.cjs analyze --index-only` trước.
  - Đổi tên symbol bằng công cụ `rename` của GitNexus, không dùng find-and-replace.
- Commit kết thúc bằng:
  ```
  Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
  Claude-Session: https://claude.ai/code/session_016qy8FyinGcSgxG5Rh4VUSa
  ```
- Mật khẩu REGISTRAR `Register#Only1` công khai có chủ đích. Không ghi mật khẩu `TRANSPORT` vào bất kỳ file nào.

## Ghi chú cho người thực thi

- Agent **không có** mật khẩu SYS/TRANSPORT. Ở các bước chạy SQL, hãy dừng lại và nhờ người dùng tự chạy lệnh trong cửa sổ `cmd` riêng rồi dán kết quả vào. Phải nhắc trước rằng `install_all.bat` **xóa và cài lại** toàn bộ CSDL.
- `USER` trong package definer-rights trả về **user phiên** (code đã dựa vào điều này ở `pkg_staff.not_self`). Do đó cột `STAFF.CREATED_BY DEFAULT USER` sẽ là `TRANSPORT_REGISTRAR` khi tự đăng ký từ app, và là `TRANSPORT` khi chạy `09_test_scenarios.sql`.
- Giới hạn 5 tài khoản/giờ áp cho mọi lần đăng ký qua app (đều chạy bằng REGISTRAR). Nếu chạy integration test nhiều lần liên tục mà gặp `-20061` thì đợi 1 giờ.

## File Structure

| File | Thay đổi |
|---|---|
| `database/02_tables.sql` | Xóa bảng `registration_requests` và index |
| `database/04_packages.sql` | `pkg_staff.create_staff` thêm `p_expire`; `pkg_registration_public.register_account` thay `submit_request`; xóa `pkg_registration` |
| `database/05_views.sql`, `database/10_audit.sql` | Nhật ký và audit chuyển sang `PKG_REGISTRATION_PUBLIC` |
| `database/07_grants.sql` | Bỏ grant `registration_requests` và `pkg_registration` |
| `database/00_uninstall.sql` | Dọn user `TEST_REG_%` |
| `database/09_test_scenarios.sql` | Viết lại kịch bản 12 và phần dọn dữ liệu |
| `src/Transport.Application/Abstractions/IRegistrationRepository.cs`, `Services/RegistrationService.cs`, `src/Transport.Infrastructure/Repositories/RegistrationRepository.cs` | Chỉ còn `RegisterAsync` |
| `src/Transport.Presentation.Wpf/ViewModels/Dialogs/RegisterViewModel.cs`, `Views/Dialogs/RegisterDialog.xaml` | Thêm mật khẩu và nhập lại mật khẩu |
| `src/Transport.Presentation.Wpf/Services/IDialogService.cs`, `DialogService.cs`, `ViewModels/LoginViewModel.cs` | `ShowRegistration()` trả về username; điền sẵn vào form đăng nhập |
| Xóa: `RegistrationRequestsViewModel`/`Page`, `ApproveRegistrationViewModel`/`Dialog`, `RegistrationRequest`, `RegistrationStatus`, `RegistrationRequestsViewModelTests` | Luồng duyệt |
| `README.md`, `docs/user-guide.md`, spec auth-security | Tài liệu |

---

### Task 1: Tầng Oracle – tự đăng ký tạo tài khoản bán vé ngay

**Files:**
- Modify: `database/02_tables.sql:109-123`
- Modify: `database/04_packages.sql` (spec + body `pkg_staff`; `pkg_registration_public`; xóa `pkg_registration`)
- Modify: `database/05_views.sql:55`, `database/07_grants.sql:22-23,37-38`, `database/10_audit.sql:11-14`, `database/00_uninstall.sql:28-35`
- Test: `database/09_test_scenarios.sql` (khai báo, `cleanup`, kịch bản 12, ghi chú cuối file)

**Interfaces:**
- Produces: `pkg_registration_public.register_account(p_username IN VARCHAR2, p_password IN VARCHAR2, p_full_name IN VARCHAR2, p_phone IN VARCHAR2)`. Lỗi có thể nổi lên: `-20062` (dữ liệu sai), `-20061` (quá 5 tài khoản/giờ), `-20044` (mật khẩu có `"` hoặc < 8 ký tự), `-28003` (mật khẩu không đạt verify function), `-1920` (trùng username).
- Produces: `pkg_staff.create_staff(..., p_role IN VARCHAR2, p_expire IN NUMBER DEFAULT 1)`. Các lời gọi hiện có không phải đổi.

- [ ] **Step 1: Viết test trước – sửa `database/09_test_scenarios.sql`**

Trong khối `DECLARE`, thêm biến ngay sau `v_log     NUMBER;`:

```sql
  v_before  NUMBER;
```

Trong `PROCEDURE cleanup`, thay ba dòng

```sql
    DELETE FROM staff WHERE username = 'TEST_REG_01';
    BEGIN EXECUTE IMMEDIATE 'DROP USER test_reg_01'; EXCEPTION WHEN OTHERS THEN NULL; END;
    DELETE FROM registration_requests WHERE username LIKE 'TEST\_REG\_%' ESCAPE '\';
```

bằng

```sql
    FOR i IN 1 .. 9 LOOP
      DELETE FROM staff WHERE username = 'TEST_REG_0' || i;
      BEGIN EXECUTE IMMEDIATE 'DROP USER test_reg_0' || i; EXCEPTION WHEN OTHERS THEN NULL; END;
    END LOOP;
```

Thay toàn bộ kịch bản 12, tức từ dòng `-- 12. Đăng ký tài khoản: gửi, chống spam, duyệt, từ chối` đến hết `ok('12h. rejection is recorded', v_m = 1);`, bằng:

```sql
  -- 12. Tự đăng ký: kiểm tra dữ liệu, tạo tài khoản bán vé, trùng tên, mật khẩu yếu, chống spam
  expect_error('12a. invalid registration username is rejected', -20062,
    'BEGIN pkg_registration_public.register_account(''x;'', ''TestReg#1'', ''Test Reg'', NULL); END;');
  expect_error('12b. invalid registration phone is rejected', -20062,
    'BEGIN pkg_registration_public.register_account(''test_reg_01'', ''TestReg#1'', ''Test Reg'', ''123''); END;');
  -- Số tài khoản user này đã tạo trong 1 giờ qua (VD seed lúc cài), dùng để tính kết quả 12f
  SELECT COUNT(*) INTO v_before FROM staff
   WHERE created_by = USER AND created_at > SYSTIMESTAMP - INTERVAL '1' HOUR;
  pkg_registration_public.register_account('test_reg_01', 'TestReg#1', 'Test Reg', '0999000001');
  SELECT COUNT(*) INTO v_n FROM staff
   WHERE username = 'TEST_REG_01' AND role_name = 'ROLE_TICKET_SELLER' AND created_by = USER;
  SELECT COUNT(*) INTO v_m FROM all_users WHERE username = 'TEST_REG_01';
  ok('12c. registration creates a ticket seller account', v_n = 1 AND v_m = 1);
  expect_error('12d. registering an existing username is rejected', -1920,
    'BEGIN pkg_registration_public.register_account(''test_reg_01'', ''TestReg#1'', ''Test Reg'', NULL); END;');
  expect_error('12e. weak registration password is rejected by the verify function', -28003,
    'BEGIN pkg_registration_public.register_account(''test_reg_09'', ''abcdefgh'', ''Test Reg'', NULL); END;');
  -- Tối đa 5 tài khoản/giờ cho mỗi user gọi: trong 5 lần thử tiếp theo chỉ phần còn lại của hạn mức thành công
  v_n := 0;
  v_m := 0;
  FOR i IN 2 .. 6 LOOP
    BEGIN
      pkg_registration_public.register_account('test_reg_0' || i, 'TestReg#' || i, 'Test Reg', NULL);
      v_n := v_n + 1;
    EXCEPTION
      WHEN OTHERS THEN
        IF SQLCODE = -20061 THEN v_m := v_m + 1; ELSE RAISE; END IF;
    END;
  END LOOP;
  ok('12f. at most 5 registrations per hour per caller', v_n = GREATEST(4 - v_before, 0) AND v_m = 5 - v_n);
```

Kịch bản 13 giữ nguyên. Trong phần "Kiểm tra phân quyền thủ công" cuối file, thay hai dòng

```sql
--   EXEC transport.pkg_registration.reject_request(1, 'x')
--     -> PLS-00201 (REGISTRAR không gọi được thủ tục duyệt)
```

bằng

```sql
--   EXEC transport.pkg_staff.create_staff('x_op01', 'XxOper#01', 'X', NULL, 'ROLE_OPERATOR')
--     -> PLS-00201 (REGISTRAR không gọi thẳng được PKG_STAFF; chỉ tạo được tài khoản bán vé qua PKG_REGISTRATION_PUBLIC)
```

- [ ] **Step 2: Sửa `pkg_staff` trong `database/04_packages.sql`**

Ở phần spec, thay khai báo `create_staff` bằng:

```sql
  -- Tạo user Oracle, cấp role, ghi vào STAFF.
  -- p_expire = 1: mật khẩu tạm do điều hành đặt, phải đổi ở lần đăng nhập đầu; 0: người dùng tự đặt (tự đăng ký)
  PROCEDURE create_staff(p_username IN VARCHAR2, p_password IN VARCHAR2, p_full_name IN VARCHAR2,
                         p_phone IN VARCHAR2, p_role IN VARCHAR2, p_expire IN NUMBER DEFAULT 1);
```

Ở phần body, thay phần đầu của `create_staff` (từ header đến hết câu `EXECUTE IMMEDIATE 'CREATE USER ...'`) bằng đoạn dưới. Giá trị mặc định ở body phải giống hệt spec, nếu không sẽ bị PLS-00593.

```sql
  PROCEDURE create_staff(p_username IN VARCHAR2, p_password IN VARCHAR2, p_full_name IN VARCHAR2,
                         p_phone IN VARCHAR2, p_role IN VARCHAR2, p_expire IN NUMBER DEFAULT 1) IS
    v_user VARCHAR2(30) := valid_username(p_username);
  BEGIN
    check_role(p_role);
    check_password(p_password);
    -- Mật khẩu do điều hành đặt chỉ là mật khẩu tạm: nhân viên phải đổi ở lần đăng nhập đầu
    EXECUTE IMMEDIATE 'CREATE USER ' || v_user || ' IDENTIFIED BY "' || p_password || '" PROFILE profile_staff'
                      || CASE WHEN p_expire = 1 THEN ' PASSWORD EXPIRE' END;
```

Phần còn lại (khối `BEGIN ... GRANT ... INSERT ... EXCEPTION ... DROP USER`) giữ nguyên.

- [ ] **Step 3: Thay `pkg_registration_public`, xóa `pkg_registration`**

Thay từ `CREATE OR REPLACE PACKAGE pkg_registration_public AS` đến dấu `/` sau `END pkg_registration;` (body) bằng:

```sql
CREATE OR REPLACE PACKAGE pkg_registration_public AS
  -- Người chưa có tài khoản tự đăng ký (gọi bằng user TRANSPORT_REGISTRAR).
  -- Luôn tạo tài khoản bán vé; mật khẩu do người dùng tự đặt nên không bắt đổi ở lần đăng nhập đầu.
  PROCEDURE register_account(p_username IN VARCHAR2, p_password IN VARCHAR2,
                             p_full_name IN VARCHAR2, p_phone IN VARCHAR2);
END pkg_registration_public;
/

CREATE OR REPLACE PACKAGE BODY pkg_registration_public AS

  c_max_per_hour CONSTANT PLS_INTEGER := 5;  -- Số tài khoản một user (thường là REGISTRAR) được tạo trong 1 giờ

  PROCEDURE register_account(p_username IN VARCHAR2, p_password IN VARCHAR2,
                             p_full_name IN VARCHAR2, p_phone IN VARCHAR2) IS
    v_name   VARCHAR2(4000) := TRIM(p_full_name);
    v_recent PLS_INTEGER;
  BEGIN
    IF p_username IS NULL OR NOT REGEXP_LIKE(p_username, '^[A-Za-z][A-Za-z0-9_]{2,29}$')
       OR v_name IS NULL OR LENGTH(v_name) > 100
       OR (p_phone IS NOT NULL AND NOT REGEXP_LIKE(p_phone, '^0[0-9]{9}$')) THEN
      RAISE_APPLICATION_ERROR(-20062, 'Invalid registration data.');
    END IF;

    -- Chống spam: STAFF.CREATED_BY mặc định là user phiên, nên đếm được số tài khoản REGISTRAR vừa tạo
    SELECT COUNT(*) INTO v_recent
      FROM staff
     WHERE created_by = USER AND created_at > SYSTIMESTAMP - INTERVAL '1' HOUR;
    IF v_recent >= c_max_per_hour THEN
      RAISE_APPLICATION_ERROR(-20061, 'Too many registrations. Try again later.');
    END IF;

    -- Role ghi cứng: tự đăng ký không bao giờ có quyền điều hành.
    -- create_staff tự kiểm tra mật khẩu (verify function -> ORA-28003), trùng username (ORA-01920) và tự commit.
    pkg_staff.create_staff(p_username, p_password, v_name, p_phone, 'ROLE_TICKET_SELLER', p_expire => 0);
  END register_account;

END pkg_registration_public;
/
```

- [ ] **Step 4: Xóa bảng, sửa grant, view, audit, gỡ cài đặt**

`database/02_tables.sql`: xóa từ dòng comment `-- Yêu cầu đăng ký tài khoản chờ điều hành duyệt; KHÔNG lưu mật khẩu` đến hết `CREATE INDEX idx_reg_status ON registration_requests(status);`.

`database/07_grants.sql`: xóa hai dòng

```sql
GRANT SELECT ON registration_requests  TO role_operator;
GRANT EXECUTE ON pkg_registration      TO role_operator;
```

và đổi comment của grant REGISTRAR thành:

```sql
-- Người chưa có tài khoản: chỉ được tự đăng ký tài khoản bán vé, không đọc được bảng nào
GRANT EXECUTE ON pkg_registration_public TO transport_registrar;
```

`database/05_views.sql`, trong `v_login_audit`:

```sql
    OR (object_schema = 'TRANSPORT' AND object_name IN ('PKG_STAFF', 'PKG_REGISTRATION_PUBLIC'));
```

`database/10_audit.sql`:

```sql
-- Mọi lần tạo/khóa/đổi quyền/đặt lại mật khẩu nhân viên và mọi lần tự đăng ký
CREATE AUDIT POLICY pol_transport_account_admin
  ACTIONS EXECUTE ON transport.pkg_staff, EXECUTE ON transport.pkg_registration_public;
```

`database/00_uninstall.sql`: trong khối "Phòng trường hợp bảng STAFF đã mất", đổi điều kiện thành:

```sql
             WHERE username IN ('OPERATOR01', 'SELLER01', 'SELLER02', 'TEST_STAFF_01')
                OR username LIKE 'TEST\_REG\_%' ESCAPE '\') LOOP
```

- [ ] **Step 5: Kiểm tra không còn tham chiếu cũ**

Run: `grep -rn -i "registration_requests\|pkg_registration\b\|submit_request\|approve_request\|reject_request" database/`
Expected: không có dòng nào.

- [ ] **Step 6: Nhờ người dùng cài lại DB và chạy test SQL**

Nhắc trước: `install_all.bat` **xóa và cài lại** toàn bộ CSDL. Người dùng chạy trong `cmd` ở thư mục `database`:

```bat
install_all.bat
sqlplus -S -L transport/"<mật khẩu TRANSPORT>"@//localhost:1521/XEPDB1 @09_test_scenarios.sql
sqlplus -S -L "sys/<mật khẩu SYS>@//localhost:1521/XEPDB1 as sysdba" @11_security_checks.sql
```

Expected:
- `install_all.bat` không dừng ở bước nào (`user_errors` rỗng).
- `09` in `ALL TESTS PASSED`, trong đó có `PASS  12c` đến `PASS  12f` và `PASS  13`.
- `11` in `ALL CHECKS PASSED`.

- [ ] **Step 7: Commit**

```bash
node .gitnexus/run.cjs detect-changes --scope all --repo .
git add database/
git commit -m "feat(db): self-registration creates a ticket seller account without approval"
```

---

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

---

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

---

### Task 4: Tài liệu

**Files:**
- Modify: `README.md:52,66,68`
- Modify: `docs/user-guide.md` (§4.3, §4.4 gạch đầu dòng đầu, §4.6)
- Modify: `docs/superpowers/specs/2026-10-18-auth-security-design.md`

- [x] **Step 1: README**

Dòng 52 thay bằng:

```markdown
- **Tài khoản đăng ký REGISTRAR:** `TRANSPORT_REGISTRAR` (mật khẩu `Register#Only1` trong `appsettings.json`) được công khai có chủ đích theo nguyên tắc đặc quyền tối thiểu: chỉ có `CREATE SESSION` và `EXECUTE` trên `PKG_REGISTRATION_PUBLIC.REGISTER_ACCOUNT`. Thủ tục này luôn tạo tài khoản **bán vé** (không bao giờ là điều hành), tối đa 5 tài khoản mỗi giờ; REGISTRAR không đọc được bảng nào.
```

Dòng 66: `│   ├── 02_tables.sql            # Tạo các bảng nghiệp vụ chính`
Dòng 68: `│   ├── 04_packages.sql          # Packages PKG_TICKET, PKG_SCHEDULE, PKG_STAFF, PKG_REGISTRATION_PUBLIC`

- [x] **Step 2: Hướng dẫn sử dụng**

Thay nội dung §4.3 (từ dòng dưới heading đến trước `### 4.4`) bằng:

```markdown
Khi chưa có tài khoản, nhân viên tự đăng ký ngay tại màn hình đăng nhập:
1. Nhấn nút **Đăng ký tài khoản** ở cuối form đăng nhập.
2. Hộp thoại **Đăng ký tài khoản** xuất hiện:
   - **Tên đăng nhập**: Tối thiểu 3 ký tự, bắt đầu bằng chữ cái, chỉ chứa chữ cái, chữ số và dấu gạch dưới (`_`).
   - **Mật khẩu** và **Nhập lại mật khẩu**: theo quy tắc mật khẩu an toàn ở mục 4.4; hai ô phải khớp thì nút **Đăng ký** mới bật.
   - **Họ và tên**: Họ tên đầy đủ (tối đa 100 ký tự).
   - **Số điện thoại**: 10 chữ số, bắt đầu bằng 0, không bắt buộc.
3. Nhấn **Đăng ký**. Tài khoản Oracle được tạo ngay qua tài khoản kỹ thuật `TRANSPORT_REGISTRAR`, với quyền **Nhân viên bán vé**.
4. Hộp thoại đóng, tên đăng nhập được điền sẵn; nhập mật khẩu vừa đặt để đăng nhập (không phải đổi mật khẩu lần đầu).

Lưu ý: tên đăng nhập đã tồn tại sẽ bị báo lỗi; mỗi giờ hệ thống chỉ nhận tối đa 5 tài khoản tự đăng ký. Người điều hành có thể nâng quyền hoặc khóa tài khoản ở trang **Nhân viên**.
```

Ở §4.4, đổi gạch đầu dòng đầu thành:

```markdown
- **Bắt buộc đổi mật khẩu tạm**: Tài khoản do người điều hành tạo hoặc được đặt lại mật khẩu có trạng thái hết hạn ngay (`PASSWORD EXPIRE`). Trong lần đăng nhập đầu tiên bằng mật khẩu tạm, hộp thoại **Đặt mật khẩu mới** sẽ tự động bật lên. Tài khoản tự đăng ký không bị áp dụng vì mật khẩu do chính người dùng đặt.
```

Ở §4.6, đổi heading thành `### 4.6 (Người điều hành) Xem nhật ký đăng nhập`. Xóa toàn bộ tiểu mục `#### a) Duyệt yêu cầu đăng ký tài khoản ...` (5 bước). Đổi `#### b) Tra cứu nhật ký đăng nhập (Menu 'Nhật ký đăng nhập')` thành `#### Tra cứu nhật ký đăng nhập (Menu 'Nhật ký đăng nhập')`. Trong bước 3, sửa "Hành động (`LOGON`)" thành "Hành động (`LOGON`, hoặc `EXECUTE` khi quản trị tài khoản hay tự đăng ký)".

- [x] **Step 3: Ghi thay đổi vào spec auth-security**

Trong `docs/superpowers/specs/2026-10-18-auth-security-design.md`, chèn ngay sau dòng `Ngày: 2026-10-18. ...`:

```markdown
> **Cập nhật 2026-10-18 – tự đăng ký (Plan 6, `docs/superpowers/plans/2026-10-18-plan-6-self-registration.md`).** Bỏ luồng duyệt vì không có người duyệt. Các mục sau được **thay thế**:
> - §2.2–2.3: không còn bảng `REGISTRATION_REQUESTS` và package `PKG_REGISTRATION`. `PKG_REGISTRATION_PUBLIC.register_account(p_username, p_password, p_full_name, p_phone)` kiểm tra dữ liệu (`-20062`), giới hạn 5 tài khoản/giờ cho mỗi user gọi theo `STAFF.CREATED_BY` (`-20061 Too many registrations. Try again later.`), rồi gọi `pkg_staff.create_staff(..., 'ROLE_TICKET_SELLER', p_expire => 0)`. Role luôn là bán vé, không `PASSWORD EXPIRE`.
> - §2.4: `create_staff` có thêm `p_expire IN NUMBER DEFAULT 1`.
> - §2.5: audit policy và `v_login_audit` theo dõi `PKG_STAFF` và `PKG_REGISTRATION_PUBLIC`.
> - §3: bỏ `-20060`; `-20061` ứng với `TooManyRegistrations`.
> - §4.1: form đăng ký có mật khẩu và nhập lại mật khẩu; đăng ký xong điền sẵn username và đăng nhập được ngay. §4.4: bỏ trang "Yêu cầu đăng ký".
> - Đánh đổi chấp nhận: trùng username báo ORA-01920, nên có thể dò được username.
```

- [x] **Step 4: Commit**

```bash
node .gitnexus/run.cjs detect-changes --scope all --repo .
git add README.md docs/user-guide.md docs/superpowers/specs/2026-10-18-auth-security-design.md docs/superpowers/plans/2026-10-18-plan-6-self-registration.md
git commit -m "docs: self-registration without approval"
```
