# Đăng ký tài khoản và tăng cường bảo mật đăng nhập – Thiết kế

Ngày: 2026-10-18. Bổ sung cho `2026-10-17-transport-management-design.md` (gọi tắt "spec gốc"). Quy ước giữ nguyên: định danh SQL và thông điệp lỗi tiếng Anh, comment tiếng Việt, logic nghiệp vụ nằm trong PL/SQL.

> **Cập nhật 2026-10-18 – tự đăng ký (Plan 6, `docs/superpowers/plans/2026-10-18-plan-6-self-registration.md`).** Bỏ luồng duyệt vì không có người duyệt. Các mục sau được **thay thế**:
> - §2.2–2.3: không còn bảng `REGISTRATION_REQUESTS` và package `PKG_REGISTRATION`. `PKG_REGISTRATION_PUBLIC.register_account(p_username, p_password, p_full_name, p_phone)` kiểm tra dữ liệu (`-20062`), giới hạn 5 tài khoản/giờ cho mỗi user gọi theo `STAFF.CREATED_BY` (`-20061 Too many registrations. Try again later.`), rồi gọi `pkg_staff.create_staff(..., 'ROLE_TICKET_SELLER', p_expire => 0)`. Role luôn là bán vé, không `PASSWORD EXPIRE`.
> - §2.4: `create_staff` có thêm `p_expire IN NUMBER DEFAULT 1`.
> - §2.5: audit policy và `v_login_audit` theo dõi `PKG_STAFF` và `PKG_REGISTRATION_PUBLIC`.
> - §3: bỏ `-20060`; `-20061` ứng với `TooManyRegistrations`.
> - §4.1: form đăng ký có mật khẩu và nhập lại mật khẩu; đăng ký xong điền sẵn username và đăng nhập được ngay. §4.4: bỏ trang "Yêu cầu đăng ký".
> - Đánh đổi chấp nhận: trùng username báo ORA-01920, nên có thể dò được username.

## 1. Bối cảnh – kết quả kiểm tra hiện trạng

| # | Mức | Vấn đề | Cách xử lý (mục) |
|---|---|---|---|
| 1 | Nghiêm trọng | Mật khẩu chủ schema `TRANSPORT` (`Transport#2026`) ghi cứng trong `01_sys_setup.sql`, `install_all.bat`, `backup.bat`, `restore.bat`; TRANSPORT có `CREATE/ALTER/DROP USER` | §6 |
| 2 | Nghiêm trọng | Mật khẩu demo công khai trong README, dùng mãi được | §2.4, §6 |
| 3 | Cao | Lỗi đăng nhập phân biệt sai mật khẩu / bị khóa / đúng mật khẩu nhưng không phải nhân viên → dò tài khoản, thử mật khẩu TRANSPORT/SYSTEM qua app | §4.2 |
| 4 | Cao | User dùng profile `DEFAULT`: không có luật độ phức tạp, ORA-28001 hiện lỗi thô | §2.1, §4.2 |
| 5 | Trung bình | Nhân viên không tự đổi được mật khẩu; mật khẩu do operator đặt dùng mãi | §2.4, §4.3 |
| 6 | Trung bình | Không có nhật ký đăng nhập | §2.5, §4.4 |
| 7 | Thấp | `DispatcherUnhandledException` hiện nguyên `Exception.Message` | §4.5 |
| — | Thiếu | Không có màn hình đăng ký | §2.2–2.3, §4.1 |

## 2. Tầng Oracle

### 2.1 Profile và hàm kiểm tra mật khẩu (`01_sys_setup.sql`, chạy bằng SYS trong XEPDB1)

```sql
-- Luật mật khẩu nhân viên: ≥ 8 ký tự, có hoa, thường, số, ký tự đặc biệt, không chứa username, không có dấu "
CREATE OR REPLACE FUNCTION staff_password_verify(
  p_username IN VARCHAR2, p_password IN VARCHAR2, p_old_password IN VARCHAR2) RETURN BOOLEAN IS
BEGIN
  IF LENGTH(p_password) < 8
     OR NOT REGEXP_LIKE(p_password, '[A-Z]') OR NOT REGEXP_LIKE(p_password, '[a-z]')
     OR NOT REGEXP_LIKE(p_password, '[0-9]') OR NOT REGEXP_LIKE(p_password, '[^A-Za-z0-9]')
     OR INSTR(p_password, '"') > 0
     OR INSTR(UPPER(p_password), UPPER(p_username)) > 0 THEN
    RAISE_APPLICATION_ERROR(-20044, 'Password does not meet the complexity rules.');
  END IF;
  RETURN TRUE;
END;
/

CREATE PROFILE profile_staff LIMIT
  FAILED_LOGIN_ATTEMPTS 5
  PASSWORD_LOCK_TIME    15/1440      -- khóa 15 phút
  PASSWORD_LIFE_TIME    90
  PASSWORD_GRACE_TIME   0            -- hết hạn là phải đổi ngay (ORA-28001)
  PASSWORD_REUSE_MAX    5
  PASSWORD_REUSE_TIME   365
  PASSWORD_VERIFY_FUNCTION staff_password_verify;

-- REGISTRAR: không khóa khi sai (tránh bị cố tình khóa để chặn đăng ký), không hết hạn
CREATE PROFILE profile_registrar LIMIT
  FAILED_LOGIN_ATTEMPTS UNLIMITED
  PASSWORD_LIFE_TIME    UNLIMITED;

-- Cho view v_login_audit (05_views.sql) đọc nhật ký và cấp lại cho role_operator
GRANT SELECT ON sys.unified_audit_trail TO transport WITH GRANT OPTION;
```

`CREATE USER … PROFILE profile_staff` trong `PKG_STAFF` chỉ cần quyền `CREATE USER` mà TRANSPORT đã có.

- TRANSPORT giữ profile `DEFAULT`.
- Seed vẫn đạt luật mới: `Operator#01`, `Seller#001`, `Seller#002` đều có chữ hoa, chữ thường, số và `#`, và không chứa username đầy đủ.
- `PKG_STAFF.check_password` **giữ nguyên** (≥ 8 ký tự, cấm `"` để chặn injection trong `EXECUTE IMMEDIATE`). Phần độ phức tạp do verify function kiểm tra khi `CREATE/ALTER USER` và báo `ORA-28003` (app ánh xạ về `InvalidPassword`), để luật chỉ viết một chỗ trong DB.
- `Rules.Password(password, username)` (C#) kiểm tra đủ luật để báo lỗi sớm trên form.

### 2.2 User nhận đăng ký `TRANSPORT_REGISTRAR`

```sql
CREATE USER transport_registrar IDENTIFIED BY "Register#Only1" PROFILE profile_registrar;
GRANT CREATE SESSION TO transport_registrar;
-- (07_grants.sql) chỉ được gọi đúng một thủ tục
GRANT EXECUTE ON pkg_registration_public TO transport_registrar;
```

- Mật khẩu REGISTRAR **cố ý công khai**, nằm trong `appsettings.json`. Nó chỉ gọi được `pkg_registration_public.submit_request`, không đọc hay sửa được bảng nào.
- Thủ tục nộp đơn được tách thành package riêng `pkg_registration_public`. Khi REGISTRAR có `EXECUTE` thì cũng chỉ thấy đúng một thủ tục này, không thấy thủ tục duyệt.

### 2.3 Bảng và package đăng ký (`02_tables.sql`, `04_packages.sql`)

```sql
-- Yêu cầu đăng ký chờ điều hành duyệt; KHÔNG lưu mật khẩu
CREATE TABLE registration_requests (
  request_id    NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  username      VARCHAR2(30)  NOT NULL,
  full_name     VARCHAR2(100) NOT NULL,
  phone         VARCHAR2(10),
  status        VARCHAR2(10) DEFAULT 'PENDING' NOT NULL
                CONSTRAINT ck_reg_status CHECK (status IN ('PENDING','APPROVED','REJECTED')),
  client_host   VARCHAR2(64),                          -- SYS_CONTEXT('USERENV','HOST')
  requested_at  DATE DEFAULT SYSDATE NOT NULL,
  reviewed_by   VARCHAR2(30),
  reviewed_at   DATE,
  reject_reason VARCHAR2(200)
);
```

`PKG_REGISTRATION_PUBLIC` (cấp cho REGISTRAR):

- `submit_request(p_username, p_full_name, p_phone)`:
  - Kiểm tra dữ liệu: username theo regex `^[A-Za-z][A-Za-z0-9_]{2,29}$`, họ tên không rỗng và ≤ 100 ký tự, SĐT để trống hoặc đúng 10 chữ số. Sai thì báo `-20062 Invalid registration data.`
  - Chống spam: quá 50 yêu cầu `PENDING` toàn hệ thống, hoặc quá 3 yêu cầu `PENDING` từ cùng `client_host`, thì báo `-20061 Too many pending requests.`
  - Nếu username đã có yêu cầu `PENDING` thì **im lặng bỏ qua**.
  - Thủ tục **không bao giờ** kiểm tra username đã có trong `STAFF` hay `ALL_USERS`. Mọi trường hợp hợp lệ đều trả về giống nhau, nên không dò được username nào tồn tại.
  - Tự commit.

`PKG_REGISTRATION` (cấp cho `role_operator`):

- `approve_request(p_request_id, p_temp_password, p_role)`:
  - `SELECT … FOR UPDATE` yêu cầu đang `PENDING`, nếu không có thì báo `-20060 Registration request not found or already reviewed.`
  - Gọi `pkg_staff.create_staff(username, p_temp_password, full_name, phone, p_role)`. Thủ tục này tạo user với `PASSWORD EXPIRE`. Nếu trùng username thì ORA-01920 nổi lên cho operator thấy, và yêu cầu vẫn ở `PENDING` để operator từ chối.
  - Sau đó `UPDATE` status `APPROVED`, `reviewed_by = USER`, `reviewed_at = SYSDATE`, rồi commit.
- `reject_request(p_request_id, p_reason)`:
  - Lý do rỗng thì báo `-20062`. Yêu cầu không ở `PENDING` thì báo `-20060`.
  - Ghi `REJECTED`, người duyệt, thời gian và lý do, rồi commit.

`07_grants.sql`: `GRANT SELECT ON registration_requests TO role_operator; GRANT EXECUTE ON pkg_registration TO role_operator;`

### 2.4 Sửa `PKG_STAFF`

- `create_staff`: `CREATE USER … IDENTIFIED BY "…" PROFILE profile_staff PASSWORD EXPIRE`.
- `reset_password`: `ALTER USER … IDENTIFIED BY "…" PASSWORD EXPIRE`. Mật khẩu do operator đặt chỉ dùng được một lần.
- Seed demo đi qua `create_staff` nên tự có `PASSWORD EXPIRE`. Lần đầu đăng nhập bằng mật khẩu công khai sẽ bị bắt đổi.

### 2.5 Nhật ký (Unified Auditing, file mới `10_audit.sql`)

`10_audit.sql` chạy bằng SYS ở **cuối** cài đặt, vì policy thứ hai tham chiếu package của TRANSPORT.

```sql
-- Mọi lần đăng nhập, thành công lẫn thất bại (không phụ thuộc policy mặc định ORA_LOGON_FAILURES)
CREATE AUDIT POLICY pol_transport_logon ACTIONS LOGON;
AUDIT POLICY pol_transport_logon;

-- Mọi thao tác quản trị tài khoản
CREATE AUDIT POLICY pol_transport_account_admin
  ACTIONS EXECUTE ON transport.pkg_staff, EXECUTE ON transport.pkg_registration;
AUDIT POLICY pol_transport_account_admin;
```

Thêm `v_login_audit` (`05_views.sql`), cấp `SELECT` cho `role_operator`. Quyền đọc `sys.unified_audit_trail` đã cấp ở §2.1.

```sql
-- Đăng nhập thành công/thất bại và thao tác quản trị tài khoản
CREATE OR REPLACE VIEW v_login_audit AS
SELECT event_timestamp, dbusername AS username, action_name,
       CASE WHEN return_code = 0 THEN 'SUCCESS' ELSE 'FAILED' END AS result,
       return_code, userhost, object_name
  FROM sys.unified_audit_trail
 WHERE action_name = 'LOGON'
    OR object_name IN ('PKG_STAFF', 'PKG_REGISTRATION');
```

### 2.6 Gỡ cài đặt

`00_uninstall.sql` xóa thêm:
- `transport_registrar`
- `profile_staff` và `profile_registrar` (dùng `DROP PROFILE … CASCADE`)
- `staff_password_verify`
- hai audit policy (phải `NOAUDIT POLICY` trước rồi mới `DROP AUDIT POLICY`)

## 3. Bảng mã lỗi bổ sung (cập nhật spec gốc mục 9)

| Mã | Nguồn | Ý nghĩa | `BusinessErrorCode` |
|---|---|---|---|
| -20044 | verify function, `pkg_staff` | Mật khẩu không đạt luật độ phức tạp | `InvalidPassword` (giữ) |
| -20060 | `pkg_registration` | Yêu cầu không tồn tại hoặc đã xử lý | `RegistrationNotFound` |
| -20061 | `pkg_registration_public` | Quá nhiều yêu cầu đang chờ | `TooManyPendingRequests` |
| -20062 | cả hai package | Dữ liệu đăng ký/lý do không hợp lệ | `InvalidRegistrationData` |
| 28001 | Oracle | Mật khẩu hết hạn | `PasswordExpired` |
| 28003 | Oracle (verify function bọc -20044) | Mật khẩu không đạt luật | ánh xạ về `InvalidPassword` |
| 28007 | Oracle | Không được dùng lại mật khẩu cũ | `PasswordReused` |

Mật khẩu nhập lại không khớp không cần mã lỗi: hộp thoại khóa nút OK và hiện cảnh báo ngay.

Không còn hiện `AccountLocked` (28000) và `InsufficientPrivileges` ở màn hình đăng nhập (xem §4.2).

## 4. Tầng app WPF

### 4.1 Đăng ký

- `LoginWindow` có thêm link **"Đăng ký tài khoản"**, mở `RegisterDialog` (theo mẫu `TextPromptDialog`/`IDialogService`). Các trường: username, họ tên, SĐT.
- **Application:**
  - `IRegistrationService` / `RegistrationService` kiểm tra bằng `Rules.Username`, `Rules.Required` và `Rules.OptionalPhone`, rồi gọi `IRegistrationRepository.SubmitAsync`.
  - Thao tác của operator đặt trong cùng service: `GetPendingAsync`, `ApproveAsync(id, tempPassword, role)`, `RejectAsync(id, reason)`. Các hàm này có `Rules.Operator` và `Rules.Password`.
- **Infrastructure:**
  - `RegistrationRepository`. Riêng `SubmitAsync` mở kết nối bằng `OracleConnectionFactory.OpenAsync(RegistrarUser, RegistrarPassword, ct)`. Các hàm còn lại dùng kết nối của phiên như repository khác.
  - `DatabaseOptions` thêm `RegistrarUser` (mặc định `TRANSPORT_REGISTRAR`) và `RegistrarPassword`.
- Gửi thành công luôn hiện cùng một thông báo: "Đã gửi yêu cầu. Vui lòng chờ điều hành duyệt và cấp mật khẩu tạm."

### 4.2 Đăng nhập

`OracleAuthService.LoginAsync`:
- ORA-01017, ORA-28000, và trường hợp kết nối được nhưng không phải nhân viên hoặc không có role: đều ném `InvalidCredentials` với **một thông điệp chung**: "Tên đăng nhập hoặc mật khẩu không đúng, hoặc tài khoản đang bị khóa."
- ORA-28001: ném `PasswordExpired`.

Thêm `IAuthService.LoginWithNewPasswordAsync(username, oldPassword, newPassword)`:
- `OracleConnectionFactory` thêm hàm mở kết nối **không qua pool** (`Pooling=false`) rồi gọi `OracleConnection.OpenWithNewPassword(newPassword)`.
- Đổi được thì chạy tiếp đúng luồng của `LoginAsync` (đọc role, đọc STAFF, `session.Start`) với mật khẩu mới.
- Lỗi 28003 và 28007 được ánh xạ như §3.

`LoginViewModel`:
- Bắt `PasswordExpired`, mở `ChangePasswordDialog` ở chế độ "bắt buộc" (chỉ có mật khẩu mới và nhập lại).
- Hai lần nhập phải khớp thì nút OK mới bật. Gọi `LoginWithNewPasswordAsync` (Oracle kiểm tra luật qua verify function), thành công thì `ShowMain()`.
- Người dùng hủy hộp thoại thì quay lại form đăng nhập.
- Sau mỗi lần thất bại thì xóa ô mật khẩu.

### 4.3 Tự đổi mật khẩu

- Thanh trên của `MainWindow` có thêm nút **"Đổi mật khẩu"**, mở `ChangePasswordDialog` ở chế độ "thường" (mật khẩu cũ, mới, nhập lại).
- `IAuthService.ChangePasswordAsync(oldPassword, newPassword)` dùng cùng cơ chế `OpenWithNewPassword` trên username của phiên. Thành công thì cập nhật mật khẩu trong `UserSession` và chạy `OracleConnection.ClearAllPools()`.
- Sai mật khẩu cũ thì báo `InvalidCredentials`.

### 4.4 Trang mới (chỉ operator, `AddPage<T>` sau `Menu_Staff`)

| Trang | Thứ tự | Nội dung |
|---|---|---|
| **Yêu cầu đăng ký** (`RegistrationRequestsViewModel`) | 101 | Lưới các yêu cầu, lọc theo trạng thái (mặc định `PENDING`). Nút **Duyệt** mở hộp thoại nhập mật khẩu tạm và chọn role (mặc định `TicketSeller`). Nút **Từ chối** dùng `TextPrompt` để nhập lý do. Có xuất Excel. |
| **Nhật ký đăng nhập** (`LoginAuditViewModel`) | 102 | Chỉ xem `v_login_audit`, lọc theo username và khoảng ngày (mặc định 7 ngày gần nhất), sắp mới nhất trước, xuất Excel. |

Hai trang này theo mẫu `CrudPageViewModel`/`PageViewModel` đang có. Chuỗi giao diện được thêm vào `Strings.resx` và `Strings.vi.resx`.

### 4.5 Lỗi không bắt được

- `App.OnDispatcherUnhandledException` chỉ hiện chuỗi `Err_Unexpected` ("Đã xảy ra lỗi không mong muốn.").
- Chi tiết lỗi chỉ ghi ra bằng `Debug.WriteLine`.

## 5. Kiểm thử

**Unit test (`tests/Transport.UnitTests`)**
- `Rules.Password`:
  - Thiếu từng loại ký tự thì báo lỗi.
  - Có dấu `"` thì báo lỗi.
  - Chứa username thì báo lỗi (cần thêm overload nhận username).
- `RegistrationService`:
  - Dữ liệu sai bị chặn trước khi gọi repository.
  - Seller gọi `Approve`/`Reject` thì bị `OperatorOnly`.
- `OracleErrorMapper`: các mã 20060–20062, 28001, 28003 (về `InvalidPassword`), 28007.
- `LoginViewModel` dùng `FakeAuthService`:
  - Lỗi đăng nhập hiện thông báo chung.
  - `PasswordExpired` mở hộp thoại đổi mật khẩu (fake `IDialogService`).
  - Đổi thành công thì gọi `ShowMain`.
- Các test hiện có (khởi tạo view, resolve DI, kiểm tra đủ resource) phải chạy qua cả hai trang và hai hộp thoại mới.

**SQL (`09_test_scenarios.sql`, chạy bằng TRANSPORT)**
1. REGISTRAR chỉ có đúng một quyền object: `EXECUTE` trên `PKG_REGISTRATION_PUBLIC` (đọc `all_tab_privs`).
2. Gửi yêu cầu thứ 4 từ cùng host thì bị -20061. Gửi trùng username đang chờ thì không lỗi và không thêm dòng. Dữ liệu sai bị -20062.
3. Duyệt tạo dòng STAFF; duyệt lần hai bị -20060; từ chối không có lý do bị -20062.
4. `create_staff` với mật khẩu `abcdefgh` bị -28003 (verify function).

**SQL (`11_security_checks.sql` mới, chạy thủ công bằng SYS ngay sau khi cài)**
1. Mọi user trong STAFF dùng `PROFILE_STAFF`; ba tài khoản demo ở trạng thái `EXPIRED`.
2. REGISTRAR không có role, không có quyền hệ thống nào ngoài `CREATE SESSION`.
3. `PROFILE_STAFF` khóa sau 5 lần sai và dùng `STAFF_PASSWORD_VERIFY`; hai audit policy đang bật.

Trường hợp `reset_password` gắn `PASSWORD EXPIRE` được kiểm tra thủ công (đặt lại mật khẩu rồi đăng nhập thì bị bắt đổi).

**Chạy thật**
1. Đăng nhập `operator01/Operator#01`, bị bắt đổi mật khẩu, đổi xong vào app.
2. Từ màn hình đăng nhập gửi đăng ký. Operator duyệt, người dùng đăng nhập bằng mật khẩu tạm, bị bắt đổi, vào được app.
3. Nhập sai mật khẩu 5 lần thì tài khoản bị khóa, nhưng thông báo vẫn là câu chung.
4. Trang Nhật ký đăng nhập hiện đủ các sự kiện trên.

## 6. Script cài đặt, sao lưu và tài liệu

- `install_all.bat`:
  - Hỏi mật khẩu SYS (giữ như cũ) và **mật khẩu TRANSPORT**, phải nhập hai lần cho khớp.
  - Truyền mật khẩu TRANSPORT vào `01_sys_setup.sql` qua tham số `&1`. Riêng file này bật `SET DEFINE ON` và `SET VERIFY OFF`.
  - Các file `02`–`08` chạy bằng `transport/"%TR_PWD%"`.
  - Chạy thêm `10_audit.sql` bằng SYS ở cuối.
- `backup.bat` / `restore.bat`: hỏi mật khẩu TRANSPORT bằng `set /p`, không còn mật khẩu ghi cứng.
- Mật khẩu vẫn đi qua dòng lệnh `sqlplus`/`expdp`, nên tiến trình khác trên cùng máy có thể đọc được. Rủi ro này chấp nhận được cho máy cài đặt cục bộ, ghi chú lại trong README.
- `README.md`:
  - Giữ bảng tài khoản demo, thêm dòng "Lần đầu đăng nhập phải đổi mật khẩu".
  - Nêu rõ mật khẩu REGISTRAR công khai là cố ý.
  - Bỏ mọi chỗ ghi mật khẩu TRANSPORT.
- `docs/user-guide.md`: thêm các mục Đăng ký tài khoản, Đổi mật khẩu (bắt buộc và tự đổi), Duyệt yêu cầu đăng ký, Nhật ký đăng nhập.
- Spec gốc (mục 7.1, 8.2, 9, 12.3): thay chuỗi mật khẩu TRANSPORT bằng chỗ trống `<mật khẩu do người cài nhập>` và trỏ sang tài liệu này.

## 7. Ngoài phạm vi

- Mã hóa đường truyền (Oracle Native Network Encryption/TCPS): DB cài trên cùng máy với app.
- Xác thực hai bước, khôi phục mật khẩu qua email hoặc SMS: không có hạ tầng gửi tin.
- Chặn operator đặt lại mật khẩu của operator khác: giữ nguyên quyền, thay vào đó **ghi audit** (§2.5) để truy vết.
- Xóa mật khẩu khỏi bộ nhớ (`SecureString`): ODP.NET vẫn cần chuỗi mật khẩu để mở kết nối từ pool, nên làm vậy không thêm lớp bảo vệ thực sự.
