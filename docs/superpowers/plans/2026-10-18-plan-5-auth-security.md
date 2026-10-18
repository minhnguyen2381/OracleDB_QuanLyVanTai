# Đăng ký tài khoản và bảo mật đăng nhập – Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Thêm luồng đăng ký tài khoản chờ duyệt, bắt đổi mật khẩu tạm, tự đổi mật khẩu, nhật ký đăng nhập, và vá các chỗ lộ thông tin ở đăng nhập/cài đặt.

**Architecture:** Mọi luật nằm trong Oracle: profile + verify function (SYS), user `TRANSPORT_REGISTRAR` quyền tối thiểu, package `PKG_REGISTRATION_PUBLIC`/`PKG_REGISTRATION`, Unified Auditing. App WPF chỉ gọi package và dùng `OracleConnection.OpenWithNewPassword` để đổi mật khẩu; lỗi đăng nhập được gộp về một thông báo chung.

**Tech Stack:** Oracle 21c XE (XEPDB1), PL/SQL, SQL*Plus/batch; .NET 10 WPF, CommunityToolkit.Mvvm, Dapper, Oracle.ManagedDataAccess.Core 23.x, xUnit v3 (Microsoft.Testing.Platform).

**Spec:** `docs/superpowers/specs/2026-10-18-auth-security-design.md` (bổ sung cho `docs/superpowers/specs/2026-10-17-transport-management-design.md`).

## Global Constraints

- Định danh SQL (bảng, cột, package, role, profile, giá trị trạng thái) và thông điệp `RAISE_APPLICATION_ERROR` bằng **tiếng Anh**; comment bằng **tiếng Việt**.
- Logic nghiệp vụ nằm trong PL/SQL; app chỉ gọi package/view.
- Mọi chuỗi giao diện qua `Strings.resx` (en) **và** `Strings.vi.resx` (vi) – cùng bộ khóa (test `ResourceParityTests` bắt buộc). Mỗi giá trị `BusinessErrorCode` phải có khóa `Err_<số>`.
- Build không được có cảnh báo (`TreatWarningsAsErrors=true`, `EnforceCodeStyleInBuild=true`).
- Lệnh test: `dotnet test --project tests/Transport.UnitTests` (lọc: thêm `--filter-class "*TenLop"`).
- CLAUDE.md: **trước khi sửa** một hàm/lớp C# đã có, chạy `node .gitnexus/run.cjs impact "<Symbol>" --direction upstream --repo .` và báo rủi ro; **trước mỗi commit** chạy `node .gitnexus/run.cjs detect-changes --scope all --repo .` (nếu index cũ: `node .gitnexus/run.cjs analyze --index-only` trước).
- Commit kết thúc bằng:
  ```
  Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
  Claude-Session: https://claude.ai/code/session_016qy8FyinGcSgxG5Rh4VUSa
  ```
- Mật khẩu REGISTRAR `Register#Only1` công khai có chủ đích. Không được ghi mật khẩu `TRANSPORT` vào bất kỳ file nào.
- Luật mật khẩu nhân viên: ≥ 8 ký tự, có A–Z, a–z, 0–9, ký tự khác chữ/số; không chứa `"`; không chứa username (không phân biệt hoa thường).

## Ghi chú cho người thực thi

- Các bước chạy SQL cần mật khẩu SYS/TRANSPORT mà agent **không có**. Ở các bước đó: dừng lại, nhờ người dùng tự chạy lệnh trong cửa sổ `cmd` riêng (script hỏi mật khẩu bằng `set /p`) và dán kết quả. `install_all.bat` **xóa và cài lại** toàn bộ CSDL – nhắc người dùng trước khi chạy.
- Task 1–3 đều sửa SQL; có thể gộp một lần cài + chạy test ở cuối Task 3 nếu người dùng muốn.
- Sau khi cài lại, tài khoản demo bị bắt đổi mật khẩu. Integration test đọc mật khẩu mới từ biến môi trường `TRANSPORT_DB_OPERATOR_PASSWORD` / `TRANSPORT_DB_SELLER_PASSWORD` (Task 5).

## File Structure

| File | Trách nhiệm |
|---|---|
| `database/01_sys_setup.sql` (sửa) | Nhận mật khẩu TRANSPORT qua `&1`; verify function, 2 profile, user REGISTRAR, quyền đọc audit |
| `database/02_tables.sql` (sửa) | Bảng `REGISTRATION_REQUESTS` |
| `database/04_packages.sql` (sửa) | `PKG_STAFF` gắn profile + `PASSWORD EXPIRE`; `PKG_REGISTRATION_PUBLIC`, `PKG_REGISTRATION` |
| `database/05_views.sql` (sửa) | `V_LOGIN_AUDIT` |
| `database/07_grants.sql` (sửa) | Quyền cho REGISTRAR và `role_operator` |
| `database/10_audit.sql` (mới) | Hai audit policy (chạy bằng SYS cuối cùng) |
| `database/11_security_checks.sql` (mới) | Kiểm tra cấu hình bảo mật bằng SYS, in PASS/FAIL |
| `database/00_uninstall.sql`, `install_all.bat`, `backup.bat`, `restore.bat` (sửa) | Gỡ object mới; hỏi mật khẩu thay vì ghi cứng |
| `database/09_test_scenarios.sql` (sửa) | Kịch bản 10e, 12, 13 |
| `src/Transport.Domain/Errors/BusinessErrorCode.cs`, `Enums/RegistrationStatus.cs` | Mã lỗi mới, trạng thái yêu cầu |
| `src/Transport.Application/Services/Rules.cs`, `StaffService.cs`, `RegistrationService.cs` | Luật mật khẩu; dịch vụ đăng ký |
| `src/Transport.Application/Abstractions/IAuthService.cs`, `IRegistrationRepository.cs`, `ILoginAuditQueries.cs` | Giao diện |
| `src/Transport.Application/Models/RegistrationRequest.cs`, `LoginAuditEntry.cs` | Model đọc từ DB |
| `src/Transport.Infrastructure/Persistence/OracleConnectionFactory.cs`, `OracleAuthService.cs`, `OracleRepository.cs`, `OracleErrorMapper.cs`, `DatabaseOptions.cs` | Đổi mật khẩu khi mở kết nối, lỗi đăng nhập chung, kết nối REGISTRAR |
| `src/Transport.Infrastructure/Repositories/RegistrationRepository.cs`, `LoginAuditQueries.cs` | Truy cập DB |
| `src/Transport.Presentation.Wpf/ViewModels/Dialogs/{Register,ChangePassword,ApproveRegistration}ViewModel.cs` + `Views/Dialogs/*Dialog.xaml(.cs)` | 3 hộp thoại |
| `src/Transport.Presentation.Wpf/ViewModels/Pages/{RegistrationRequests,LoginAudit}ViewModel.cs` + `Views/Pages/*Page.xaml(.cs)` | 2 trang mới |
| `LoginViewModel.cs`, `LoginWindow.xaml`, `MainViewModel.cs`, `MainWindow.xaml`, `App.xaml.cs`, `Services/(I)DialogService.cs`, `PresentationServices.cs`, `PageTemplates.xaml`, `Resources/Strings*.resx`, `appsettings.json` | Nối luồng UI |
| `README.md`, `docs/user-guide.md`, spec gốc | Tài liệu |

---

### Task 1: Luật mật khẩu Oracle, mật khẩu tạm hết hạn ngay, bỏ mật khẩu ghi cứng

**Files:**
- Modify: `database/01_sys_setup.sql`, `database/04_packages.sql` (PKG_STAFF), `database/00_uninstall.sql`, `database/install_all.bat`, `database/backup.bat`, `database/restore.bat`, `database/09_test_scenarios.sql`
- Create: `database/11_security_checks.sql`

**Interfaces:**
- Produces: function `SYS.STAFF_PASSWORD_VERIFY`, profile `PROFILE_STAFF`, `PROFILE_REGISTRAR`; `pkg_staff.create_staff`/`reset_password` tạo mật khẩu `PASSWORD EXPIRE`; `01_sys_setup.sql` nhận tham số `&1` = mật khẩu TRANSPORT.

- [ ] **Step 1: Viết test SQL trước – thêm kịch bản 10e vào `09_test_scenarios.sql`**

Ngay sau dòng `expect_error('10d. unknown staff is rejected', ...` (2 dòng), thêm:

```sql
  -- Mật khẩu đủ 8 ký tự nhưng yếu: verify function của PROFILE_STAFF từ chối (ORA-28003)
  expect_error('10e. weak password is rejected by the verify function', -28003,
    'BEGIN pkg_staff.create_staff(''test_staff_01'', ''abcdefgh'', ''X'', NULL, ''ROLE_OPERATOR''); END;');
```

- [ ] **Step 2: Tạo `database/11_security_checks.sql`**

```sql
-- Kiểm tra cấu hình bảo mật sau khi cài (chạy THỦ CÔNG bằng SYS trong XEPDB1, ngay sau install_all.bat).
-- In PASS/FAIL từng mục. Mục 2 chỉ đúng khi chưa ai đăng nhập bằng tài khoản demo.
WHENEVER SQLERROR EXIT FAILURE ROLLBACK
SET DEFINE OFF
SET SERVEROUTPUT ON SIZE UNLIMITED

DECLARE
  g_fail PLS_INTEGER := 0;
  v_n    NUMBER;

  PROCEDURE ok(p_name VARCHAR2, p_cond BOOLEAN) IS
  BEGIN
    IF p_cond THEN
      DBMS_OUTPUT.PUT_LINE('PASS  ' || p_name);
    ELSE
      g_fail := g_fail + 1;
      DBMS_OUTPUT.PUT_LINE('FAIL  ' || p_name);
    END IF;
  END ok;
BEGIN
  -- 1. Mọi nhân viên dùng PROFILE_STAFF
  SELECT COUNT(*) INTO v_n
    FROM transport.staff s JOIN dba_users u ON u.username = s.username
   WHERE u.profile <> 'PROFILE_STAFF';
  ok('1. all staff accounts use PROFILE_STAFF', v_n = 0);

  -- 2. Tài khoản demo phải đổi mật khẩu ở lần đăng nhập đầu
  SELECT COUNT(*) INTO v_n FROM dba_users
   WHERE username IN ('OPERATOR01', 'SELLER01', 'SELLER02') AND account_status LIKE 'EXPIRED%';
  ok('2. demo accounts are EXPIRED right after install', v_n = 3);

  -- 4. Profile khóa sau 5 lần sai và dùng hàm kiểm tra mật khẩu
  SELECT COUNT(*) INTO v_n FROM dba_profiles
   WHERE profile = 'PROFILE_STAFF'
     AND ((resource_name = 'FAILED_LOGIN_ATTEMPTS' AND limit = '5')
       OR (resource_name = 'PASSWORD_VERIFY_FUNCTION' AND limit = 'STAFF_PASSWORD_VERIFY'));
  ok('4. PROFILE_STAFF locks after 5 failures and verifies passwords', v_n = 2);

  DBMS_OUTPUT.PUT_LINE(CASE WHEN g_fail = 0 THEN 'ALL CHECKS PASSED' ELSE g_fail || ' CHECK(S) FAILED' END);
END;
/

EXIT
```

(Mục 3 và 5 được thêm ở Task 2 và Task 3.)

- [ ] **Step 3: Sửa `database/01_sys_setup.sql`**

Thay dòng ngay sau `WHENEVER SQLERROR ...`:

```sql
SET DEFINE OFF
```
bằng:
```sql
-- Bật thay biến để nhận mật khẩu TRANSPORT từ install_all.bat (tham số &1); không in giá trị ra màn hình
SET DEFINE ON
SET VERIFY OFF
```

Thay khối tạo user TRANSPORT:
```sql
-- Chủ schema: chỉ dùng để tạo/bảo trì object, app không đăng nhập bằng user này
CREATE USER transport IDENTIFIED BY "Transport#2026" DEFAULT TABLESPACE users QUOTA UNLIMITED ON users;
```
bằng:
```sql
-- Chủ schema: chỉ dùng để tạo/bảo trì object, app không đăng nhập bằng user này.
-- Mật khẩu do người cài nhập lúc chạy install_all.bat, không ghi trong mã nguồn.
CREATE USER transport IDENTIFIED BY "&1" DEFAULT TABLESPACE users QUOTA UNLIMITED ON users;
```

Thêm ngay trước dòng `-- Thư mục cho Data Pump`:

```sql
-- Luật mật khẩu nhân viên: >= 8 ký tự, có hoa, thường, số, ký tự đặc biệt,
-- không chứa dấu nháy kép và không chứa username (áp dụng khi CREATE/ALTER USER, lỗi ORA-28003)
CREATE OR REPLACE FUNCTION staff_password_verify(
  p_username     IN VARCHAR2,
  p_password     IN VARCHAR2,
  p_old_password IN VARCHAR2) RETURN BOOLEAN IS
BEGIN
  IF p_password IS NULL OR LENGTH(p_password) < 8
     OR NOT REGEXP_LIKE(p_password, '[A-Z]', 'c')
     OR NOT REGEXP_LIKE(p_password, '[a-z]', 'c')
     OR NOT REGEXP_LIKE(p_password, '[0-9]')
     OR NOT REGEXP_LIKE(p_password, '[^A-Za-z0-9]')
     OR INSTR(p_password, '"') > 0
     OR INSTR(UPPER(p_password), UPPER(p_username)) > 0 THEN
    RAISE_APPLICATION_ERROR(-20044, 'Password does not meet the complexity rules.');
  END IF;
  RETURN TRUE;
END staff_password_verify;
/

-- Profile của nhân viên: khóa 15 phút sau 5 lần sai, đổi mật khẩu mỗi 90 ngày, không dùng lại 5 mật khẩu gần nhất
CREATE PROFILE profile_staff LIMIT
  FAILED_LOGIN_ATTEMPTS    5
  PASSWORD_LOCK_TIME       15/1440
  PASSWORD_LIFE_TIME       90
  PASSWORD_GRACE_TIME      0
  PASSWORD_REUSE_MAX       5
  PASSWORD_REUSE_TIME      365
  PASSWORD_VERIFY_FUNCTION staff_password_verify;

-- Profile của user nhận đăng ký: không khóa khi sai (tránh bị cố tình khóa để chặn đăng ký), không hết hạn
CREATE PROFILE profile_registrar LIMIT
  FAILED_LOGIN_ATTEMPTS UNLIMITED
  PASSWORD_LIFE_TIME    UNLIMITED;

```

Kiểm tra lại toàn file không còn ký tự `&` nào ngoài `"&1"` (SQL*Plus sẽ hỏi biến).

- [ ] **Step 4: Sửa `PKG_STAFF` trong `database/04_packages.sql`**

Thay comment + thân `check_password`:
```sql
  -- Mật khẩu tối thiểu 8 ký tự, không chứa dấu nháy kép (vì được bọc trong "...")
```
bằng:
```sql
  -- Chặn dấu nháy kép (mật khẩu được bọc trong "..." khi EXECUTE IMMEDIATE) và độ dài tối thiểu.
  -- Luật độ phức tạp do STAFF_PASSWORD_VERIFY của PROFILE_STAFF kiểm tra (ORA-28003).
```

Trong `create_staff` thay:
```sql
    EXECUTE IMMEDIATE 'CREATE USER ' || v_user || ' IDENTIFIED BY "' || p_password || '"';
```
bằng:
```sql
    -- Mật khẩu do điều hành đặt chỉ là mật khẩu tạm: nhân viên phải đổi ở lần đăng nhập đầu
    EXECUTE IMMEDIATE 'CREATE USER ' || v_user || ' IDENTIFIED BY "' || p_password
                      || '" PROFILE profile_staff PASSWORD EXPIRE';
```

Trong `reset_password` thay:
```sql
    EXECUTE IMMEDIATE 'ALTER USER ' || v_user || ' IDENTIFIED BY "' || p_password || '"';
```
bằng:
```sql
    EXECUTE IMMEDIATE 'ALTER USER ' || v_user || ' IDENTIFIED BY "' || p_password || '" PASSWORD EXPIRE';
```

- [ ] **Step 5: Sửa `database/00_uninstall.sql`**

Thêm sau `DROP DIRECTORY backup_dir;`:
```sql
-- Profile CASCADE chuyển user còn lại về DEFAULT; xóa hàm sau khi profile không còn dùng nó
DROP PROFILE profile_staff CASCADE;
DROP PROFILE profile_registrar CASCADE;
DROP FUNCTION staff_password_verify;
```

- [ ] **Step 6: Viết lại `database/install_all.bat`**

```bat
@echo off
:: Cài lại toàn bộ CSDL vận tải (XÓA dữ liệu cũ). Cần mật khẩu SYS của Oracle XE.
:: Mật khẩu chủ schema TRANSPORT do người cài tự đặt, không lưu ở file nào (không dùng ký tự " hoặc %).
setlocal
cd /d "%~dp0"
set NLS_LANG=AMERICAN_AMERICA.AL32UTF8
set DB=//localhost:1521/XEPDB1

echo This will DROP and recreate schema TRANSPORT, its roles and staff users.
set /p CONFIRM=Continue? (Y/N): 
if /i not "%CONFIRM%"=="Y" exit /b 1
set /p SYS_PWD=Enter SYS password: 
set /p TR_PWD=Choose a password for schema owner TRANSPORT: 
set /p TR_PWD2=Repeat the TRANSPORT password: 
if "%TR_PWD%"=="" (echo Password must not be empty. & exit /b 1)
if not "%TR_PWD%"=="%TR_PWD2%" (echo Passwords do not match. & exit /b 1)

if not exist D:\OracleBackup mkdir D:\OracleBackup

sqlplus -S -L "sys/%SYS_PWD%@%DB% as sysdba" @00_uninstall.sql
sqlplus -S -L "sys/%SYS_PWD%@%DB% as sysdba" @01_sys_setup.sql "%TR_PWD%" || goto :fail

for %%f in (02_tables 03_triggers 04_packages 05_views 06_backup 07_grants 08_seed_data) do (
  echo === %%f ===
  sqlplus -S -L transport/"%TR_PWD%"@%DB% @%%f.sql || goto :fail
)

echo.
echo INSTALL OK. Demo accounts (a new password is required at first sign-in):
echo   operator01/Operator#01, seller01/Seller#001, seller02/Seller#002
exit /b 0

:fail
echo.
echo INSTALL FAILED - see the error above.
exit /b 1
```

- [ ] **Step 7: Viết lại `database/backup.bat` và `database/restore.bat`**

`backup.bat`:
```bat
@echo off
:: Xuất schema TRANSPORT bằng Data Pump; hỏi mật khẩu TRANSPORT (không ghi trong file)
setlocal
set NLS_LANG=AMERICAN_AMERICA.AL32UTF8
set /p TR_PWD=Enter TRANSPORT password: 
for /f %%i in ('powershell -NoProfile -Command "Get-Date -Format yyyyMMdd_HHmm"') do set TS=%%i
expdp transport/"%TR_PWD%"@localhost:1521/XEPDB1 schemas=TRANSPORT directory=BACKUP_DIR dumpfile=transport_%TS%.dmp logfile=export_%TS%.log
```

`restore.bat`:
```bat
@echo off
:: Nhập lại schema TRANSPORT từ file dump; hỏi mật khẩu TRANSPORT (không ghi trong file)
setlocal
set NLS_LANG=AMERICAN_AMERICA.AL32UTF8
if "%~1"=="" (echo Usage: restore.bat transport_YYYYMMDD_HHMM.dmp & exit /b 1)
set /p TR_PWD=Enter TRANSPORT password: 
impdp transport/"%TR_PWD%"@localhost:1521/XEPDB1 schemas=TRANSPORT directory=BACKUP_DIR dumpfile=%1 table_exists_action=REPLACE
```

- [ ] **Step 8: Xác nhận không còn mật khẩu TRANSPORT trong mã**

Run: `git grep -n "Transport#2026" -- database src tests`
Expected: không có kết quả.

- [ ] **Step 9: Nhờ người dùng cài lại và chạy test SQL**

Nhắc người dùng: lệnh sau XÓA dữ liệu hiện có. Người dùng chạy trong `cmd`:
```
cd /d D:\DuAn\OracleDB_QuanLyVanTai\database
install_all.bat
sqlplus -S -L transport/"<mật khẩu TRANSPORT>"@//localhost:1521/XEPDB1 @09_test_scenarios.sql
sqlplus -S -L "sys/<mật khẩu SYS>@//localhost:1521/XEPDB1 as sysdba" @11_security_checks.sql
```
Expected: `INSTALL OK`, `ALL TESTS PASSED` (có dòng `PASS  10e. weak password ...`), `ALL CHECKS PASSED`.

- [ ] **Step 10: Commit**

```bash
node .gitnexus/run.cjs detect-changes --scope all --repo .
git add database/
git commit -m "feat(db): staff password profile, expiring temp passwords, no hardcoded schema password"
```

---

### Task 2: Đăng ký tài khoản trong Oracle (bảng, 2 package, user REGISTRAR)

**Files:**
- Modify: `database/01_sys_setup.sql`, `database/02_tables.sql`, `database/04_packages.sql`, `database/07_grants.sql`, `database/00_uninstall.sql`, `database/09_test_scenarios.sql`, `database/11_security_checks.sql`

**Interfaces:**
- Consumes: `pkg_staff.create_staff` (Task 1).
- Produces:
  - `pkg_registration_public.submit_request(p_username IN VARCHAR2, p_full_name IN VARCHAR2, p_phone IN VARCHAR2)` – lỗi -20061, -20062
  - `pkg_registration.approve_request(p_request_id IN NUMBER, p_temp_password IN VARCHAR2, p_role IN VARCHAR2)` – lỗi -20060 (+ lỗi của `create_staff`)
  - `pkg_registration.reject_request(p_request_id IN NUMBER, p_reason IN VARCHAR2)` – lỗi -20060, -20062
  - Bảng `registration_requests(request_id, username, full_name, phone, status, client_host, requested_at, reviewed_by, reviewed_at, reject_reason)`; status `PENDING|APPROVED|REJECTED`
  - User `TRANSPORT_REGISTRAR` / `Register#Only1`

- [ ] **Step 1: Viết test SQL trước – kịch bản 12, 13 và dọn dữ liệu**

Trong `09_test_scenarios.sql`, thủ tục `cleanup`, thêm ngay sau `BEGIN EXECUTE IMMEDIATE 'DROP USER test_staff_01'; ...`:
```sql
    DELETE FROM staff WHERE username = 'TEST_REG_01';
    BEGIN EXECUTE IMMEDIATE 'DROP USER test_reg_01'; EXCEPTION WHEN OTHERS THEN NULL; END;
    DELETE FROM registration_requests WHERE username LIKE 'TEST\_REG\_%' ESCAPE '\';
```

Thêm ngay **trước** dòng `cleanup;` cuối khối chính (sau `EXECUTE IMMEDIATE 'DROP USER test_staff_01';`):
```sql

  -- 12. Đăng ký tài khoản: gửi, chống spam, duyệt, từ chối
  expect_error('12a. invalid registration username is rejected', -20062,
    'BEGIN pkg_registration_public.submit_request(''x;'', ''Test Reg'', NULL); END;');
  expect_error('12b. invalid registration phone is rejected', -20062,
    'BEGIN pkg_registration_public.submit_request(''test_reg_01'', ''Test Reg'', ''123''); END;');
  pkg_registration_public.submit_request('test_reg_01', 'Test Reg', '0999000001');
  pkg_registration_public.submit_request('test_reg_01', 'Test Reg', '0999000001');
  SELECT COUNT(*) INTO v_n FROM registration_requests WHERE username = 'TEST_REG_01' AND status = 'PENDING';
  ok('12c. duplicate pending request is silently ignored', v_n = 1);
  -- Tối đa 3 yêu cầu đang chờ từ một máy: trong 4 lần gửi tiếp theo phải có ít nhất 2 lần bị chặn
  v_m := 0;
  FOR i IN 2 .. 5 LOOP
    BEGIN
      pkg_registration_public.submit_request('test_reg_0' || i, 'Test Reg', NULL);
    EXCEPTION
      WHEN OTHERS THEN
        IF SQLCODE = -20061 THEN v_m := v_m + 1; ELSE RAISE; END IF;
    END;
  END LOOP;
  ok('12d. more than 3 pending requests from one computer are rejected', v_m >= 2);

  SELECT request_id INTO v_n FROM registration_requests WHERE username = 'TEST_REG_01' AND status = 'PENDING';
  pkg_registration.approve_request(v_n, 'TempPass#1', 'ROLE_TICKET_SELLER');
  SELECT COUNT(*) INTO v_m FROM staff WHERE username = 'TEST_REG_01' AND role_name = 'ROLE_TICKET_SELLER';
  ok('12e. approval creates the staff account', v_m = 1);
  expect_error('12f. approving twice is rejected', -20060,
    'BEGIN pkg_registration.approve_request(' || v_n || ', ''TempPass#1'', ''ROLE_TICKET_SELLER''); END;');

  SELECT request_id INTO v_n FROM registration_requests WHERE username = 'TEST_REG_02' AND status = 'PENDING';
  expect_error('12g. rejecting without a reason is rejected', -20062,
    'BEGIN pkg_registration.reject_request(' || v_n || ', '' ''); END;');
  pkg_registration.reject_request(v_n, 'Test');
  SELECT COUNT(*) INTO v_m FROM registration_requests
   WHERE request_id = v_n AND status = 'REJECTED' AND reviewed_by = USER AND reject_reason = 'Test';
  ok('12h. rejection is recorded', v_m = 1);

  -- 13. REGISTRAR chỉ được gọi đúng một package
  SELECT COUNT(*) INTO v_n FROM all_tab_privs WHERE grantee = 'TRANSPORT_REGISTRAR';
  SELECT COUNT(*) INTO v_m FROM all_tab_privs
   WHERE grantee = 'TRANSPORT_REGISTRAR' AND privilege = 'EXECUTE' AND table_name = 'PKG_REGISTRATION_PUBLIC';
  ok('13. registrar can only execute PKG_REGISTRATION_PUBLIC', v_n = 1 AND v_m = 1);
```

Trong phần comment "Kiểm tra phân quyền thủ công" cuối file, thêm:
```sql
-- Đăng nhập transport_registrar/Register#Only1 rồi chạy:
--   SELECT COUNT(*) FROM transport.staff;
--     -> ORA-00942 (REGISTRAR không đọc được bảng nào)
--   EXEC transport.pkg_registration.reject_request(1, 'x')
--     -> PLS-00201 (REGISTRAR không gọi được thủ tục duyệt)
```

- [ ] **Step 2: Tạo user REGISTRAR trong `01_sys_setup.sql`**

Thêm ngay sau khối `CREATE PROFILE profile_registrar ...;`:
```sql
-- User nhận đăng ký: mật khẩu công khai có chủ đích (nằm trong appsettings.json của app),
-- chỉ được gọi PKG_REGISTRATION_PUBLIC.SUBMIT_REQUEST (cấp ở 07_grants.sql)
CREATE USER transport_registrar IDENTIFIED BY "Register#Only1" PROFILE profile_registrar;
GRANT CREATE SESSION TO transport_registrar;
```

- [ ] **Step 3: Thêm bảng vào `02_tables.sql`** (ngay sau bảng `staff`, trước `EXIT`)

```sql

-- Yêu cầu đăng ký tài khoản chờ điều hành duyệt; KHÔNG lưu mật khẩu
CREATE TABLE registration_requests (
  request_id    NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  username      VARCHAR2(30)  NOT NULL,
  full_name     VARCHAR2(100) NOT NULL,
  phone         VARCHAR2(10),
  status        VARCHAR2(10) DEFAULT 'PENDING' NOT NULL
                CONSTRAINT ck_reg_status CHECK (status IN ('PENDING','APPROVED','REJECTED')),
  client_host   VARCHAR2(64),                 -- Máy gửi yêu cầu (SYS_CONTEXT('USERENV','HOST'))
  requested_at  DATE DEFAULT SYSDATE NOT NULL,
  reviewed_by   VARCHAR2(30),
  reviewed_at   DATE,
  reject_reason VARCHAR2(200)
);
CREATE INDEX idx_reg_status ON registration_requests(status);
```

- [ ] **Step 4: Thêm 2 package vào `04_packages.sql`** (ngay sau `END pkg_staff;` + `/`, trước khối kiểm tra `user_errors`)

```sql

CREATE OR REPLACE PACKAGE pkg_registration_public AS
  -- Người chưa có tài khoản gửi yêu cầu (gọi bằng user TRANSPORT_REGISTRAR).
  -- Không bao giờ cho biết username đã tồn tại, để không dò được tài khoản.
  PROCEDURE submit_request(p_username IN VARCHAR2, p_full_name IN VARCHAR2, p_phone IN VARCHAR2);
END pkg_registration_public;
/

CREATE OR REPLACE PACKAGE BODY pkg_registration_public AS

  c_max_pending          CONSTANT PLS_INTEGER := 50;  -- Tổng số yêu cầu đang chờ
  c_max_pending_per_host CONSTANT PLS_INTEGER := 3;   -- Số yêu cầu đang chờ từ một máy

  PROCEDURE submit_request(p_username IN VARCHAR2, p_full_name IN VARCHAR2, p_phone IN VARCHAR2) IS
    v_name      VARCHAR2(4000) := TRIM(p_full_name);
    v_host      VARCHAR2(64)   := SUBSTR(SYS_CONTEXT('USERENV', 'HOST'), 1, 64);
    v_user      VARCHAR2(30);
    v_total     PLS_INTEGER;
    v_from_host PLS_INTEGER;
  BEGIN
    IF p_username IS NULL OR NOT REGEXP_LIKE(p_username, '^[A-Za-z][A-Za-z0-9_]{2,29}$')
       OR v_name IS NULL OR LENGTH(v_name) > 100
       OR (p_phone IS NOT NULL AND NOT REGEXP_LIKE(p_phone, '^0[0-9]{9}$')) THEN
      RAISE_APPLICATION_ERROR(-20062, 'Invalid registration data.');
    END IF;
    v_user := UPPER(p_username);

    -- Đã có yêu cầu đang chờ cho username này: bỏ qua, trả về như thành công
    SELECT COUNT(*) INTO v_total FROM registration_requests WHERE username = v_user AND status = 'PENDING';
    IF v_total > 0 THEN
      RETURN;
    END IF;

    -- Chống spam
    SELECT COUNT(*), COUNT(CASE WHEN client_host = v_host THEN 1 END)
      INTO v_total, v_from_host
      FROM registration_requests
     WHERE status = 'PENDING';
    IF v_total >= c_max_pending OR v_from_host >= c_max_pending_per_host THEN
      RAISE_APPLICATION_ERROR(-20061, 'Too many pending requests.');
    END IF;

    INSERT INTO registration_requests (username, full_name, phone, client_host)
    VALUES (v_user, v_name, p_phone, v_host);
    COMMIT;
  END submit_request;

END pkg_registration_public;
/

CREATE OR REPLACE PACKAGE pkg_registration AS
  -- Duyệt: tạo user Oracle qua PKG_STAFF (mật khẩu tạm, hết hạn ngay) rồi đánh dấu APPROVED
  PROCEDURE approve_request(p_request_id IN NUMBER, p_temp_password IN VARCHAR2, p_role IN VARCHAR2);
  -- Từ chối kèm lý do
  PROCEDURE reject_request(p_request_id IN NUMBER, p_reason IN VARCHAR2);
END pkg_registration;
/

CREATE OR REPLACE PACKAGE BODY pkg_registration AS

  -- Yêu cầu phải đang chờ duyệt
  FUNCTION pending_request(p_request_id IN NUMBER) RETURN registration_requests%ROWTYPE IS
    v_req registration_requests%ROWTYPE;
  BEGIN
    SELECT * INTO v_req FROM registration_requests WHERE request_id = p_request_id AND status = 'PENDING';
    RETURN v_req;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      RAISE_APPLICATION_ERROR(-20060, 'Registration request not found or already reviewed.');
  END pending_request;

  PROCEDURE approve_request(p_request_id IN NUMBER, p_temp_password IN VARCHAR2, p_role IN VARCHAR2) IS
    v_req registration_requests%ROWTYPE := pending_request(p_request_id);
  BEGIN
    -- create_staff tự kiểm tra username/role/mật khẩu và tự commit.
    -- Nếu lỗi (VD trùng username ORA-01920) thì yêu cầu vẫn PENDING để điều hành từ chối.
    pkg_staff.create_staff(v_req.username, p_temp_password, v_req.full_name, v_req.phone, p_role);
    UPDATE registration_requests
       SET status = 'APPROVED', reviewed_by = USER, reviewed_at = SYSDATE
     WHERE request_id = p_request_id;
    COMMIT;
  END approve_request;

  PROCEDURE reject_request(p_request_id IN NUMBER, p_reason IN VARCHAR2) IS
    v_req registration_requests%ROWTYPE := pending_request(p_request_id);
  BEGIN
    IF TRIM(p_reason) IS NULL OR LENGTH(TRIM(p_reason)) > 200 THEN
      RAISE_APPLICATION_ERROR(-20062, 'Invalid registration data.');
    END IF;
    UPDATE registration_requests
       SET status = 'REJECTED', reviewed_by = USER, reviewed_at = SYSDATE, reject_reason = TRIM(p_reason)
     WHERE request_id = v_req.request_id;
    COMMIT;
  END reject_request;

END pkg_registration;
/
```

- [ ] **Step 5: Cấp quyền trong `07_grants.sql`**

Trong khối "Điều hành", thêm sau `GRANT EXECUTE ON backup_operational_data TO role_operator;`:
```sql
GRANT SELECT ON registration_requests  TO role_operator;
GRANT EXECUTE ON pkg_registration      TO role_operator;
```
Thêm trước `EXIT`:
```sql

-- Người chưa có tài khoản: chỉ được gửi yêu cầu đăng ký, không đọc được bảng nào
GRANT EXECUTE ON pkg_registration_public TO transport_registrar;
```

- [ ] **Step 6: Cập nhật `00_uninstall.sql`**

Trong danh sách user dọn dự phòng, đổi `('OPERATOR01', 'SELLER01', 'SELLER02', 'TEST_STAFF_01')` thành `('OPERATOR01', 'SELLER01', 'SELLER02', 'TEST_STAFF_01', 'TEST_REG_01')`.
Thêm ngay sau `DROP USER transport CASCADE;`:
```sql
DROP USER transport_registrar CASCADE;
```

- [ ] **Step 7: Thêm mục 3 vào `11_security_checks.sql`** (trước mục 4)

```sql
  -- 3. REGISTRAR: không role, không quyền hệ thống nào ngoài CREATE SESSION, chỉ EXECUTE một package
  SELECT COUNT(*) INTO v_n FROM dba_role_privs WHERE grantee = 'TRANSPORT_REGISTRAR';
  ok('3a. registrar has no role', v_n = 0);
  SELECT COUNT(*) INTO v_n FROM dba_sys_privs WHERE grantee = 'TRANSPORT_REGISTRAR' AND privilege <> 'CREATE SESSION';
  ok('3b. registrar has no system privilege besides CREATE SESSION', v_n = 0);
  SELECT COUNT(*) INTO v_n FROM dba_tab_privs
   WHERE grantee = 'TRANSPORT_REGISTRAR' AND NOT (privilege = 'EXECUTE' AND table_name = 'PKG_REGISTRATION_PUBLIC');
  ok('3c. registrar can only execute PKG_REGISTRATION_PUBLIC', v_n = 0);

```

- [ ] **Step 8: Nhờ người dùng cài lại và chạy test** (giống Task 1 Step 9)

Expected: `ALL TESTS PASSED` với các dòng `PASS  12a` … `PASS  13`; `ALL CHECKS PASSED` với `3a`–`3c`. Người dùng thử thêm phần "kiểm tra thủ công" của REGISTRAR.

- [ ] **Step 9: Commit**

```bash
node .gitnexus/run.cjs detect-changes --scope all --repo .
git add database/
git commit -m "feat(db): registration requests with least-privilege registrar account"
```

---

### Task 3: Nhật ký đăng nhập (Unified Auditing + view)

**Files:**
- Create: `database/10_audit.sql`
- Modify: `database/01_sys_setup.sql`, `database/05_views.sql`, `database/07_grants.sql`, `database/00_uninstall.sql`, `database/install_all.bat`, `database/11_security_checks.sql`

**Interfaces:**
- Produces: view `v_login_audit(event_time TIMESTAMP, username, action_name, result 'SUCCESS'|'FAILED', return_code, userhost, object_name)`, `SELECT` cấp cho `role_operator`.

- [ ] **Step 1: Thêm mục 5 vào `11_security_checks.sql`** (sau mục 4, trước dòng in tổng kết)

```sql

  -- 5. Hai audit policy đang bật
  SELECT COUNT(*) INTO v_n FROM audit_unified_enabled_policies
   WHERE policy_name IN ('POL_TRANSPORT_LOGON', 'POL_TRANSPORT_ACCOUNT_ADMIN');
  ok('5. audit policies are enabled', v_n = 2);
```

- [ ] **Step 2: Cho TRANSPORT đọc nhật ký – `01_sys_setup.sql`**

Thêm ngay sau `GRANT SELECT ON sys.dba_scheduler_jobs TO transport WITH GRANT OPTION;`:
```sql
-- Cho view V_LOGIN_AUDIT đọc nhật ký Unified Auditing và cấp lại cho ROLE_OPERATOR
GRANT SELECT ON sys.unified_audit_trail TO transport WITH GRANT OPTION;
```

- [ ] **Step 3: Tạo `database/10_audit.sql`**

```sql
-- Bật Unified Auditing cho đăng nhập và thao tác quản trị tài khoản (chạy bằng SYS trong XEPDB1).
-- Chạy SAU 04_packages.sql vì policy thứ hai tham chiếu package của TRANSPORT.
WHENEVER SQLERROR EXIT FAILURE ROLLBACK
SET DEFINE OFF
SET SERVEROUTPUT ON SIZE UNLIMITED

-- Mọi lần đăng nhập, thành công lẫn thất bại
CREATE AUDIT POLICY pol_transport_logon ACTIONS LOGON;
AUDIT POLICY pol_transport_logon;

-- Mọi lần tạo/khóa/đổi quyền/đặt lại mật khẩu nhân viên và duyệt/từ chối đăng ký
CREATE AUDIT POLICY pol_transport_account_admin
  ACTIONS EXECUTE ON transport.pkg_staff, EXECUTE ON transport.pkg_registration;
AUDIT POLICY pol_transport_account_admin;

EXIT
```

- [ ] **Step 4: Thêm view vào `05_views.sql`** (sau `v_trip_statistics`, trước khối kiểm tra `user_errors`)

```sql

-- Nhật ký đăng nhập (thành công/thất bại) và thao tác quản trị tài khoản, đọc từ Unified Auditing
CREATE OR REPLACE VIEW v_login_audit AS
SELECT CAST(event_timestamp AS TIMESTAMP)                     AS event_time,
       dbusername                                             AS username,
       action_name,
       CASE WHEN return_code = 0 THEN 'SUCCESS' ELSE 'FAILED' END AS result,
       return_code,
       userhost,
       object_name
  FROM sys.unified_audit_trail
 WHERE action_name = 'LOGON'
    OR (object_schema = 'TRANSPORT' AND object_name IN ('PKG_STAFF', 'PKG_REGISTRATION'));
```

- [ ] **Step 5: Cấp quyền trong `07_grants.sql`**

Thêm sau `GRANT EXECUTE ON pkg_registration TO role_operator;`:
```sql
GRANT SELECT ON v_login_audit          TO role_operator;
```

- [ ] **Step 6: Gỡ policy trong `00_uninstall.sql`**

Thêm ngay sau `SET SERVEROUTPUT ON SIZE UNLIMITED` (đầu file, trước khi xóa user):
```sql

-- Tắt rồi xóa audit policy trước khi xóa schema (policy tham chiếu package của TRANSPORT)
NOAUDIT POLICY pol_transport_logon;
NOAUDIT POLICY pol_transport_account_admin;
DROP AUDIT POLICY pol_transport_logon;
DROP AUDIT POLICY pol_transport_account_admin;
```

- [ ] **Step 7: Chạy `10_audit.sql` trong `install_all.bat`**

Thay khối `for ... ) ` + dòng trống sau nó bằng:
```bat
for %%f in (02_tables 03_triggers 04_packages 05_views 06_backup 07_grants 08_seed_data) do (
  echo === %%f ===
  sqlplus -S -L transport/"%TR_PWD%"@%DB% @%%f.sql || goto :fail
)

echo === 10_audit ===
sqlplus -S -L "sys/%SYS_PWD%@%DB% as sysdba" @10_audit.sql || goto :fail
```

- [ ] **Step 8: Nhờ người dùng cài lại, chạy 09 và 11, rồi kiểm tra view**

Ngoài các lệnh ở Task 1 Step 9, người dùng chạy thêm (bằng TRANSPORT):
```sql
SELECT username, result, COUNT(*) FROM v_login_audit GROUP BY username, result;
```
Expected: `ALL CHECKS PASSED` (có `PASS  5`), và view trả về ít nhất một dòng `TRANSPORT SUCCESS`.

- [ ] **Step 9: Commit**

```bash
node .gitnexus/run.cjs detect-changes --scope all --repo .
git add database/
git commit -m "feat(db): audit sign-ins and account administration with unified auditing"
```

---

### Task 4: Mã lỗi mới, luật mật khẩu C#, ánh xạ lỗi Oracle

**Files:**
- Modify: `src/Transport.Domain/Errors/BusinessErrorCode.cs`, `src/Transport.Application/Services/Rules.cs`, `src/Transport.Application/Services/StaffService.cs`, `src/Transport.Infrastructure/Persistence/OracleErrorMapper.cs`, `src/Transport.Presentation.Wpf/Resources/Strings.resx`, `src/Transport.Presentation.Wpf/Resources/Strings.vi.resx`
- Test: `tests/Transport.UnitTests/Application/StaffServiceTests.cs`, `tests/Transport.UnitTests/Infrastructure/OracleErrorMapperTests.cs`

**Interfaces:**
- Produces:
  - `BusinessErrorCode.RegistrationNotFound = 20060`, `TooManyPendingRequests = 20061`, `InvalidRegistrationData = 20062`, `PasswordExpired = 28001`, `PasswordReused = 28007`
  - `internal static void Rules.Password(string? password, string? username)` (thay chữ ký cũ một tham số)
  - `OracleErrorMapper.Map`: 28001 → `PasswordExpired`, 28003 → `InvalidPassword`, 28007 → `PasswordReused`

- [ ] **Step 1: Impact analysis**

Run: `node .gitnexus/run.cjs impact "Rules" --direction upstream --repo .` và `node .gitnexus/run.cjs impact "OracleErrorMapper" --direction upstream --repo .`
Expected: `Rules.Password` chỉ được `StaffService` gọi. Ghi lại mức rủi ro trong báo cáo.

- [ ] **Step 2: Viết test thất bại**

`StaffServiceTests.cs` – thêm dòng vào theory `CreateAsync_EnforcesSameRulesAsPkgStaff` (sau dòng `has\"quote1`):
```csharp
    [InlineData("seller03", "alllower#1", BusinessErrorCode.InvalidPassword)]
    [InlineData("seller03", "ALLUPPER#1", BusinessErrorCode.InvalidPassword)]
    [InlineData("seller03", "NoDigits#x", BusinessErrorCode.InvalidPassword)]
    [InlineData("seller03", "NoSpecial12", BusinessErrorCode.InvalidPassword)]
    [InlineData("seller03", "Seller03#x", BusinessErrorCode.InvalidPassword)]
```
và thêm test:
```csharp
    [Fact]
    public async Task ResetPassword_RejectsWeakPassword()
    {
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.ResetPasswordAsync("SELLER01", "weakpass"));

        Assert.Equal(BusinessErrorCode.InvalidPassword, ex.Code);
        Assert.Empty(_repository.Calls);
    }
```

`OracleErrorMapperTests.cs` – thêm `[InlineData(20060)] [InlineData(20061)] [InlineData(20062)]` vào `Map_ApplicationErrors_KeepTheirNumber`; thêm vào `Map_KnownSystemErrors`:
```csharp
    [InlineData(28001, BusinessErrorCode.PasswordExpired)]
    [InlineData(28003, BusinessErrorCode.InvalidPassword)]
    [InlineData(28007, BusinessErrorCode.PasswordReused)]
```

- [ ] **Step 3: Chạy test, xác nhận thất bại**

Run: `dotnet test --project tests/Transport.UnitTests`
Expected: lỗi biên dịch (`PasswordExpired` chưa có) hoặc FAIL ở các dòng mới.

- [ ] **Step 4: Thêm mã lỗi vào `BusinessErrorCode.cs`**

Sau `ScheduleHasActiveTickets = 20052,`:
```csharp
    RegistrationNotFound = 20060,
    TooManyPendingRequests = 20061,
    InvalidRegistrationData = 20062,
```
Sau `AccountLocked = 28000,`:
```csharp
    PasswordExpired = 28001,
    PasswordReused = 28007,
```

- [ ] **Step 5: Ánh xạ lỗi trong `OracleErrorMapper.SystemErrors`**

Sau `[28000] = BusinessErrorCode.AccountLocked,`:
```csharp
        [28001] = BusinessErrorCode.PasswordExpired,
        // Verify function của PROFILE_STAFF từ chối mật khẩu (bọc ORA-20044)
        [28003] = BusinessErrorCode.InvalidPassword,
        [28007] = BusinessErrorCode.PasswordReused,
```

- [ ] **Step 6: Luật mật khẩu trong `Rules.cs`**

Thay toàn bộ phương thức `Password` (kể cả comment phía trên):
```csharp
    // Cùng luật với SYS.STAFF_PASSWORD_VERIFY (PROFILE_STAFF) và dấu nháy kép bị PKG_STAFF chặn
    public static void Password(string? password, string? username)
    {
        if (password is null || password.Length < MinPasswordLength || password.Contains('"')
            || !password.Any(char.IsAsciiLetterUpper) || !password.Any(char.IsAsciiLetterLower)
            || !password.Any(char.IsAsciiDigit) || password.All(char.IsAsciiLetterOrDigit)
            || (!string.IsNullOrWhiteSpace(username)
                && password.Contains(username.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            throw new BusinessRuleException(BusinessErrorCode.InvalidPassword,
                "Password does not meet the complexity rules.");
        }
    }
```

- [ ] **Step 7: Cập nhật lời gọi trong `StaffService.cs`**

`CreateAsync`: `Rules.Password(request.Password);` → `Rules.Password(request.Password, request.Username);`
`ResetPasswordAsync`: `Rules.Password(password);` → `Rules.Password(password, username);`

- [ ] **Step 8: Chuỗi lỗi trong resx**

`Strings.resx` – đổi giá trị:
- `Err_20044` → `Password must be at least 8 characters with an uppercase letter, a lowercase letter, a digit and a special character, and must not contain the username or double quotes.`
- `Err_1017` → `Wrong username or password, or the account is locked.`
- `Dialog_Password_Label` → `New temporary password (the user must change it at next sign-in)`

Thêm trước `</root>`:
```xml
  <data name="Err_20060" xml:space="preserve"><value>The registration request does not exist or has already been reviewed.</value></data>
  <data name="Err_20061" xml:space="preserve"><value>There are too many pending registration requests. Please try again later.</value></data>
  <data name="Err_20062" xml:space="preserve"><value>Invalid registration data.</value></data>
  <data name="Err_28001" xml:space="preserve"><value>Your password has expired. Please set a new password.</value></data>
  <data name="Err_28007" xml:space="preserve"><value>You cannot reuse a recent password.</value></data>
```

`Strings.vi.resx` – đổi giá trị:
- `Err_20044` → `Mật khẩu tối thiểu 8 ký tự, gồm chữ hoa, chữ thường, chữ số và ký tự đặc biệt; không chứa tên đăng nhập hay dấu nháy kép.`
- `Err_1017` → `Sai tên đăng nhập hoặc mật khẩu, hoặc tài khoản đang bị khóa.`
- `Dialog_Password_Label` → `Mật khẩu tạm mới (người dùng phải đổi ở lần đăng nhập tới)`

Thêm trước `</root>`:
```xml
  <data name="Err_20060" xml:space="preserve"><value>Yêu cầu đăng ký không tồn tại hoặc đã được xử lý.</value></data>
  <data name="Err_20061" xml:space="preserve"><value>Đang có quá nhiều yêu cầu đăng ký chờ duyệt. Vui lòng thử lại sau.</value></data>
  <data name="Err_20062" xml:space="preserve"><value>Thông tin đăng ký không hợp lệ.</value></data>
  <data name="Err_28001" xml:space="preserve"><value>Mật khẩu đã hết hạn. Vui lòng đặt mật khẩu mới.</value></data>
  <data name="Err_28007" xml:space="preserve"><value>Không được dùng lại mật khẩu gần đây.</value></data>
```

- [ ] **Step 9: Chạy test**

Run: `dotnet test --project tests/Transport.UnitTests`
Expected: tất cả PASS (gồm `ResourceParityTests`).

- [ ] **Step 10: Commit**

```bash
node .gitnexus/run.cjs detect-changes --scope all --repo .
git add src tests
git commit -m "feat: password complexity rule and error codes for registration and password expiry"
```

---

### Task 5: Đăng nhập không lộ thông tin, đổi mật khẩu khi mở kết nối

**Files:**
- Modify: `src/Transport.Application/Abstractions/IAuthService.cs`, `src/Transport.Infrastructure/Persistence/OracleConnectionFactory.cs`, `src/Transport.Infrastructure/Persistence/OracleAuthService.cs`, `tests/Transport.UnitTests/Fakes/FakeAuthService.cs`, `tests/Transport.UnitTests/Infrastructure/OracleIntegrationTests.cs`
- Test: `tests/Transport.UnitTests/Infrastructure/OracleAuthServiceTests.cs` (mới)

**Interfaces:**
- Consumes: `BusinessErrorCode.PasswordExpired`, `OracleErrorMapper.Map` (Task 4).
- Produces:
  - `IAuthService.LoginWithNewPasswordAsync(string username, string oldPassword, string newPassword, CancellationToken ct = default)`
  - `IAuthService.ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken ct = default)`
  - `OracleConnectionFactory.OpenWithNewPasswordAsync(string username, string oldPassword, string newPassword, CancellationToken ct)`
  - `internal static BusinessRuleException? OracleAuthService.MapLoginError(int number, string message)`
  - `FakeAuthService`: `NewPasswordError`, `LastNewPasswordLogin`, `ChangeError`, `LastChange`

- [ ] **Step 1: Impact analysis**

Run: `node .gitnexus/run.cjs impact "IAuthService" --direction upstream --repo .`, `... impact "OracleConnectionFactory" ...`, `... impact "OracleAuthService" ...`
Expected: các người gọi là `LoginViewModel`, `MainViewModel`, `FakeAuthService`, repository dùng `OpenAsync(ct)` (không đổi chữ ký). Báo rủi ro.

- [ ] **Step 2: Viết test thất bại – `tests/Transport.UnitTests/Infrastructure/OracleAuthServiceTests.cs`**

```csharp
using Transport.Domain.Errors;
using Transport.Infrastructure.Persistence;

namespace Transport.UnitTests.Infrastructure;

public class OracleAuthServiceTests
{
    // Sai mật khẩu và bị khóa trả cùng một lỗi để không dò được tài khoản nào tồn tại/đang bị khóa
    [Theory]
    [InlineData(1017)]
    [InlineData(28000)]
    public void LoginErrors_DoNotRevealAccountState(int number)
    {
        var ex = OracleAuthService.MapLoginError(number, $"ORA-{number:D5}: original Oracle text");

        Assert.Equal(BusinessErrorCode.InvalidCredentials, ex!.Code);
        // Không mang theo thông điệp gốc của Oracle (VD "the account is locked")
        Assert.Equal("Invalid username or password, or the account is locked.", ex.Message);
    }

    [Theory]
    [InlineData(28001, BusinessErrorCode.PasswordExpired)]
    [InlineData(28003, BusinessErrorCode.InvalidPassword)]
    [InlineData(28007, BusinessErrorCode.PasswordReused)]
    [InlineData(12541, BusinessErrorCode.DatabaseUnavailable)]
    public void OtherLoginErrors_UseTheCommonMapping(int number, BusinessErrorCode expected)
    {
        Assert.Equal(expected, OracleAuthService.MapLoginError(number, "ORA-00000: x")!.Code);
    }

    [Fact]
    public void UnknownErrors_AreNotMapped()
    {
        Assert.Null(OracleAuthService.MapLoginError(942, "ORA-00942: table or view does not exist"));
    }
}
```

- [ ] **Step 3: Chạy test, xác nhận thất bại**

Run: `dotnet test --project tests/Transport.UnitTests --filter-class "*OracleAuthServiceTests"`
Expected: lỗi biên dịch `MapLoginError` không tồn tại.

- [ ] **Step 4: Mở rộng `IAuthService`**

```csharp
namespace Transport.Application.Abstractions;

public interface IAuthService
{
    // Đăng nhập bằng user Oracle; ném BusinessRuleException khi sai mật khẩu/bị khóa/không có quyền
    // (cùng mã InvalidCredentials) hoặc PasswordExpired khi phải đổi mật khẩu
    Task LoginAsync(string username, string password, CancellationToken ct = default);

    // Đăng nhập đồng thời đổi mật khẩu (dùng khi LoginAsync báo PasswordExpired)
    Task LoginWithNewPasswordAsync(string username, string oldPassword, string newPassword, CancellationToken ct = default);

    // Người đang đăng nhập tự đổi mật khẩu
    Task ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken ct = default);

    void Logout();
}
```

- [ ] **Step 5: Viết lại `OracleConnectionFactory.cs`**

```csharp
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;

namespace Transport.Infrastructure.Persistence;

// Mở kết nối (lấy từ pool) bằng tài khoản Oracle của nhân viên đang đăng nhập
internal sealed partial class OracleConnectionFactory(IOptions<DatabaseOptions> options, UserSession session)
{
    public Task<OracleConnection> OpenAsync(CancellationToken ct) =>
        session.IsAuthenticated
            ? OpenAsync(session.Username, session.Password, ct)
            : throw new InvalidOperationException("No user is signed in.");

    public Task<OracleConnection> OpenAsync(string username, string password, CancellationToken ct) =>
        OpenCoreAsync(Create(username, password, pooling: true), c => c.OpenAsync(ct), ct);

    // Đổi mật khẩu ngay lúc mở kết nối (dùng được cả khi mật khẩu đã hết hạn - ORA-28001).
    // Không qua pool: pool không mở được phiên bằng mật khẩu đã hết hạn.
    public Task<OracleConnection> OpenWithNewPasswordAsync(string username, string oldPassword, string newPassword,
        CancellationToken ct) =>
        OpenCoreAsync(Create(username, oldPassword, pooling: false),
            c => Task.Run(() => c.OpenWithNewPassword(newPassword), ct), ct);

    private OracleConnection Create(string username, string password, bool pooling) =>
        new(new OracleConnectionStringBuilder
        {
            DataSource = options.Value.DataSource,
            UserID = username,
            Password = password,
            Pooling = pooling,
        }.ConnectionString);

    private async Task<OracleConnection> OpenCoreAsync(OracleConnection connection, Func<OracleConnection, Task> open,
        CancellationToken ct)
    {
        try
        {
            var schema = options.Value.Schema;
            if (!SchemaPattern().IsMatch(schema))
            {
                throw new InvalidOperationException($"Invalid schema name '{schema}'.");
            }

            await open(connection);
            await using var command = connection.CreateCommand();
            // Nhờ vậy câu SQL trong app không cần tiền tố "transport."
            command.CommandText = $"ALTER SESSION SET CURRENT_SCHEMA = {schema}";
            await command.ExecuteNonQueryAsync(ct);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    [GeneratedRegex("^[A-Za-z][A-Za-z0-9_]{0,29}$")]
    private static partial Regex SchemaPattern();
}
```

- [ ] **Step 6: Viết lại `OracleAuthService.cs`**

```csharp
using Dapper;
using Oracle.ManagedDataAccess.Client;
using Transport.Application.Abstractions;
using Transport.Domain.Errors;

namespace Transport.Infrastructure.Persistence;

// Đăng nhập = mở kết nối Oracle bằng tài khoản nhân viên, đọc role đang bật và họ tên trong STAFF
internal sealed class OracleAuthService(OracleConnectionFactory factory, UserSession session) : IAuthService
{
    private const string OperatorRole = "ROLE_OPERATOR";
    private const string SellerRole = "ROLE_TICKET_SELLER";

    public Task LoginAsync(string username, string password, CancellationToken ct = default)
    {
        var user = Normalize(username);
        return SignInAsync(user, password, () => factory.OpenAsync(user, password, ct), ct);
    }

    public Task LoginWithNewPasswordAsync(string username, string oldPassword, string newPassword, CancellationToken ct = default)
    {
        var user = Normalize(username);
        return SignInAsync(user, newPassword, () => factory.OpenWithNewPasswordAsync(user, oldPassword, newPassword, ct), ct);
    }

    public async Task ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken ct = default)
    {
        if (!session.IsAuthenticated)
        {
            throw new InvalidOperationException("No user is signed in.");
        }

        await LoginWithNewPasswordAsync(session.Username, currentPassword, newPassword, ct);
        // Kết nối trong pool vẫn mang mật khẩu cũ
        OracleConnection.ClearAllPools();
    }

    public void Logout()
    {
        session.Clear();
        // Bỏ các kết nối trong pool mang thông tin đăng nhập cũ
        OracleConnection.ClearAllPools();
    }

    // Sai mật khẩu, bị khóa, không phải nhân viên: cùng một lỗi để không lộ tài khoản nào tồn tại hay đang bị khóa
    internal static BusinessRuleException? MapLoginError(int number, string message) =>
        number is 1017 or 28000 ? InvalidCredentials() : OracleErrorMapper.Map(number, message);

    private async Task SignInAsync(string user, string password, Func<Task<OracleConnection>> open, CancellationToken ct)
    {
        try
        {
            await using var connection = await open();
            var roles = (await connection.QueryAsync<string>(
                    new CommandDefinition("SELECT role FROM session_roles", cancellationToken: ct)))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var fullName = await connection.ExecuteScalarAsync<string?>(new CommandDefinition(
                "SELECT full_name FROM staff WHERE username = :p_username", new { p_username = user }, cancellationToken: ct));

            // Chỉ nhân viên có trong STAFF và có một trong hai role mới vào được app (chặn cả TRANSPORT, REGISTRAR)
            var isOperator = roles.Contains(OperatorRole);
            if (fullName is null || (!isOperator && !roles.Contains(SellerRole)))
            {
                throw InvalidCredentials();
            }

            session.Start(user, password, fullName, isOperator);
        }
        catch (OracleException ex) when (MapLoginError(ex.Number, ex.Message) is { } mapped)
        {
            throw mapped;
        }
    }

    private static BusinessRuleException InvalidCredentials() =>
        new(BusinessErrorCode.InvalidCredentials, "Invalid username or password, or the account is locked.");

    private static string Normalize(string username) => username.Trim().ToUpperInvariant();
}
```

- [ ] **Step 7: Cập nhật `FakeAuthService.cs`**

```csharp
using Transport.Application.Abstractions;
using Transport.Domain.Errors;

namespace Transport.UnitTests.Fakes;

internal sealed class FakeAuthService : IAuthService
{
    public BusinessRuleException? LoginError { get; set; }
    public BusinessRuleException? NewPasswordError { get; set; }
    public BusinessRuleException? ChangeError { get; set; }
    public (string Username, string Password)? LastLogin { get; private set; }
    public (string Username, string OldPassword, string NewPassword)? LastNewPasswordLogin { get; private set; }
    public (string CurrentPassword, string NewPassword)? LastChange { get; private set; }
    public int LogoutCount { get; private set; }

    public Task LoginAsync(string username, string password, CancellationToken ct = default)
    {
        LastLogin = (username, password);
        return LoginError is null ? Task.CompletedTask : Task.FromException(LoginError);
    }

    public Task LoginWithNewPasswordAsync(string username, string oldPassword, string newPassword, CancellationToken ct = default)
    {
        LastNewPasswordLogin = (username, oldPassword, newPassword);
        return NewPasswordError is null ? Task.CompletedTask : Task.FromException(NewPasswordError);
    }

    public Task ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken ct = default)
    {
        LastChange = (currentPassword, newPassword);
        return ChangeError is null ? Task.CompletedTask : Task.FromException(ChangeError);
    }

    public void Logout() => LogoutCount++;
}
```

- [ ] **Step 8: Integration test – mật khẩu từ biến môi trường + luồng mật khẩu hết hạn**

Trong `OracleIntegrationTests.cs`, thêm sau `RequireDatabase()`:
```csharp
    // Tài khoản demo bị bắt đổi mật khẩu ở lần đăng nhập đầu: đổi trong app rồi đặt hai biến môi trường này
    private static string OperatorPassword =>
        Environment.GetEnvironmentVariable("TRANSPORT_DB_OPERATOR_PASSWORD") ?? "Operator#01";

    private static string SellerPassword =>
        Environment.GetEnvironmentVariable("TRANSPORT_DB_SELLER_PASSWORD") ?? "Seller#001";
```
Thay mọi `"Operator#01"` bằng `OperatorPassword` và `"Seller#001"` bằng `SellerPassword` (5 chỗ).
Thêm `using Transport.Application.Services;` nếu chưa có, và thêm test:
```csharp
    [Fact]
    public async Task NewStaff_MustReplaceTemporaryPasswordAtFirstSignIn()
    {
        RequireDatabase();
        // User mới mỗi lần chạy; app không xóa được user nên cuối test khóa lại
        var username = $"it_{DateTime.Now:MMddHHmmss}";
        using (var admin = await SignInAsync("operator01", OperatorPassword))
        {
            await admin.GetRequiredService<StaffService>().CreateAsync(
                new NewStaffRequest(username, "TempPass#1", "Integration Test", null, StaffRole.TicketSeller));
        }

        using var services = CreateServices();
        var auth = services.GetRequiredService<IAuthService>();
        var expired = await Assert.ThrowsAsync<BusinessRuleException>(() => auth.LoginAsync(username, "TempPass#1"));
        Assert.Equal(BusinessErrorCode.PasswordExpired, expired.Code);

        await auth.LoginWithNewPasswordAsync(username, "TempPass#1", "NewPass#2");
        Assert.Equal(username.ToUpperInvariant(), services.GetRequiredService<IUserSession>().Username);

        await auth.ChangePasswordAsync("NewPass#2", "NewPass#3");
        auth.Logout();
        await auth.LoginAsync(username, "NewPass#3");

        using var cleanup = await SignInAsync("operator01", OperatorPassword);
        await cleanup.GetRequiredService<StaffService>().SetLockedAsync(username, true);
    }
```

- [ ] **Step 9: Chạy test**

Run: `dotnet build TransportManagement.slnx` rồi `dotnet test --project tests/Transport.UnitTests`
Expected: build 0 warning; tất cả PASS (integration test bị skip nếu chưa đặt `TRANSPORT_DB_TESTS=1`).

Nếu người dùng đã cài DB (Task 1–3) và đổi mật khẩu demo: nhờ họ chạy
```
set TRANSPORT_DB_TESTS=1
set TRANSPORT_DB_OPERATOR_PASSWORD=<mật khẩu mới operator01>
set TRANSPORT_DB_SELLER_PASSWORD=<mật khẩu mới seller01>
dotnet test --project tests/Transport.UnitTests --filter-class "*OracleIntegrationTests"
```
Expected: PASS, kể cả `NewStaff_MustReplaceTemporaryPasswordAtFirstSignIn`. **Nếu `OpenWithNewPassword` không đổi được mật khẩu chưa hết hạn** (test hỏng ở `ChangePasswordAsync`): báo lại người dùng trước khi đổi cách làm, không tự ý chuyển sang ghép câu `ALTER USER`.

- [ ] **Step 10: Commit**

```bash
node .gitnexus/run.cjs detect-changes --scope all --repo .
git add src tests
git commit -m "feat(auth): generic sign-in errors and password change on connect"
```

---

### Task 6: Dịch vụ đăng ký tài khoản (Application + Infrastructure)

**Files:**
- Create: `src/Transport.Domain/Enums/RegistrationStatus.cs`, `src/Transport.Application/Models/RegistrationRequest.cs`, `src/Transport.Application/Abstractions/IRegistrationRepository.cs`, `src/Transport.Application/Services/RegistrationService.cs`, `src/Transport.Infrastructure/Repositories/RegistrationRepository.cs`, `tests/Transport.UnitTests/Fakes/FakeRegistrationRepository.cs`, `tests/Transport.UnitTests/Application/RegistrationServiceTests.cs`
- Modify: `src/Transport.Infrastructure/Persistence/DatabaseOptions.cs`, `src/Transport.Infrastructure/Persistence/OracleConnectionFactory.cs`, `src/Transport.Infrastructure/Persistence/OracleRepository.cs`, `src/Transport.Application/DependencyInjection.cs`, `src/Transport.Infrastructure/DependencyInjection.cs`, `src/Transport.Presentation.Wpf/appsettings.json`, `tests/Transport.UnitTests/Infrastructure/OracleIntegrationTests.cs`

**Interfaces:**
- Consumes: `Rules.Password(password, username)` (Task 4); packages Task 2.
- Produces:
  - `enum RegistrationStatus { Pending, Approved, Rejected }`
  - `record RegistrationRequest { int RequestId; string Username; string FullName; string? Phone; RegistrationStatus Status; string? ClientHost; DateTime RequestedAt; string? ReviewedBy; DateTime? ReviewedAt; string? RejectReason }`
  - `IRegistrationRepository`: `SubmitAsync(string username, string fullName, string? phone, CancellationToken ct = default)`, `GetAllAsync(CancellationToken ct = default)`, `ApproveAsync(int requestId, string tempPassword, StaffRole role, CancellationToken ct = default)`, `RejectAsync(int requestId, string reason, CancellationToken ct = default)`
  - `RegistrationService`: `SubmitAsync(string username, string fullName, string? phone, CancellationToken ct = default)`, `GetAllAsync(...)`, `ApproveAsync(RegistrationRequest request, string tempPassword, StaffRole role, CancellationToken ct = default)`, `RejectAsync(int requestId, string reason, CancellationToken ct = default)`
  - `FakeRegistrationRepository` (`Requests`, `Calls`)

- [ ] **Step 1: Impact analysis**

Run: `node .gitnexus/run.cjs impact "OracleRepository" --direction upstream --repo .` và `... impact "DatabaseOptions" ...`
Expected: mọi repository kế thừa `OracleRepository`; chữ ký `RunAsync(work, ct)`, `CallAsync` giữ nguyên.

- [ ] **Step 2: Tạo enum, model, giao diện (để test biên dịch được)**

`src/Transport.Domain/Enums/RegistrationStatus.cs`:
```csharp
namespace Transport.Domain.Enums;

// Trạng thái REGISTRATION_REQUESTS.STATUS
public enum RegistrationStatus
{
    Pending,
    Approved,
    Rejected,
}
```

`src/Transport.Application/Models/RegistrationRequest.cs`:
```csharp
using Transport.Domain.Enums;

namespace Transport.Application.Models;

// Một dòng REGISTRATION_REQUESTS (không có mật khẩu)
public sealed record RegistrationRequest
{
    public int RequestId { get; init; }
    public required string Username { get; init; }
    public required string FullName { get; init; }
    public string? Phone { get; init; }
    public RegistrationStatus Status { get; init; }
    public string? ClientHost { get; init; }
    public DateTime RequestedAt { get; init; }
    public string? ReviewedBy { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public string? RejectReason { get; init; }
}
```

`src/Transport.Application/Abstractions/IRegistrationRepository.cs`:
```csharp
using Transport.Application.Models;
using Transport.Domain.Enums;

namespace Transport.Application.Abstractions;

// Gọi PKG_REGISTRATION_PUBLIC (chưa đăng nhập) và PKG_REGISTRATION (điều hành)
public interface IRegistrationRepository
{
    Task SubmitAsync(string username, string fullName, string? phone, CancellationToken ct = default);
    Task<IReadOnlyList<RegistrationRequest>> GetAllAsync(CancellationToken ct = default);
    Task ApproveAsync(int requestId, string tempPassword, StaffRole role, CancellationToken ct = default);
    Task RejectAsync(int requestId, string reason, CancellationToken ct = default);
}
```

`tests/Transport.UnitTests/Fakes/FakeRegistrationRepository.cs`:
```csharp
using Transport.Application.Abstractions;
using Transport.Application.Models;
using Transport.Domain.Enums;

namespace Transport.UnitTests.Fakes;

internal sealed class FakeRegistrationRepository : IRegistrationRepository
{
    public List<RegistrationRequest> Requests { get; } = [];
    public List<string> Calls { get; } = [];

    public Task SubmitAsync(string username, string fullName, string? phone, CancellationToken ct = default) { Calls.Add($"submit:{username}:{fullName}:{phone}"); return Task.CompletedTask; }
    public Task<IReadOnlyList<RegistrationRequest>> GetAllAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<RegistrationRequest>>(Requests.ToList());
    public Task ApproveAsync(int requestId, string tempPassword, StaffRole role, CancellationToken ct = default) { Calls.Add($"approve:{requestId}:{role}"); return Task.CompletedTask; }
    public Task RejectAsync(int requestId, string reason, CancellationToken ct = default) { Calls.Add($"reject:{requestId}:{reason}"); return Task.CompletedTask; }
}
```

- [ ] **Step 3: Viết test thất bại – `tests/Transport.UnitTests/Application/RegistrationServiceTests.cs`**

```csharp
using Transport.Application.Models;
using Transport.Application.Services;
using Transport.Domain.Enums;
using Transport.Domain.Errors;
using Transport.UnitTests.Fakes;

namespace Transport.UnitTests.Application;

public class RegistrationServiceTests
{
    private readonly FakeRegistrationRepository _repository = new();

    private static readonly FakeUserSession Anonymous = new() { Username = string.Empty, FullName = string.Empty, IsOperator = false, IsAuthenticated = false };

    private static readonly RegistrationRequest Pending = new()
    {
        RequestId = 7,
        Username = "NEWUSER01",
        FullName = "Người Mới",
        Status = RegistrationStatus.Pending,
        RequestedAt = new DateTime(2026, 10, 18, 9, 0, 0),
    };

    private RegistrationService Create(FakeUserSession session) => new(_repository, session);

    [Fact]
    public async Task Submit_WorksWithoutSignInAndTrimsInput()
    {
        await Create(Anonymous).SubmitAsync(" newuser01 ", " Người Mới ", " ");

        Assert.Equal(new[] { "submit:newuser01:Người Mới:" }, _repository.Calls);
    }

    [Theory]
    [InlineData("ab", "Tên", null, BusinessErrorCode.InvalidUsername)]
    [InlineData("x; drop", "Tên", null, BusinessErrorCode.InvalidUsername)]
    [InlineData("newuser01", " ", null, BusinessErrorCode.RequiredField)]
    [InlineData("newuser01", "Tên", "123", BusinessErrorCode.InvalidPhone)]
    public async Task Submit_ValidatesBeforeCallingOracle(string username, string fullName, string? phone, BusinessErrorCode expected)
    {
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => Create(Anonymous).SubmitAsync(username, fullName, phone));

        Assert.Equal(expected, ex.Code);
        Assert.Empty(_repository.Calls);
    }

    [Fact]
    public async Task Approve_RequiresAStrongTemporaryPassword()
    {
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => Create(new FakeUserSession()).ApproveAsync(Pending, "weakpass", StaffRole.TicketSeller));

        Assert.Equal(BusinessErrorCode.InvalidPassword, ex.Code);
        Assert.Empty(_repository.Calls);
    }

    [Fact]
    public async Task Approve_And_Reject_ReachRepository()
    {
        var service = Create(new FakeUserSession());

        await service.ApproveAsync(Pending, "TempPass#1", StaffRole.TicketSeller);
        await service.RejectAsync(7, " Trùng người ");

        Assert.Equal(new[] { "approve:7:TicketSeller", "reject:7:Trùng người" }, _repository.Calls);
    }

    [Fact]
    public async Task Reject_RequiresReason()
    {
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => Create(new FakeUserSession()).RejectAsync(7, " "));

        Assert.Equal(BusinessErrorCode.RequiredField, ex.Code);
    }

    [Fact]
    public async Task Sellers_CannotReviewRequests()
    {
        var service = Create(FakeUserSession.Seller());

        Assert.Equal(BusinessErrorCode.OperatorOnly, (await Assert.ThrowsAsync<BusinessRuleException>(() => service.GetAllAsync())).Code);
        Assert.Equal(BusinessErrorCode.OperatorOnly, (await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.ApproveAsync(Pending, "TempPass#1", StaffRole.TicketSeller))).Code);
        Assert.Equal(BusinessErrorCode.OperatorOnly, (await Assert.ThrowsAsync<BusinessRuleException>(() => service.RejectAsync(7, "x"))).Code);
    }
}
```

- [ ] **Step 4: Chạy test, xác nhận thất bại**

Run: `dotnet test --project tests/Transport.UnitTests --filter-class "*RegistrationServiceTests"`
Expected: lỗi biên dịch `RegistrationService` không tồn tại.

- [ ] **Step 5: Tạo `src/Transport.Application/Services/RegistrationService.cs`**

```csharp
using Transport.Application.Abstractions;
using Transport.Application.Models;
using Transport.Domain.Enums;

namespace Transport.Application.Services;

// Đăng ký tài khoản: người chưa có tài khoản gửi yêu cầu, điều hành duyệt (cấp mật khẩu tạm) hoặc từ chối
public sealed class RegistrationService(IRegistrationRepository requests, IUserSession session)
{
    // Không cần đăng nhập; repository gọi bằng user TRANSPORT_REGISTRAR
    public Task SubmitAsync(string username, string fullName, string? phone, CancellationToken ct = default)
    {
        var user = username.Trim();
        Rules.Username(user);
        Rules.Required(fullName);
        Rules.OptionalPhone(phone);
        return requests.SubmitAsync(user, fullName.Trim(), Rules.TrimToNull(phone), ct);
    }

    public async Task<IReadOnlyList<RegistrationRequest>> GetAllAsync(CancellationToken ct = default)
    {
        Rules.Operator(session);
        return await requests.GetAllAsync(ct);
    }

    // Mật khẩu tạm phải đạt luật PROFILE_STAFF; nhân viên mới phải đổi ở lần đăng nhập đầu
    public async Task ApproveAsync(RegistrationRequest request, string tempPassword, StaffRole role, CancellationToken ct = default)
    {
        Rules.Operator(session);
        Rules.Password(tempPassword, request.Username);
        await requests.ApproveAsync(request.RequestId, tempPassword, role, ct);
    }

    public async Task RejectAsync(int requestId, string reason, CancellationToken ct = default)
    {
        Rules.Operator(session);
        Rules.Required(reason);
        await requests.RejectAsync(requestId, reason.Trim(), ct);
    }
}
```

Đăng ký trong `src/Transport.Application/DependencyInjection.cs`, sau `services.AddSingleton<StaffService>();`:
```csharp
        services.AddSingleton<RegistrationService>();
```

- [ ] **Step 6: Chạy test**

Run: `dotnet test --project tests/Transport.UnitTests --filter-class "*RegistrationServiceTests"`
Expected: PASS.

- [ ] **Step 7: Cấu hình user REGISTRAR**

`DatabaseOptions.cs`, thêm sau `Schema`:
```csharp

    // User chỉ được gửi yêu cầu đăng ký; mật khẩu công khai có chủ đích (spec auth-security §2.2)
    public string RegistrarUser { get; set; } = "TRANSPORT_REGISTRAR";

    public string RegistrarPassword { get; set; } = "Register#Only1";
```

`appsettings.json`:
```json
{
  "Database": {
    "DataSource": "localhost:1521/XEPDB1",
    "Schema": "TRANSPORT",
    "RegistrarUser": "TRANSPORT_REGISTRAR",
    "RegistrarPassword": "Register#Only1"
  },
  "Language": "vi"
}
```

`OracleConnectionFactory.cs`, thêm sau `OpenAsync(string username, ...)`:
```csharp

    // Người chưa có tài khoản gửi yêu cầu đăng ký bằng user chỉ có quyền gọi submit_request
    public Task<OracleConnection> OpenRegistrarAsync(CancellationToken ct) =>
        OpenAsync(options.Value.RegistrarUser, options.Value.RegistrarPassword, ct);
```

- [ ] **Step 8: Cho `OracleRepository` mở kết nối REGISTRAR**

Thay `CallAsync` và `RunAsync` trong `OracleRepository.cs` bằng:
```csharp
    // Gọi procedure/package, VD "pkg_ticket.pay_ticket"
    protected Task CallAsync(string procedure, object? args, CancellationToken ct) =>
        RunAsync(c => c.ExecuteAsync(Procedure(procedure, args, ct)), ct);

    // Gọi procedure khi chưa đăng nhập, bằng user TRANSPORT_REGISTRAR
    protected Task CallAsRegistrarAsync(string procedure, object? args, CancellationToken ct) =>
        RunAsync(factory.OpenRegistrarAsync, c => c.ExecuteAsync(Procedure(procedure, args, ct)), ct);

    protected Task<T> RunAsync<T>(Func<OracleConnection, Task<T>> work, CancellationToken ct) =>
        RunAsync(factory.OpenAsync, work, ct);

    private static async Task<T> RunAsync<T>(Func<CancellationToken, Task<OracleConnection>> open,
        Func<OracleConnection, Task<T>> work, CancellationToken ct)
    {
        try
        {
            await using var connection = await open(ct);
            return await work(connection);
        }
        catch (OracleException ex) when (OracleErrorMapper.Map(ex.Number, ex.Message) is { } mapped)
        {
            throw mapped;
        }
    }

    private static CommandDefinition Procedure(string procedure, object? args, CancellationToken ct) =>
        new(procedure, args, commandType: CommandType.StoredProcedure, cancellationToken: ct);
```

- [ ] **Step 9: Tạo `src/Transport.Infrastructure/Repositories/RegistrationRepository.cs`**

```csharp
using Transport.Application.Abstractions;
using Transport.Application.Models;
using Transport.Domain.Enums;
using Transport.Infrastructure.Persistence;

namespace Transport.Infrastructure.Repositories;

// Gửi yêu cầu bằng user REGISTRAR; duyệt/từ chối bằng tài khoản điều hành qua PKG_REGISTRATION
internal sealed class RegistrationRepository(OracleConnectionFactory factory) : OracleRepository(factory), IRegistrationRepository
{
    public Task SubmitAsync(string username, string fullName, string? phone, CancellationToken ct = default) =>
        CallAsRegistrarAsync("pkg_registration_public.submit_request",
            new { p_username = username, p_full_name = fullName, p_phone = phone }, ct);

    public Task<IReadOnlyList<RegistrationRequest>> GetAllAsync(CancellationToken ct = default) => QueryAsync<RegistrationRequest>(
        """
        SELECT request_id, username, full_name, phone, status, client_host,
               requested_at, reviewed_by, reviewed_at, reject_reason
          FROM registration_requests
         ORDER BY request_id DESC
        """, null, ct);

    public Task ApproveAsync(int requestId, string tempPassword, StaffRole role, CancellationToken ct = default) =>
        CallAsync("pkg_registration.approve_request",
            new { p_request_id = requestId, p_temp_password = tempPassword, p_role = DbEnum.RoleToDb(role) }, ct);

    public Task RejectAsync(int requestId, string reason, CancellationToken ct = default) =>
        CallAsync("pkg_registration.reject_request", new { p_request_id = requestId, p_reason = reason }, ct);
}
```

Đăng ký trong `src/Transport.Infrastructure/DependencyInjection.cs`, sau `IStaffRepository`:
```csharp
        services.AddSingleton<IRegistrationRepository, RegistrationRepository>();
```

- [ ] **Step 10: Integration test đăng ký**

Thêm vào `OracleIntegrationTests.cs`:
```csharp
    [Fact]
    public async Task Registration_IsSubmittedWithoutSignInAndReviewedByOperator()
    {
        RequireDatabase();
        var username = $"rq_{DateTime.Now:MMddHHmmss}";
        using (var anonymous = CreateServices())
        {
            await anonymous.GetRequiredService<RegistrationService>().SubmitAsync(username, "Integration Test", null);
        }

        using var admin = await SignInAsync("operator01", OperatorPassword);
        var registrations = admin.GetRequiredService<RegistrationService>();
        var request = (await registrations.GetAllAsync()).Single(r => r.Username == username.ToUpperInvariant());
        Assert.Equal(RegistrationStatus.Pending, request.Status);

        await registrations.RejectAsync(request.RequestId, "Integration test");
    }
```

- [ ] **Step 11: Chạy toàn bộ test**

Run: `dotnet build TransportManagement.slnx` rồi `dotnet test --project tests/Transport.UnitTests`
Expected: 0 warning, tất cả PASS (`DependencyInjectionTests` vẫn 11 trang).

- [ ] **Step 12: Commit**

```bash
node .gitnexus/run.cjs detect-changes --scope all --repo .
git add src tests
git commit -m "feat: registration service with least-privilege registrar connection"
```

---

### Task 7: Ba hộp thoại – Đăng ký, Đổi mật khẩu, Duyệt yêu cầu

**Files:**
- Create: `src/Transport.Presentation.Wpf/ViewModels/Dialogs/RegisterViewModel.cs`, `ChangePasswordViewModel.cs`, `ApproveRegistrationViewModel.cs`; `src/Transport.Presentation.Wpf/Views/Dialogs/RegisterDialog.xaml(.cs)`, `ChangePasswordDialog.xaml(.cs)`, `ApproveRegistrationDialog.xaml(.cs)`
- Modify: `src/Transport.Presentation.Wpf/Services/IDialogService.cs`, `DialogService.cs`, `Resources/Strings*.resx`, `tests/Transport.UnitTests/Fakes/FakeDialogService.cs`, `tests/Transport.UnitTests/Presentation/DialogViewModelTests.cs`, `tests/Transport.UnitTests/Presentation/ViewInstantiationTests.cs`

**Interfaces:**
- Consumes: `RegistrationService.SubmitAsync` (Task 6).
- Produces:
  - `record PasswordChange(string CurrentPassword, string NewPassword)`; `record ApprovalInput(string TempPassword, StaffRole Role)` (namespace `Transport.Presentation.Wpf.ViewModels.Dialogs`)
  - `IDialogService.ShowRegistration(): bool`, `PromptPasswordChange(bool requireCurrent): PasswordChange?`, `PromptApproval(string title): ApprovalInput?`
  - `FakeDialogService`: `RegistrationResult`, `RegistrationCount`, `PasswordChangeResult`, `PasswordChangeRequests`, `ApprovalResult`

- [ ] **Step 1: Impact analysis**

Run: `node .gitnexus/run.cjs impact "IDialogService" --direction upstream --repo .` và `... impact "DialogService" ...`
Expected: mọi page ViewModel dùng qua `PageServices`; chỉ thêm phương thức, không đổi phương thức cũ. `DialogService` có thêm tham số constructor (DI tự giải quyết).

- [ ] **Step 2: Viết test thất bại – thêm vào `DialogViewModelTests.cs`**

Thêm `using Transport.Application.Services;`, `using Transport.Domain.Errors;`, `using Transport.UnitTests.Fakes;` và:
```csharp
    [Fact]
    public void ChangePassword_OkNeedsCurrentAndMatchingNewPasswords()
    {
        var dialog = new ChangePasswordViewModel("Đổi mật khẩu", requireCurrent: true);

        dialog.NewPassword = "NewPass#1";
        dialog.ConfirmPassword = "NewPass#2";
        Assert.True(dialog.IsMismatch);
        Assert.False(dialog.OkCommand.CanExecute(null));

        dialog.ConfirmPassword = "NewPass#1";
        Assert.False(dialog.IsMismatch);
        Assert.False(dialog.OkCommand.CanExecute(null));

        dialog.CurrentPassword = "Old#Pass1";
        Assert.True(dialog.OkCommand.CanExecute(null));
        Assert.Equal(new PasswordChange("Old#Pass1", "NewPass#1"), dialog.Result);
    }

    [Fact]
    public void ChangePassword_ForcedChange_DoesNotAskForCurrentPassword()
    {
        var dialog = new ChangePasswordViewModel("Đặt mật khẩu mới", requireCurrent: false)
        {
            NewPassword = "NewPass#1",
            ConfirmPassword = "NewPass#1",
        };

        Assert.True(dialog.OkCommand.CanExecute(null));
    }

    [Fact]
    public void ApproveRegistration_DefaultsToTicketSellerAndNeedsPassword()
    {
        var dialog = new ApproveRegistrationViewModel("Duyệt");

        Assert.Equal(StaffRole.TicketSeller, dialog.Role);
        Assert.False(dialog.OkCommand.CanExecute(null));
        dialog.TempPassword = "TempPass#1";
        Assert.True(dialog.OkCommand.CanExecute(null));
        Assert.Equal(new ApprovalInput("TempPass#1", StaffRole.TicketSeller), dialog.Result);
    }

    [Fact]
    public async Task Register_SubmitsAndCloses()
    {
        var repository = new FakeRegistrationRepository();
        var dialog = new RegisterViewModel(new RegistrationService(repository, new FakeUserSession()), new FakeLocalizationService());
        bool? result = null;
        dialog.CloseRequested += r => result = r;

        Assert.False(dialog.SubmitCommand.CanExecute(null));
        dialog.Username = "newuser01";
        dialog.FullName = "Người Mới";
        await dialog.SubmitCommand.ExecuteAsync(null);

        Assert.True(result);
        Assert.Equal(new[] { "submit:newuser01:Người Mới:" }, repository.Calls);
    }

    [Fact]
    public async Task Register_ShowsTranslatedErrorAndStaysOpen()
    {
        var dialog = new RegisterViewModel(new RegistrationService(new FakeRegistrationRepository(), new FakeUserSession()),
            new FakeLocalizationService())
        {
            Username = "ab",
            FullName = "Người Mới",
        };
        bool? result = null;
        dialog.CloseRequested += r => result = r;

        await dialog.SubmitCommand.ExecuteAsync(null);

        Assert.Null(result);
        Assert.Equal($"Err_{(int)BusinessErrorCode.InvalidUsername}", dialog.ErrorMessage);
    }
```

Trong `ViewInstantiationTests.cs` thêm `using Transport.Presentation.Wpf.Views.Dialogs;` và sau `_ = new BackupPage();`:
```csharp
                _ = new RegisterDialog();
                _ = new ChangePasswordDialog();
                _ = new ApproveRegistrationDialog();
```

- [ ] **Step 3: Chạy test, xác nhận thất bại**

Run: `dotnet test --project tests/Transport.UnitTests --filter-class "*DialogViewModelTests"`
Expected: lỗi biên dịch (các ViewModel chưa có).

- [ ] **Step 4: `ChangePasswordViewModel.cs`**

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Transport.Presentation.Wpf.ViewModels.Dialogs;

// Kết quả hộp thoại đổi mật khẩu; CurrentPassword rỗng khi bị bắt đổi lúc đăng nhập
public sealed record PasswordChange(string CurrentPassword, string NewPassword);

// Tự đổi mật khẩu (cần mật khẩu hiện tại) hoặc bắt buộc đặt mật khẩu mới khi mật khẩu hết hạn
public sealed partial class ChangePasswordViewModel : ObservableObject, IDialogViewModel
{
    public ChangePasswordViewModel(string title, bool requireCurrent)
    {
        Title = title;
        RequireCurrent = requireCurrent;
        CurrentPassword = string.Empty;
        NewPassword = string.Empty;
        ConfirmPassword = string.Empty;
    }

    public event Action<bool>? CloseRequested;

    public string Title { get; }

    public bool RequireCurrent { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OkCommand))]
    public partial string CurrentPassword { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OkCommand))]
    [NotifyPropertyChangedFor(nameof(IsMismatch))]
    public partial string NewPassword { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OkCommand))]
    [NotifyPropertyChangedFor(nameof(IsMismatch))]
    public partial string ConfirmPassword { get; set; }

    // Đã nhập lại nhưng chưa khớp: hiện cảnh báo
    public bool IsMismatch => ConfirmPassword.Length > 0 && ConfirmPassword != NewPassword;

    public PasswordChange Result => new(CurrentPassword, NewPassword);

    [RelayCommand(CanExecute = nameof(CanOk))]
    private void Ok() => CloseRequested?.Invoke(true);

    private bool CanOk() =>
        (!RequireCurrent || CurrentPassword.Length > 0) && NewPassword.Length > 0 && NewPassword == ConfirmPassword;
}
```

- [ ] **Step 5: `ApproveRegistrationViewModel.cs`**

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Transport.Domain.Enums;

namespace Transport.Presentation.Wpf.ViewModels.Dialogs;

public sealed record ApprovalInput(string TempPassword, StaffRole Role);

// Duyệt yêu cầu đăng ký: đặt mật khẩu tạm và chọn role
public sealed partial class ApproveRegistrationViewModel : ObservableObject, IDialogViewModel
{
    public ApproveRegistrationViewModel(string title)
    {
        Title = title;
        TempPassword = string.Empty;
        Role = StaffRole.TicketSeller;
    }

    public event Action<bool>? CloseRequested;

    public string Title { get; }

    public IReadOnlyList<StaffRole> Roles { get; } = Enum.GetValues<StaffRole>();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OkCommand))]
    public partial string TempPassword { get; set; }

    [ObservableProperty]
    public partial StaffRole Role { get; set; }

    public ApprovalInput Result => new(TempPassword, Role);

    [RelayCommand(CanExecute = nameof(CanOk))]
    private void Ok() => CloseRequested?.Invoke(true);

    private bool CanOk() => TempPassword.Length > 0;
}
```

- [ ] **Step 6: `RegisterViewModel.cs`**

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Transport.Application.Services;
using Transport.Domain.Errors;
using Transport.Presentation.Wpf.Localization;

namespace Transport.Presentation.Wpf.ViewModels.Dialogs;

// Form đăng ký ở màn hình đăng nhập; lỗi hiện ngay trong hộp thoại để không phải nhập lại
public sealed partial class RegisterViewModel : ObservableObject, IDialogViewModel
{
    private readonly RegistrationService _registrations;
    private readonly ILocalizationService _localization;

    public RegisterViewModel(RegistrationService registrations, ILocalizationService localization)
    {
        _registrations = registrations;
        _localization = localization;
        Username = string.Empty;
        FullName = string.Empty;
        Phone = string.Empty;
    }

    public event Action<bool>? CloseRequested;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    public partial string Username { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    public partial string FullName { get; set; }

    [ObservableProperty]
    public partial string Phone { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private async Task SubmitAsync()
    {
        ErrorMessage = null;
        try
        {
            await _registrations.SubmitAsync(Username, FullName, Phone);
            CloseRequested?.Invoke(true);
        }
        catch (BusinessRuleException ex)
        {
            ErrorMessage = _localization.Translate(ex);
        }
    }

    private bool CanSubmit() => !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(FullName);
}
```

- [ ] **Step 7: Ba file XAML + code-behind**

`Views/Dialogs/RegisterDialog.xaml`:
```xml
<Window x:Class="Transport.Presentation.Wpf.Views.Dialogs.RegisterDialog"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:l="clr-namespace:Transport.Presentation.Wpf.Localization"
        xmlns:vm="clr-namespace:Transport.Presentation.Wpf.ViewModels.Dialogs"
        mc:Ignorable="d"
        d:DataContext="{d:DesignInstance Type=vm:RegisterViewModel}"
        Title="{l:Loc Register_Title}" Width="440" SizeToContent="Height" ResizeMode="NoResize"
        WindowStartupLocation="CenterOwner" ShowInTaskbar="False"
        FocusManager.FocusedElement="{Binding ElementName=UsernameInput}">
    <StackPanel Margin="24">
        <TextBlock Text="{l:Loc Register_Hint}" TextWrapping="Wrap" Margin="0,0,0,8" />
        <TextBlock Text="{l:Loc Staff_Username}" Style="{StaticResource FieldLabelStyle}" />
        <TextBox x:Name="UsernameInput" Text="{Binding Username, UpdateSourceTrigger=PropertyChanged}" MaxLength="30"
                 AutomationProperties.Name="{l:Loc Staff_Username}" />
        <TextBlock Text="{l:Loc Staff_FullName}" Style="{StaticResource FieldLabelStyle}" />
        <TextBox Text="{Binding FullName, UpdateSourceTrigger=PropertyChanged}" MaxLength="100"
                 AutomationProperties.Name="{l:Loc Staff_FullName}" />
        <TextBlock Text="{l:Loc Staff_Phone}" Style="{StaticResource FieldLabelStyle}" />
        <TextBox Text="{Binding Phone, UpdateSourceTrigger=PropertyChanged}" MaxLength="10"
                 AutomationProperties.Name="{l:Loc Staff_Phone}" />
        <TextBlock Text="{Binding ErrorMessage}" Foreground="{StaticResource ErrorBrush}" TextWrapping="Wrap" Margin="0,12,0,0"
                   Visibility="{Binding ErrorMessage, Converter={StaticResource NullToCollapsed}}" />
        <StackPanel Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,20,0,0">
            <Button Content="{l:Loc Btn_Cancel}" IsCancel="True" Margin="0,0,8,0" MinWidth="96" />
            <Button Content="{l:Loc Register_Submit}" Command="{Binding SubmitCommand}" IsDefault="True"
                    Style="{StaticResource PrimaryButtonStyle}" MinWidth="96" />
        </StackPanel>
    </StackPanel>
</Window>
```

`Views/Dialogs/ChangePasswordDialog.xaml`:
```xml
<Window x:Class="Transport.Presentation.Wpf.Views.Dialogs.ChangePasswordDialog"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:l="clr-namespace:Transport.Presentation.Wpf.Localization"
        xmlns:c="clr-namespace:Transport.Presentation.Wpf.Controls"
        xmlns:vm="clr-namespace:Transport.Presentation.Wpf.ViewModels.Dialogs"
        mc:Ignorable="d"
        d:DataContext="{d:DesignInstance Type=vm:ChangePasswordViewModel}"
        Title="{Binding Title}" Width="440" SizeToContent="Height" ResizeMode="NoResize"
        WindowStartupLocation="CenterOwner" ShowInTaskbar="False">
    <StackPanel Margin="24">
        <TextBlock Text="{l:Loc ChangePassword_ExpiredHint}" TextWrapping="Wrap" Margin="0,0,0,8"
                   Visibility="{Binding RequireCurrent, Converter={StaticResource InverseBoolToVisibility}}" />
        <StackPanel Visibility="{Binding RequireCurrent, Converter={StaticResource BoolToVisibility}}">
            <TextBlock Text="{l:Loc ChangePassword_Current}" Style="{StaticResource FieldLabelStyle}" />
            <PasswordBox c:PasswordBoxAssistant.BoundPassword="{Binding CurrentPassword}"
                         AutomationProperties.Name="{l:Loc ChangePassword_Current}" />
        </StackPanel>
        <TextBlock Text="{l:Loc ChangePassword_New}" Style="{StaticResource FieldLabelStyle}" />
        <PasswordBox c:PasswordBoxAssistant.BoundPassword="{Binding NewPassword}"
                     AutomationProperties.Name="{l:Loc ChangePassword_New}" />
        <TextBlock Text="{l:Loc ChangePassword_Confirm}" Style="{StaticResource FieldLabelStyle}" />
        <PasswordBox c:PasswordBoxAssistant.BoundPassword="{Binding ConfirmPassword}"
                     AutomationProperties.Name="{l:Loc ChangePassword_Confirm}" />
        <TextBlock Text="{l:Loc ChangePassword_Mismatch}" Foreground="{StaticResource ErrorBrush}" Margin="0,8,0,0"
                   Visibility="{Binding IsMismatch, Converter={StaticResource BoolToVisibility}}" />
        <TextBlock Text="{l:Loc ChangePassword_Rules}" Style="{StaticResource StatLabelStyle}" TextWrapping="Wrap" Margin="0,8,0,0" />
        <StackPanel Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,20,0,0">
            <Button Content="{l:Loc Btn_Cancel}" IsCancel="True" Margin="0,0,8,0" MinWidth="96" />
            <Button Content="{l:Loc Btn_OK}" Command="{Binding OkCommand}" IsDefault="True"
                    Style="{StaticResource PrimaryButtonStyle}" MinWidth="96" />
        </StackPanel>
    </StackPanel>
</Window>
```

`Views/Dialogs/ApproveRegistrationDialog.xaml`:
```xml
<Window x:Class="Transport.Presentation.Wpf.Views.Dialogs.ApproveRegistrationDialog"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:l="clr-namespace:Transport.Presentation.Wpf.Localization"
        xmlns:c="clr-namespace:Transport.Presentation.Wpf.Controls"
        xmlns:vm="clr-namespace:Transport.Presentation.Wpf.ViewModels.Dialogs"
        mc:Ignorable="d"
        d:DataContext="{d:DesignInstance Type=vm:ApproveRegistrationViewModel}"
        Title="{Binding Title}" Width="440" SizeToContent="Height" ResizeMode="NoResize"
        WindowStartupLocation="CenterOwner" ShowInTaskbar="False"
        FocusManager.FocusedElement="{Binding ElementName=PasswordInput}">
    <StackPanel Margin="24">
        <TextBlock Text="{l:Loc Reg_TempPassword}" Style="{StaticResource FieldLabelStyle}" />
        <PasswordBox x:Name="PasswordInput" c:PasswordBoxAssistant.BoundPassword="{Binding TempPassword}"
                     AutomationProperties.Name="{l:Loc Reg_TempPassword}" />
        <TextBlock Text="{l:Loc ChangePassword_Rules}" Style="{StaticResource StatLabelStyle}" TextWrapping="Wrap" Margin="0,8,0,0" />
        <TextBlock Text="{l:Loc Staff_Role}" Style="{StaticResource FieldLabelStyle}" />
        <ComboBox ItemsSource="{Binding Roles}" SelectedItem="{Binding Role}" AutomationProperties.Name="{l:Loc Staff_Role}">
            <ComboBox.ItemTemplate>
                <DataTemplate>
                    <TextBlock Text="{Binding Converter={StaticResource Localize}, ConverterParameter=StaffRole_}" />
                </DataTemplate>
            </ComboBox.ItemTemplate>
        </ComboBox>
        <StackPanel Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,20,0,0">
            <Button Content="{l:Loc Btn_Cancel}" IsCancel="True" Margin="0,0,8,0" MinWidth="96" />
            <Button Content="{l:Loc Reg_Approve}" Command="{Binding OkCommand}" IsDefault="True"
                    Style="{StaticResource PrimaryButtonStyle}" MinWidth="96" />
        </StackPanel>
    </StackPanel>
</Window>
```

Code-behind (mỗi file một lớp, theo mẫu `PasswordPromptDialog.xaml.cs`):
```csharp
using System.Windows;

namespace Transport.Presentation.Wpf.Views.Dialogs;

public partial class RegisterDialog : Window
{
    public RegisterDialog() => InitializeComponent();
}
```
(tương tự `ChangePasswordDialog`, `ApproveRegistrationDialog`).

- [ ] **Step 8: `IDialogService` và `DialogService`**

`IDialogService.cs` – thêm `using Transport.Presentation.Wpf.ViewModels.Dialogs;` và trước `}` cuối:
```csharp

    // Form đăng ký ở màn hình đăng nhập; true nếu đã gửi yêu cầu thành công
    bool ShowRegistration();

    // Đổi mật khẩu; requireCurrent = false khi bị bắt đổi lúc đăng nhập. null nếu hủy
    PasswordChange? PromptPasswordChange(bool requireCurrent);

    // Duyệt yêu cầu đăng ký: mật khẩu tạm + role; null nếu hủy
    ApprovalInput? PromptApproval(string title);
```

`DialogService.cs` – đổi khai báo lớp thành:
```csharp
public sealed class DialogService(ILocalizationService localization, RegistrationService registrations) : IDialogService
```
thêm `using Transport.Application.Services;` và sau `ChoosePaymentMethod`:
```csharp

    public bool ShowRegistration() => ShowModal(new RegisterDialog(), new RegisterViewModel(registrations, localization));

    public PasswordChange? PromptPasswordChange(bool requireCurrent)
    {
        var title = localization[requireCurrent ? "ChangePassword_Title" : "ChangePassword_ExpiredTitle"];
        var viewModel = new ChangePasswordViewModel(title, requireCurrent);
        return ShowModal(new ChangePasswordDialog(), viewModel) ? viewModel.Result : null;
    }

    public ApprovalInput? PromptApproval(string title)
    {
        var viewModel = new ApproveRegistrationViewModel(title);
        return ShowModal(new ApproveRegistrationDialog(), viewModel) ? viewModel.Result : null;
    }
```

- [ ] **Step 9: `FakeDialogService`**

Thêm `using Transport.Presentation.Wpf.ViewModels.Dialogs;`, các thuộc tính sau `Prompts`:
```csharp
    public bool RegistrationResult { get; set; }
    public int RegistrationCount { get; private set; }
    public PasswordChange? PasswordChangeResult { get; set; }
    public List<bool> PasswordChangeRequests { get; } = [];
    public ApprovalInput? ApprovalResult { get; set; }
```
và các phương thức:
```csharp
    public bool ShowRegistration()
    {
        RegistrationCount++;
        return RegistrationResult;
    }

    public PasswordChange? PromptPasswordChange(bool requireCurrent)
    {
        PasswordChangeRequests.Add(requireCurrent);
        return PasswordChangeResult;
    }

    public ApprovalInput? PromptApproval(string title)
    {
        Prompts.Add(title);
        return ApprovalResult;
    }
```

- [ ] **Step 10: Chuỗi giao diện**

`Strings.resx`, thêm trước `</root>`:
```xml
  <data name="Register_Title" xml:space="preserve"><value>Request an account</value></data>
  <data name="Register_Hint" xml:space="preserve"><value>An operator will review your request and give you a temporary password.</value></data>
  <data name="Register_Submit" xml:space="preserve"><value>Send request</value></data>
  <data name="ChangePassword_Title" xml:space="preserve"><value>Change password</value></data>
  <data name="ChangePassword_ExpiredTitle" xml:space="preserve"><value>Set a new password</value></data>
  <data name="ChangePassword_ExpiredHint" xml:space="preserve"><value>Your password has expired or was set by an operator. Choose a new password to continue.</value></data>
  <data name="ChangePassword_Current" xml:space="preserve"><value>Current password</value></data>
  <data name="ChangePassword_New" xml:space="preserve"><value>New password</value></data>
  <data name="ChangePassword_Confirm" xml:space="preserve"><value>Confirm new password</value></data>
  <data name="ChangePassword_Mismatch" xml:space="preserve"><value>The passwords do not match.</value></data>
  <data name="ChangePassword_Rules" xml:space="preserve"><value>At least 8 characters with an uppercase letter, a lowercase letter, a digit and a special character; must not contain the username.</value></data>
  <data name="Reg_TempPassword" xml:space="preserve"><value>Temporary password</value></data>
  <data name="Reg_Approve" xml:space="preserve"><value>Approve</value></data>
```

`Strings.vi.resx`, thêm trước `</root>`:
```xml
  <data name="Register_Title" xml:space="preserve"><value>Đăng ký tài khoản</value></data>
  <data name="Register_Hint" xml:space="preserve"><value>Điều hành sẽ duyệt yêu cầu và cấp mật khẩu tạm cho bạn.</value></data>
  <data name="Register_Submit" xml:space="preserve"><value>Gửi yêu cầu</value></data>
  <data name="ChangePassword_Title" xml:space="preserve"><value>Đổi mật khẩu</value></data>
  <data name="ChangePassword_ExpiredTitle" xml:space="preserve"><value>Đặt mật khẩu mới</value></data>
  <data name="ChangePassword_ExpiredHint" xml:space="preserve"><value>Mật khẩu đã hết hạn hoặc do điều hành cấp. Hãy đặt mật khẩu mới để tiếp tục.</value></data>
  <data name="ChangePassword_Current" xml:space="preserve"><value>Mật khẩu hiện tại</value></data>
  <data name="ChangePassword_New" xml:space="preserve"><value>Mật khẩu mới</value></data>
  <data name="ChangePassword_Confirm" xml:space="preserve"><value>Nhập lại mật khẩu mới</value></data>
  <data name="ChangePassword_Mismatch" xml:space="preserve"><value>Mật khẩu nhập lại không khớp.</value></data>
  <data name="ChangePassword_Rules" xml:space="preserve"><value>Tối thiểu 8 ký tự, có chữ hoa, chữ thường, chữ số và ký tự đặc biệt; không chứa tên đăng nhập.</value></data>
  <data name="Reg_TempPassword" xml:space="preserve"><value>Mật khẩu tạm</value></data>
  <data name="Reg_Approve" xml:space="preserve"><value>Duyệt</value></data>
```

- [ ] **Step 11: Chạy test**

Run: `dotnet build TransportManagement.slnx` rồi `dotnet test --project tests/Transport.UnitTests`
Expected: 0 warning, tất cả PASS (gồm `ViewInstantiationTests`, `ResourceParityTests`).

- [ ] **Step 12: Commit**

```bash
node .gitnexus/run.cjs detect-changes --scope all --repo .
git add src tests
git commit -m "feat(ui): registration, change password and approval dialogs"
```

---

### Task 8: Nối luồng vào màn hình đăng nhập và cửa sổ chính

**Files:**
- Modify: `src/Transport.Presentation.Wpf/ViewModels/LoginViewModel.cs`, `Views/LoginWindow.xaml`, `ViewModels/MainViewModel.cs`, `Views/MainWindow.xaml`, `App.xaml.cs`, `Resources/Strings*.resx`
- Test: `tests/Transport.UnitTests/Presentation/LoginViewModelTests.cs`, `MainViewModelTests.cs`

**Interfaces:**
- Consumes: `IAuthService.LoginWithNewPasswordAsync`, `ChangePasswordAsync` (Task 5); `IDialogService.ShowRegistration`, `PromptPasswordChange` (Task 7).
- Produces: `LoginViewModel(IAuthService auth, ILocalizationService localization, IWindowService windows, IDialogService dialogs)`, thuộc tính `InfoMessage`, `RegisterCommand`; `MainViewModel.ChangePasswordCommand`.

- [ ] **Step 1: Impact analysis**

Run: `node .gitnexus/run.cjs impact "LoginViewModel" --direction upstream --repo .`, `... impact "MainViewModel" ...`
Expected: tạo qua DI (`PresentationServices`) và test. Đổi constructor `LoginViewModel` → sửa test.

- [ ] **Step 2: Viết test thất bại**

`LoginViewModelTests.cs`: thêm field `private readonly FakeDialogService _dialogs = new();`, đổi `CreateViewModel` thành `new(_auth, _localization, _windows, _dialogs)`, thêm `using Transport.Presentation.Wpf.ViewModels.Dialogs;`. Trong `Login_Failure_ShowsTranslatedError` thêm `Assert.Equal(string.Empty, login.Password);`. Thêm:
```csharp
    [Fact]
    public async Task ExpiredPassword_AsksForNewPasswordThenSignsIn()
    {
        _auth.LoginError = new BusinessRuleException(BusinessErrorCode.PasswordExpired, "expired");
        _dialogs.PasswordChangeResult = new PasswordChange(string.Empty, "NewPass#2");
        var login = CreateViewModel();
        login.Username = " seller01 ";
        login.Password = "Seller#001";

        await login.LoginCommand.ExecuteAsync(null);

        Assert.Equal(new[] { false }, _dialogs.PasswordChangeRequests);
        Assert.Equal(("seller01", "Seller#001", "NewPass#2"), _auth.LastNewPasswordLogin);
        Assert.Equal(new[] { "main" }, _windows.Shown);
    }

    [Fact]
    public async Task ExpiredPassword_Cancelled_StaysOnLogin()
    {
        _auth.LoginError = new BusinessRuleException(BusinessErrorCode.PasswordExpired, "expired");
        var login = CreateViewModel();
        login.Username = "seller01";
        login.Password = "Seller#001";

        await login.LoginCommand.ExecuteAsync(null);

        Assert.Null(_auth.LastNewPasswordLogin);
        Assert.Empty(_windows.Shown);
        Assert.Equal(string.Empty, login.Password);
    }

    [Fact]
    public async Task ExpiredPassword_WeakNewPassword_ShowsError()
    {
        _auth.LoginError = new BusinessRuleException(BusinessErrorCode.PasswordExpired, "expired");
        _auth.NewPasswordError = new BusinessRuleException(BusinessErrorCode.InvalidPassword, "weak");
        _dialogs.PasswordChangeResult = new PasswordChange(string.Empty, "weak");
        var login = CreateViewModel();
        login.Username = "seller01";
        login.Password = "Seller#001";

        await login.LoginCommand.ExecuteAsync(null);

        Assert.Equal("Err_20044", login.ErrorMessage);
        Assert.Empty(_windows.Shown);
    }

    [Theory]
    [InlineData(true, "Register_Submitted")]
    [InlineData(false, null)]
    public void Register_ShowsConfirmationOnlyWhenSubmitted(bool submitted, string? expected)
    {
        _dialogs.RegistrationResult = submitted;
        var login = CreateViewModel();

        login.RegisterCommand.Execute(null);

        Assert.Equal(1, _dialogs.RegistrationCount);
        Assert.Equal(expected, login.InfoMessage);
    }
```

`MainViewModelTests.cs` – thêm `using Transport.Domain.Errors;`, `using Transport.Presentation.Wpf.ViewModels.Dialogs;` và:
```csharp
    [Fact]
    public async Task ChangePassword_Success_ShowsNotification()
    {
        _dialogs.PasswordChangeResult = new PasswordChange("Old#Pass1", "NewPass#2");
        var vm = Create(new FakeUserSession());

        await vm.ChangePasswordCommand.ExecuteAsync(null);

        Assert.Equal(new[] { true }, _dialogs.PasswordChangeRequests);
        Assert.Equal(("Old#Pass1", "NewPass#2"), _auth.LastChange);
        Assert.Equal("ChangePassword_Done", vm.Notifications.Message);
    }

    [Fact]
    public async Task ChangePassword_Failure_ShowsTranslatedError()
    {
        _dialogs.PasswordChangeResult = new PasswordChange("wrong", "NewPass#2");
        _auth.ChangeError = new BusinessRuleException(BusinessErrorCode.InvalidCredentials, "invalid");
        var vm = Create(new FakeUserSession());

        await vm.ChangePasswordCommand.ExecuteAsync(null);

        Assert.Equal("Err_1017", vm.Notifications.Message);
        Assert.Equal(NotificationSeverity.Error, vm.Notifications.Severity);
    }

    [Fact]
    public async Task ChangePassword_Cancelled_DoesNothing()
    {
        var vm = Create(new FakeUserSession());

        await vm.ChangePasswordCommand.ExecuteAsync(null);

        Assert.Null(_auth.LastChange);
    }
```

- [ ] **Step 3: Chạy test, xác nhận thất bại**

Run: `dotnet test --project tests/Transport.UnitTests --filter-class "*LoginViewModelTests"`
Expected: lỗi biên dịch (constructor 4 tham số, `RegisterCommand`, `ChangePasswordCommand` chưa có).

- [ ] **Step 4: Viết lại `LoginViewModel.cs`**

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Transport.Application.Abstractions;
using Transport.Domain.Errors;
using Transport.Presentation.Wpf.Localization;
using Transport.Presentation.Wpf.Services;

namespace Transport.Presentation.Wpf.ViewModels;

public sealed partial class LoginViewModel : ObservableObject
{
    private readonly IAuthService _auth;
    private readonly ILocalizationService _localization;
    private readonly IWindowService _windows;
    private readonly IDialogService _dialogs;

    public LoginViewModel(IAuthService auth, ILocalizationService localization, IWindowService windows, IDialogService dialogs)
    {
        _auth = auth;
        _localization = localization;
        _windows = windows;
        _dialogs = dialogs;
        Username = string.Empty;
        Password = string.Empty;
        Language = localization.CurrentLanguage;
    }

    public IReadOnlyList<string> Languages { get; } = ["vi", "en"];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    public partial string Username { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    public partial string Password { get; set; }

    [ObservableProperty]
    public partial string Language { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial string? InfoMessage { get; set; }

    partial void OnLanguageChanged(string value) => _localization.SetLanguage(value);

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync()
    {
        ErrorMessage = null;
        InfoMessage = null;
        var username = Username.Trim();
        try
        {
            await _auth.LoginAsync(username, Password);
            _windows.ShowMain();
        }
        catch (BusinessRuleException ex) when (ex.Code == BusinessErrorCode.PasswordExpired)
        {
            await ChangeExpiredPasswordAsync(username);
        }
        catch (BusinessRuleException ex)
        {
            Fail(ex);
        }
    }

    [RelayCommand]
    private void Register()
    {
        if (_dialogs.ShowRegistration())
        {
            InfoMessage = _localization["Register_Submitted"];
        }
    }

    // Mật khẩu hết hạn hoặc là mật khẩu tạm do điều hành cấp: bắt đặt mật khẩu mới rồi mới vào app
    private async Task ChangeExpiredPasswordAsync(string username)
    {
        if (_dialogs.PromptPasswordChange(requireCurrent: false) is not { } change)
        {
            Password = string.Empty;
            return;
        }

        try
        {
            await _auth.LoginWithNewPasswordAsync(username, Password, change.NewPassword);
            _windows.ShowMain();
        }
        catch (BusinessRuleException ex)
        {
            Fail(ex);
        }
    }

    // Báo lỗi và xóa ô mật khẩu để không thử lại bằng mật khẩu cũ
    private void Fail(BusinessRuleException ex)
    {
        ErrorMessage = _localization.Translate(ex);
        Password = string.Empty;
    }

    private bool CanLogin() => Username.Trim().Length > 0 && Password.Length > 0;
}
```

- [ ] **Step 5: `LoginWindow.xaml`**

Thay khối từ `<Button Content="{l:Loc Login_Button}" ...` đến hết `<ProgressBar ... />` bằng:
```xml
            <Button Content="{l:Loc Login_Button}" Command="{Binding LoginCommand}" IsDefault="True"
                    Style="{StaticResource PrimaryButtonStyle}" HorizontalAlignment="Stretch" Margin="0,24,0,0" />
            <ProgressBar IsIndeterminate="True" Height="3" Margin="0,8,0,0"
                         Visibility="{Binding LoginCommand.IsRunning, Converter={StaticResource BoolToVisibility}}" />

            <TextBlock Text="{Binding InfoMessage}" Foreground="{StaticResource SuccessBrush}" TextWrapping="Wrap"
                       Margin="0,12,0,0" Visibility="{Binding InfoMessage, Converter={StaticResource NullToCollapsed}}" />
            <Button Content="{l:Loc Login_Register}" Command="{Binding RegisterCommand}"
                    HorizontalAlignment="Center" Margin="0,16,0,0" />
```

- [ ] **Step 6: `MainViewModel.cs` – lệnh đổi mật khẩu**

Thêm `using Transport.Domain.Errors;` và sau phương thức `Logout()`:
```csharp

    [RelayCommand]
    private async Task ChangePasswordAsync()
    {
        if (_dialogs.PromptPasswordChange(requireCurrent: true) is not { } change)
        {
            return;
        }

        try
        {
            await _auth.ChangePasswordAsync(change.CurrentPassword, change.NewPassword);
            Notifications.ShowSuccess(_localization["ChangePassword_Done"]);
        }
        catch (BusinessRuleException ex)
        {
            Notifications.ShowError(_localization.Translate(ex));
        }
    }
```

- [ ] **Step 7: `MainWindow.xaml` – nút Đổi mật khẩu**

Ngay trước `<Button Command="{Binding ToggleLanguageCommand}" ...>` thêm:
```xml
                    <Button Command="{Binding ChangePasswordCommand}" AutomationProperties.Name="{l:Loc Btn_ChangePassword}" Margin="0,0,8,0">
                        <StackPanel Orientation="Horizontal">
                            <TextBlock Text="&#xE8D7;" FontFamily="{StaticResource SymbolFont}" Margin="0,0,8,0" VerticalAlignment="Center" />
                            <TextBlock Text="{l:Loc Btn_ChangePassword}" />
                        </StackPanel>
                    </Button>
```

- [ ] **Step 8: `App.xaml.cs` – không hiện chi tiết lỗi**

Thêm `using System.Diagnostics;` và thay thân `OnDispatcherUnhandledException`:
```csharp
    // Lỗi ngoài dự kiến: báo chung cho người dùng (không lộ chi tiết hệ thống), chi tiết chỉ ghi ra Debug
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Debug.WriteLine(e.Exception);
        MessageBox.Show(LocalizationService.Instance["Err_Unexpected"], LocalizationService.Instance["App_Title"],
            MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
```
(Xóa comment cũ `// Lỗi ngoài dự kiến: báo cho người dùng thay vì tắt ứng dụng`.)

- [ ] **Step 9: Chuỗi giao diện**

`Strings.resx`, thêm trước `</root>`:
```xml
  <data name="Login_Register" xml:space="preserve"><value>Request an account</value></data>
  <data name="Register_Submitted" xml:space="preserve"><value>Request sent. Please wait for an operator to approve it and give you a temporary password.</value></data>
  <data name="Btn_ChangePassword" xml:space="preserve"><value>Change password</value></data>
  <data name="ChangePassword_Done" xml:space="preserve"><value>Password changed.</value></data>
  <data name="Err_Unexpected" xml:space="preserve"><value>An unexpected error occurred. Please try again.</value></data>
```
`Strings.vi.resx`:
```xml
  <data name="Login_Register" xml:space="preserve"><value>Đăng ký tài khoản</value></data>
  <data name="Register_Submitted" xml:space="preserve"><value>Đã gửi yêu cầu. Vui lòng chờ điều hành duyệt và cấp mật khẩu tạm.</value></data>
  <data name="Btn_ChangePassword" xml:space="preserve"><value>Đổi mật khẩu</value></data>
  <data name="ChangePassword_Done" xml:space="preserve"><value>Đã đổi mật khẩu.</value></data>
  <data name="Err_Unexpected" xml:space="preserve"><value>Đã xảy ra lỗi không mong muốn. Vui lòng thử lại.</value></data>
```

- [ ] **Step 10: Chạy test**

Run: `dotnet build TransportManagement.slnx` rồi `dotnet test --project tests/Transport.UnitTests`
Expected: 0 warning, tất cả PASS (gồm `DependencyInjectionTests` resolve `LoginViewModel`).

- [ ] **Step 11: Commit**

```bash
node .gitnexus/run.cjs detect-changes --scope all --repo .
git add src tests
git commit -m "feat(ui): forced password change, self-service password change and registration link"
```

---

### Task 9: Trang "Yêu cầu đăng ký" (điều hành)

**Files:**
- Create: `src/Transport.Presentation.Wpf/ViewModels/Pages/RegistrationRequestsViewModel.cs`, `Views/Pages/RegistrationRequestsPage.xaml(.cs)`, `tests/Transport.UnitTests/Presentation/RegistrationRequestsViewModelTests.cs`
- Modify: `PresentationServices.cs`, `Views/PageTemplates.xaml`, `Resources/Strings*.resx`, `tests/.../DependencyInjectionTests.cs`, `tests/.../ViewInstantiationTests.cs`

**Interfaces:**
- Consumes: `RegistrationService` (Task 6), `IDialogService.PromptApproval`, `PromptText` (Task 7).
- Produces: `RegistrationRequestsViewModel(PageServices services, RegistrationService registrations)`; trang menu `Menu_Registrations`, thứ tự 101, chỉ điều hành.

- [ ] **Step 1: Viết test thất bại – `RegistrationRequestsViewModelTests.cs`**

```csharp
using Transport.Application.Models;
using Transport.Application.Services;
using Transport.Domain.Enums;
using Transport.Presentation.Wpf.ViewModels.Dialogs;
using Transport.Presentation.Wpf.ViewModels.Pages;
using Transport.UnitTests.Fakes;

namespace Transport.UnitTests.Presentation;

public class RegistrationRequestsViewModelTests
{
    private readonly PageTestContext _context = new();
    private readonly FakeRegistrationRepository _repository = new();

    public RegistrationRequestsViewModelTests()
    {
        _repository.Requests.Add(new RegistrationRequest
        {
            RequestId = 7, Username = "NEWUSER01", FullName = "Người Mới",
            Status = RegistrationStatus.Pending, RequestedAt = new DateTime(2026, 10, 18, 9, 0, 0),
        });
        _repository.Requests.Add(new RegistrationRequest
        {
            RequestId = 6, Username = "OLDUSER01", FullName = "Người Cũ",
            Status = RegistrationStatus.Approved, RequestedAt = new DateTime(2026, 10, 17, 9, 0, 0),
        });
    }

    private RegistrationRequestsViewModel Create() => new(_context.Services, new RegistrationService(_repository, _context.Session));

    [Fact]
    public async Task Load_ShowsPendingOnlyByDefault()
    {
        var vm = Create();

        await vm.LoadAsync();
        Assert.Equal(new[] { "NEWUSER01" }, vm.Items.Select(r => r.Username));

        vm.ShowOnlyPending = false;
        Assert.Equal(2, vm.Items.Count);
    }

    [Fact]
    public async Task Approve_SendsDialogInputAndReloads()
    {
        _context.Dialogs.ApprovalResult = new ApprovalInput("TempPass#1", StaffRole.Operator);
        var vm = Create();
        await vm.LoadAsync();
        vm.SelectedItem = vm.Items[0];

        await vm.ApproveCommand.ExecuteAsync(null);

        Assert.Equal(new[] { "Reg_ApproveTitle|NEWUSER01" }, _context.Dialogs.Prompts);
        Assert.Equal(new[] { "approve:7:Operator" }, _repository.Calls);
        Assert.Equal(new[] { "Reg_Approved" }, _context.Notifications.Successes);
    }

    [Fact]
    public async Task Approve_Cancelled_DoesNothing()
    {
        var vm = Create();
        await vm.LoadAsync();
        vm.SelectedItem = vm.Items[0];

        await vm.ApproveCommand.ExecuteAsync(null);

        Assert.Empty(_repository.Calls);
    }

    [Fact]
    public async Task Reject_SendsReason()
    {
        _context.Dialogs.PromptTextResult = "Trùng người";
        var vm = Create();
        await vm.LoadAsync();
        vm.SelectedItem = vm.Items[0];

        await vm.RejectCommand.ExecuteAsync(null);

        Assert.Equal(new[] { "reject:7:Trùng người" }, _repository.Calls);
        Assert.Equal(new[] { "Reg_Rejected" }, _context.Notifications.Successes);
    }

    [Fact]
    public async Task ReviewedRequests_CannotBeApprovedOrRejected()
    {
        var vm = Create();
        await vm.LoadAsync();
        vm.ShowOnlyPending = false;

        vm.SelectedItem = vm.Items.Single(r => r.Status == RegistrationStatus.Approved);

        Assert.False(vm.ApproveCommand.CanExecute(null));
        Assert.False(vm.RejectCommand.CanExecute(null));
    }
}
```

Trong `DependencyInjectionTests.cs` đổi `Assert.Equal(11, descriptors.Count);` → `Assert.Equal(12, descriptors.Count);`.
Trong `ViewInstantiationTests.cs` thêm `_ = new RegistrationRequestsPage();` sau `_ = new BackupPage();`.

- [ ] **Step 2: Chạy test, xác nhận thất bại**

Run: `dotnet test --project tests/Transport.UnitTests --filter-class "*RegistrationRequestsViewModelTests"`
Expected: lỗi biên dịch.

- [ ] **Step 3: `RegistrationRequestsViewModel.cs`**

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Transport.Application.Models;
using Transport.Application.Services;
using Transport.Domain.Enums;

namespace Transport.Presentation.Wpf.ViewModels.Pages;

// Duyệt/từ chối yêu cầu đăng ký tài khoản (PKG_REGISTRATION) – chỉ điều hành
public sealed partial class RegistrationRequestsViewModel : PageViewModel
{
    private readonly RegistrationService _registrations;
    private List<RegistrationRequest> _all = [];

    public RegistrationRequestsViewModel(PageServices services, RegistrationService registrations)
        : base(services)
    {
        _registrations = registrations;
        ShowOnlyPending = true;
    }

    public ObservableCollection<RegistrationRequest> Items { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApproveCommand))]
    [NotifyCanExecuteChangedFor(nameof(RejectCommand))]
    public partial RegistrationRequest? SelectedItem { get; set; }

    [ObservableProperty]
    public partial bool ShowOnlyPending { get; set; }

    public override Task LoadAsync() => RunBusyAsync(ReloadAsync);

    partial void OnShowOnlyPendingChanged(bool value) => ApplyFilter();

    [RelayCommand]
    private Task RefreshAsync() => RunBusyAsync(ReloadAsync);

    [RelayCommand(CanExecute = nameof(IsPendingSelected))]
    private Task ApproveAsync() => RunBusyAsync(async () =>
    {
        var request = SelectedItem!;
        if (Dialogs.PromptApproval(Loc.Format("Reg_ApproveTitle", request.Username)) is not { } input)
        {
            return;
        }

        await _registrations.ApproveAsync(request, input.TempPassword, input.Role);
        Notify.ShowSuccess(Loc["Reg_Approved"]);
        await ReloadAsync();
    });

    [RelayCommand(CanExecute = nameof(IsPendingSelected))]
    private Task RejectAsync() => RunBusyAsync(async () =>
    {
        var request = SelectedItem!;
        if (Dialogs.PromptText(Loc.Format("Reg_RejectTitle", request.Username), Loc["Reg_RejectReason"]) is not { } reason)
        {
            return;
        }

        await _registrations.RejectAsync(request.RequestId, reason);
        Notify.ShowSuccess(Loc["Reg_Rejected"]);
        await ReloadAsync();
    });

    [RelayCommand]
    private Task ExportToExcelAsync() => ExportAsync(Items.ToList(),
    [
        new("Reg_Id", r => r.RequestId),
        new("Staff_Username", r => r.Username),
        new("Staff_FullName", r => r.FullName),
        new("Staff_Phone", r => r.Phone),
        new("Reg_Status", r => Loc[$"RegStatus_{r.Status}"]),
        new("Reg_RequestedAt", r => r.RequestedAt, "dd/MM/yyyy HH:mm"),
        new("Reg_Host", r => r.ClientHost),
        new("Reg_ReviewedBy", r => r.ReviewedBy),
        new("Reg_ReviewedAt", r => r.ReviewedAt, "dd/MM/yyyy HH:mm"),
        new("Reg_RejectReason", r => r.RejectReason),
    ], "Reg_ExportTitle");

    private async Task ReloadAsync()
    {
        _all = [.. await _registrations.GetAllAsync()];
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        Items.Clear();
        foreach (var request in _all.Where(r => !ShowOnlyPending || r.Status == RegistrationStatus.Pending))
        {
            Items.Add(request);
        }
    }

    private bool IsPendingSelected() => SelectedItem?.Status == RegistrationStatus.Pending;
}
```

- [ ] **Step 4: `RegistrationRequestsPage.xaml` + `.xaml.cs`**

```xml
<UserControl x:Class="Transport.Presentation.Wpf.Views.Pages.RegistrationRequestsPage"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:l="clr-namespace:Transport.Presentation.Wpf.Localization"
             xmlns:c="clr-namespace:Transport.Presentation.Wpf.Controls"
             xmlns:vm="clr-namespace:Transport.Presentation.Wpf.ViewModels.Pages"
             mc:Ignorable="d" d:DesignWidth="1100" d:DesignHeight="680"
             d:DataContext="{d:DesignInstance Type=vm:RegistrationRequestsViewModel}">
    <Border Style="{StaticResource CardStyle}">
        <DockPanel>
            <DockPanel DockPanel.Dock="Top" Margin="0,0,0,8">
                <StackPanel DockPanel.Dock="Right" Orientation="Horizontal">
                    <Button Content="{l:Loc Reg_Approve}" Command="{Binding ApproveCommand}" Style="{StaticResource PrimaryButtonStyle}" Margin="8,0,0,0" />
                    <Button Content="{l:Loc Reg_Reject}" Command="{Binding RejectCommand}" Margin="8,0,0,0" />
                    <Button Content="{l:Loc Btn_Refresh}" Command="{Binding RefreshCommand}" c:Shortcut.Gesture="F5" Margin="8,0,0,0" />
                    <Button Content="{l:Loc Btn_ExportExcel}" Command="{Binding ExportToExcelCommand}" c:Shortcut.Gesture="Ctrl+E" Margin="8,0,0,0" />
                </StackPanel>
                <CheckBox Content="{l:Loc Reg_OnlyPending}" IsChecked="{Binding ShowOnlyPending}" VerticalAlignment="Center" />
            </DockPanel>
            <DataGrid Style="{StaticResource ListGridStyle}" ItemsSource="{Binding Items}" SelectedItem="{Binding SelectedItem}"
                      AutomationProperties.Name="{l:Loc Menu_Registrations}">
                <DataGrid.Columns>
                    <DataGridTextColumn Header="{l:Loc Reg_Id}" Binding="{Binding RequestId}" Width="Auto" />
                    <DataGridTextColumn Header="{l:Loc Staff_Username}" Binding="{Binding Username}" Width="*" />
                    <DataGridTextColumn Header="{l:Loc Staff_FullName}" Binding="{Binding FullName}" Width="2*" />
                    <DataGridTextColumn Header="{l:Loc Staff_Phone}" Binding="{Binding Phone}" Width="Auto" />
                    <DataGridTextColumn Header="{l:Loc Reg_Status}" Width="Auto"
                                        Binding="{Binding Status, Converter={StaticResource Localize}, ConverterParameter=RegStatus_}" />
                    <DataGridTextColumn Header="{l:Loc Reg_RequestedAt}" Binding="{Binding RequestedAt, StringFormat={}{0:dd/MM/yyyy HH:mm}}" Width="Auto" />
                    <DataGridTextColumn Header="{l:Loc Reg_Host}" Binding="{Binding ClientHost}" Width="*" />
                    <DataGridTextColumn Header="{l:Loc Reg_ReviewedBy}" Binding="{Binding ReviewedBy}" Width="*" />
                    <DataGridTextColumn Header="{l:Loc Reg_RejectReason}" Binding="{Binding RejectReason}" Width="2*" />
                </DataGrid.Columns>
            </DataGrid>
        </DockPanel>
    </Border>
</UserControl>
```

```csharp
using System.Windows.Controls;

namespace Transport.Presentation.Wpf.Views.Pages;

public partial class RegistrationRequestsPage : UserControl
{
    public RegistrationRequestsPage() => InitializeComponent();
}
```
(Kiểm tra mẫu code-behind của `BackupPage.xaml.cs` và làm giống hệt.)

- [ ] **Step 5: Đăng ký trang**

`PageTemplates.xaml`, thêm sau dòng `StaffViewModel`:
```xml
    <DataTemplate DataType="{x:Type vm:RegistrationRequestsViewModel}"><pages:RegistrationRequestsPage /></DataTemplate>
```
`PresentationServices.AddPages`, thêm sau dòng `StaffViewModel`:
```csharp
        services.AddPage<RegistrationRequestsViewModel>("Menu_Registrations", "\uE8FA", 101, operatorOnly: true);
```

- [ ] **Step 6: Chuỗi giao diện**

`Strings.resx`:
```xml
  <data name="Menu_Registrations" xml:space="preserve"><value>Account requests</value></data>
  <data name="Reg_Id" xml:space="preserve"><value>#</value></data>
  <data name="Reg_Status" xml:space="preserve"><value>Status</value></data>
  <data name="Reg_RequestedAt" xml:space="preserve"><value>Requested at</value></data>
  <data name="Reg_Host" xml:space="preserve"><value>Computer</value></data>
  <data name="Reg_ReviewedBy" xml:space="preserve"><value>Reviewed by</value></data>
  <data name="Reg_ReviewedAt" xml:space="preserve"><value>Reviewed at</value></data>
  <data name="Reg_RejectReason" xml:space="preserve"><value>Reject reason</value></data>
  <data name="Reg_OnlyPending" xml:space="preserve"><value>Pending requests only</value></data>
  <data name="Reg_Reject" xml:space="preserve"><value>Reject</value></data>
  <data name="Reg_ApproveTitle" xml:space="preserve"><value>Approve request of {0}</value></data>
  <data name="Reg_RejectTitle" xml:space="preserve"><value>Reject request of {0}</value></data>
  <data name="Reg_Approved" xml:space="preserve"><value>Account created. Give the temporary password to the user in person.</value></data>
  <data name="Reg_Rejected" xml:space="preserve"><value>Request rejected.</value></data>
  <data name="Reg_ExportTitle" xml:space="preserve"><value>Account requests</value></data>
  <data name="RegStatus_Pending" xml:space="preserve"><value>Pending</value></data>
  <data name="RegStatus_Approved" xml:space="preserve"><value>Approved</value></data>
  <data name="RegStatus_Rejected" xml:space="preserve"><value>Rejected</value></data>
```
`Strings.vi.resx`:
```xml
  <data name="Menu_Registrations" xml:space="preserve"><value>Yêu cầu đăng ký</value></data>
  <data name="Reg_Id" xml:space="preserve"><value>Mã</value></data>
  <data name="Reg_Status" xml:space="preserve"><value>Trạng thái</value></data>
  <data name="Reg_RequestedAt" xml:space="preserve"><value>Thời gian gửi</value></data>
  <data name="Reg_Host" xml:space="preserve"><value>Máy gửi</value></data>
  <data name="Reg_ReviewedBy" xml:space="preserve"><value>Người duyệt</value></data>
  <data name="Reg_ReviewedAt" xml:space="preserve"><value>Thời gian duyệt</value></data>
  <data name="Reg_RejectReason" xml:space="preserve"><value>Lý do từ chối</value></data>
  <data name="Reg_OnlyPending" xml:space="preserve"><value>Chỉ yêu cầu đang chờ</value></data>
  <data name="Reg_Reject" xml:space="preserve"><value>Từ chối</value></data>
  <data name="Reg_ApproveTitle" xml:space="preserve"><value>Duyệt yêu cầu của {0}</value></data>
  <data name="Reg_RejectTitle" xml:space="preserve"><value>Từ chối yêu cầu của {0}</value></data>
  <data name="Reg_Approved" xml:space="preserve"><value>Đã tạo tài khoản. Hãy đưa trực tiếp mật khẩu tạm cho người dùng.</value></data>
  <data name="Reg_Rejected" xml:space="preserve"><value>Đã từ chối yêu cầu.</value></data>
  <data name="Reg_ExportTitle" xml:space="preserve"><value>Yêu cầu đăng ký</value></data>
  <data name="RegStatus_Pending" xml:space="preserve"><value>Đang chờ</value></data>
  <data name="RegStatus_Approved" xml:space="preserve"><value>Đã duyệt</value></data>
  <data name="RegStatus_Rejected" xml:space="preserve"><value>Đã từ chối</value></data>
```

- [ ] **Step 7: Chạy test**

Run: `dotnet build TransportManagement.slnx` rồi `dotnet test --project tests/Transport.UnitTests`
Expected: 0 warning, tất cả PASS.

- [ ] **Step 8: Commit**

```bash
node .gitnexus/run.cjs detect-changes --scope all --repo .
git add src tests
git commit -m "feat(ui): account requests page for operators"
```

---

### Task 10: Trang "Nhật ký đăng nhập" (điều hành)

**Files:**
- Create: `src/Transport.Application/Models/LoginAuditEntry.cs`, `src/Transport.Application/Abstractions/ILoginAuditQueries.cs`, `src/Transport.Infrastructure/Repositories/LoginAuditQueries.cs`, `src/Transport.Presentation.Wpf/ViewModels/Pages/LoginAuditViewModel.cs`, `Views/Pages/LoginAuditPage.xaml(.cs)`, `tests/Transport.UnitTests/Fakes/FakeLoginAuditQueries.cs`, `tests/Transport.UnitTests/Presentation/LoginAuditViewModelTests.cs`
- Modify: `src/Transport.Infrastructure/DependencyInjection.cs`, `PresentationServices.cs`, `Views/PageTemplates.xaml`, `Resources/Strings*.resx`, `DependencyInjectionTests.cs`, `ViewInstantiationTests.cs`

**Interfaces:**
- Consumes: view `v_login_audit` (Task 3).
- Produces: `ILoginAuditQueries.GetAsync(string? username, DateTime from, DateTime to, CancellationToken ct = default)`; `record LoginAuditEntry { DateTime EventTime; string Username; string ActionName; string Result; int ReturnCode; string? UserHost; string? ObjectName }`; `LoginAuditViewModel(PageServices services, ILoginAuditQueries audit, TimeProvider time)`; trang `Menu_LoginAudit`, thứ tự 102.

- [ ] **Step 1: Model, giao diện, fake**

`LoginAuditEntry.cs`:
```csharp
namespace Transport.Application.Models;

// Một dòng V_LOGIN_AUDIT
public sealed record LoginAuditEntry
{
    public DateTime EventTime { get; init; }
    public required string Username { get; init; }
    public required string ActionName { get; init; }
    // 'SUCCESS' hoặc 'FAILED'
    public required string Result { get; init; }
    public int ReturnCode { get; init; }
    public string? UserHost { get; init; }
    public string? ObjectName { get; init; }
}
```

`ILoginAuditQueries.cs`:
```csharp
using Transport.Application.Models;

namespace Transport.Application.Abstractions;

// Đọc V_LOGIN_AUDIT (quyền SELECT chỉ cấp cho ROLE_OPERATOR)
public interface ILoginAuditQueries
{
    // from/to tính theo ngày, gồm cả ngày "to"; username null/rỗng = mọi tài khoản
    Task<IReadOnlyList<LoginAuditEntry>> GetAsync(string? username, DateTime from, DateTime to, CancellationToken ct = default);
}
```

`FakeLoginAuditQueries.cs`:
```csharp
using Transport.Application.Abstractions;
using Transport.Application.Models;

namespace Transport.UnitTests.Fakes;

internal sealed class FakeLoginAuditQueries : ILoginAuditQueries
{
    public List<LoginAuditEntry> Entries { get; } = [];
    public (string? Username, DateTime From, DateTime To)? LastQuery { get; private set; }

    public Task<IReadOnlyList<LoginAuditEntry>> GetAsync(string? username, DateTime from, DateTime to, CancellationToken ct = default)
    {
        LastQuery = (username, from, to);
        return Task.FromResult<IReadOnlyList<LoginAuditEntry>>(Entries.ToList());
    }
}
```

- [ ] **Step 2: Viết test thất bại – `LoginAuditViewModelTests.cs`**

```csharp
using Transport.Application.Models;
using Transport.Presentation.Wpf.ViewModels.Pages;
using Transport.UnitTests.Fakes;

namespace Transport.UnitTests.Presentation;

public class LoginAuditViewModelTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 18, 10, 0, 0, TimeSpan.Zero);
    private readonly PageTestContext _context = new();
    private readonly FakeLoginAuditQueries _audit = new();

    public LoginAuditViewModelTests() => _audit.Entries.Add(new LoginAuditEntry
    {
        EventTime = new DateTime(2026, 10, 18, 9, 0, 0),
        Username = "SELLER01",
        ActionName = "LOGON",
        Result = "FAILED",
        ReturnCode = 1017,
    });

    private LoginAuditViewModel Create() => new(_context.Services, _audit, new FixedTimeProvider(Now));

    [Fact]
    public async Task Load_ShowsTheLastSevenDays()
    {
        var vm = Create();

        await vm.LoadAsync();

        Assert.Equal((string.Empty, new DateTime(2026, 10, 11), new DateTime(2026, 10, 18)), _audit.LastQuery);
        Assert.Single(vm.Entries);
    }

    [Fact]
    public async Task Search_UsesTheFilters()
    {
        var vm = Create();
        vm.UsernameFilter = "seller01";
        vm.FromDate = new DateTime(2026, 10, 1);

        await vm.SearchCommand.ExecuteAsync(null);

        Assert.Equal(("seller01", new DateTime(2026, 10, 1), new DateTime(2026, 10, 18)), _audit.LastQuery);
    }
}
```

Trong `DependencyInjectionTests.cs` đổi `12` → `13`. Trong `ViewInstantiationTests.cs` thêm `_ = new LoginAuditPage();`.

- [ ] **Step 3: Chạy test, xác nhận thất bại**

Run: `dotnet test --project tests/Transport.UnitTests --filter-class "*LoginAuditViewModelTests"`
Expected: lỗi biên dịch `LoginAuditViewModel` không tồn tại.

- [ ] **Step 4: `LoginAuditQueries.cs` (Infrastructure)**

```csharp
using Transport.Application.Abstractions;
using Transport.Application.Models;
using Transport.Infrastructure.Persistence;

namespace Transport.Infrastructure.Repositories;

internal sealed class LoginAuditQueries(OracleConnectionFactory factory) : OracleRepository(factory), ILoginAuditQueries
{
    // Giới hạn 500 dòng mới nhất để lưới không quá nặng
    public Task<IReadOnlyList<LoginAuditEntry>> GetAsync(string? username, DateTime from, DateTime to, CancellationToken ct = default) =>
        QueryAsync<LoginAuditEntry>(
            """
            SELECT event_time, username, action_name, result, return_code, userhost AS user_host, object_name
              FROM v_login_audit
             WHERE event_time >= :p_from AND event_time < :p_to
               AND (:p_username IS NULL OR username = UPPER(:p_username))
             ORDER BY event_time DESC
             FETCH FIRST 500 ROWS ONLY
            """,
            new
            {
                p_from = from.Date,
                p_to = to.Date.AddDays(1),
                p_username = string.IsNullOrWhiteSpace(username) ? null : username.Trim(),
            }, ct);
}
```

Đăng ký trong `src/Transport.Infrastructure/DependencyInjection.cs`, sau `IStatisticsQueries`:
```csharp
        services.AddSingleton<ILoginAuditQueries, LoginAuditQueries>();
```

- [ ] **Step 5: `LoginAuditViewModel.cs`**

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Transport.Application.Abstractions;
using Transport.Application.Models;

namespace Transport.Presentation.Wpf.ViewModels.Pages;

// Nhật ký đăng nhập và thao tác quản trị tài khoản (V_LOGIN_AUDIT) – chỉ điều hành
public sealed partial class LoginAuditViewModel : PageViewModel
{
    private readonly ILoginAuditQueries _audit;

    public LoginAuditViewModel(PageServices services, ILoginAuditQueries audit, TimeProvider time)
        : base(services)
    {
        _audit = audit;
        var today = time.GetLocalNow().Date;
        FromDate = today.AddDays(-7);
        ToDate = today;
        UsernameFilter = string.Empty;
    }

    public ObservableCollection<LoginAuditEntry> Entries { get; } = [];

    [ObservableProperty]
    public partial string UsernameFilter { get; set; }

    [ObservableProperty]
    public partial DateTime FromDate { get; set; }

    [ObservableProperty]
    public partial DateTime ToDate { get; set; }

    public override Task LoadAsync() => RunBusyAsync(SearchCoreAsync);

    [RelayCommand]
    private Task SearchAsync() => RunBusyAsync(SearchCoreAsync);

    [RelayCommand]
    private Task ExportToExcelAsync() => ExportAsync(Entries.ToList(),
    [
        new("Audit_Time", e => e.EventTime, "dd/MM/yyyy HH:mm:ss"),
        new("Audit_Username", e => e.Username),
        new("Audit_Action", e => e.ActionName),
        new("Audit_Result", e => Loc[$"AuditResult_{e.Result}"]),
        new("Audit_Host", e => e.UserHost),
        new("Audit_Object", e => e.ObjectName),
    ], "Audit_ExportTitle");

    private async Task SearchCoreAsync()
    {
        var rows = await _audit.GetAsync(UsernameFilter, FromDate, ToDate);
        Entries.Clear();
        foreach (var row in rows)
        {
            Entries.Add(row);
        }
    }
}
```

- [ ] **Step 6: `LoginAuditPage.xaml` + `.xaml.cs`**

```xml
<UserControl x:Class="Transport.Presentation.Wpf.Views.Pages.LoginAuditPage"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:l="clr-namespace:Transport.Presentation.Wpf.Localization"
             xmlns:c="clr-namespace:Transport.Presentation.Wpf.Controls"
             xmlns:vm="clr-namespace:Transport.Presentation.Wpf.ViewModels.Pages"
             mc:Ignorable="d" d:DesignWidth="1100" d:DesignHeight="680"
             d:DataContext="{d:DesignInstance Type=vm:LoginAuditViewModel}">
    <Border Style="{StaticResource CardStyle}">
        <DockPanel>
            <WrapPanel DockPanel.Dock="Top" Margin="0,0,0,8">
                <StackPanel Margin="0,0,12,0">
                    <TextBlock Text="{l:Loc Audit_Username}" Style="{StaticResource FieldLabelStyle}" />
                    <TextBox Width="200" Text="{Binding UsernameFilter, UpdateSourceTrigger=PropertyChanged}"
                             AutomationProperties.Name="{l:Loc Audit_Username}" />
                </StackPanel>
                <StackPanel Margin="0,0,12,0">
                    <TextBlock Text="{l:Loc Lbl_From}" Style="{StaticResource FieldLabelStyle}" />
                    <DatePicker SelectedDate="{Binding FromDate}" AutomationProperties.Name="{l:Loc Lbl_From}" />
                </StackPanel>
                <StackPanel Margin="0,0,12,0">
                    <TextBlock Text="{l:Loc Lbl_To}" Style="{StaticResource FieldLabelStyle}" />
                    <DatePicker SelectedDate="{Binding ToDate}" AutomationProperties.Name="{l:Loc Lbl_To}" />
                </StackPanel>
                <Button Content="{l:Loc Btn_Search}" Command="{Binding SearchCommand}" Style="{StaticResource PrimaryButtonStyle}"
                        VerticalAlignment="Bottom" Margin="0,0,8,0" />
                <Button Content="{l:Loc Btn_ExportExcel}" Command="{Binding ExportToExcelCommand}" c:Shortcut.Gesture="Ctrl+E"
                        VerticalAlignment="Bottom" />
            </WrapPanel>
            <DataGrid Style="{StaticResource ListGridStyle}" ItemsSource="{Binding Entries}" AutomationProperties.Name="{l:Loc Menu_LoginAudit}">
                <DataGrid.Columns>
                    <DataGridTextColumn Header="{l:Loc Audit_Time}" Binding="{Binding EventTime, StringFormat={}{0:dd/MM/yyyy HH:mm:ss}}" Width="Auto" />
                    <DataGridTextColumn Header="{l:Loc Audit_Username}" Binding="{Binding Username}" Width="*" />
                    <DataGridTextColumn Header="{l:Loc Audit_Action}" Binding="{Binding ActionName}" Width="*" />
                    <DataGridTextColumn Header="{l:Loc Audit_Result}" Width="Auto"
                                        Binding="{Binding Result, Converter={StaticResource Localize}, ConverterParameter=AuditResult_}" />
                    <DataGridTextColumn Header="{l:Loc Audit_Host}" Binding="{Binding UserHost}" Width="*" />
                    <DataGridTextColumn Header="{l:Loc Audit_Object}" Binding="{Binding ObjectName}" Width="*" />
                </DataGrid.Columns>
            </DataGrid>
        </DockPanel>
    </Border>
</UserControl>
```
Code-behind giống `RegistrationRequestsPage` (lớp `LoginAuditPage`).

- [ ] **Step 7: Đăng ký trang**

`PageTemplates.xaml`: thêm `<DataTemplate DataType="{x:Type vm:LoginAuditViewModel}"><pages:LoginAuditPage /></DataTemplate>`.
`PresentationServices.AddPages`, sau dòng `RegistrationRequestsViewModel`:
```csharp
        services.AddPage<LoginAuditViewModel>("Menu_LoginAudit", "\uE72E", 102, operatorOnly: true);
```

- [ ] **Step 8: Chuỗi giao diện**

`Strings.resx`:
```xml
  <data name="Menu_LoginAudit" xml:space="preserve"><value>Sign-in log</value></data>
  <data name="Audit_Time" xml:space="preserve"><value>Time</value></data>
  <data name="Audit_Username" xml:space="preserve"><value>Account</value></data>
  <data name="Audit_Action" xml:space="preserve"><value>Action</value></data>
  <data name="Audit_Result" xml:space="preserve"><value>Result</value></data>
  <data name="Audit_Host" xml:space="preserve"><value>Computer</value></data>
  <data name="Audit_Object" xml:space="preserve"><value>Object</value></data>
  <data name="Audit_ExportTitle" xml:space="preserve"><value>Sign-in log</value></data>
  <data name="AuditResult_SUCCESS" xml:space="preserve"><value>Success</value></data>
  <data name="AuditResult_FAILED" xml:space="preserve"><value>Failed</value></data>
```
`Strings.vi.resx`:
```xml
  <data name="Menu_LoginAudit" xml:space="preserve"><value>Nhật ký đăng nhập</value></data>
  <data name="Audit_Time" xml:space="preserve"><value>Thời gian</value></data>
  <data name="Audit_Username" xml:space="preserve"><value>Tài khoản</value></data>
  <data name="Audit_Action" xml:space="preserve"><value>Hành động</value></data>
  <data name="Audit_Result" xml:space="preserve"><value>Kết quả</value></data>
  <data name="Audit_Host" xml:space="preserve"><value>Máy</value></data>
  <data name="Audit_Object" xml:space="preserve"><value>Đối tượng</value></data>
  <data name="Audit_ExportTitle" xml:space="preserve"><value>Nhật ký đăng nhập</value></data>
  <data name="AuditResult_SUCCESS" xml:space="preserve"><value>Thành công</value></data>
  <data name="AuditResult_FAILED" xml:space="preserve"><value>Thất bại</value></data>
```

- [ ] **Step 9: Chạy test**

Run: `dotnet build TransportManagement.slnx` rồi `dotnet test --project tests/Transport.UnitTests`
Expected: 0 warning, tất cả PASS (DI: 13 trang).

- [ ] **Step 10: Commit**

```bash
node .gitnexus/run.cjs detect-changes --scope all --repo .
git add src tests
git commit -m "feat(ui): sign-in log page backed by unified auditing"
```

---

### Task 11: Tài liệu và kiểm tra cuối

**Files:**
- Modify: `README.md`, `docs/user-guide.md`, `docs/superpowers/specs/2026-10-17-transport-management-design.md`

- [ ] **Step 1: README**

- Ở phần cài đặt (quanh dòng 27–30): thêm câu "Script sẽ hỏi mật khẩu SYS và yêu cầu bạn **tự đặt mật khẩu cho chủ schema TRANSPORT** (không dùng ký tự `"` hoặc `%`). Hãy ghi nhớ mật khẩu này để chạy `backup.bat` / `restore.bat`."
- Ngay dưới bảng tài khoản demo (dòng 39–43): thêm
  > Lần đăng nhập đầu tiên, ứng dụng bắt đặt mật khẩu mới (mật khẩu trên chỉ dùng được một lần). Mật khẩu mới phải ≥ 8 ký tự, có chữ hoa, chữ thường, chữ số, ký tự đặc biệt và không chứa tên đăng nhập. Sai 5 lần liên tiếp tài khoản bị khóa 15 phút.
- Thêm mục ngắn "Bảo mật": REGISTRAR (`TRANSPORT_REGISTRAR` / `Register#Only1` trong `appsettings.json`) công khai có chủ đích vì chỉ gửi được yêu cầu đăng ký; nhật ký đăng nhập dùng Unified Auditing; kiểm tra cấu hình bằng `database/11_security_checks.sql` (chạy bằng SYS). Ghi chú: các script `.bat` truyền mật khẩu qua dòng lệnh `sqlplus`/`expdp`, tiến trình khác trên cùng máy có thể thấy – chỉ chạy trên máy cài đặt tin cậy.
- Cây thư mục: thêm `10_audit.sql`, `11_security_checks.sql`.

- [ ] **Step 2: Hướng dẫn sử dụng `docs/user-guide.md`**

Thêm 4 mục (tiếng Việt, theo giọng văn sẵn có của file), đặt sau mục Đăng nhập:
1. **Đăng ký tài khoản** – bấm "Đăng ký tài khoản" ở màn hình đăng nhập, nhập tên đăng nhập/họ tên/SĐT, chờ điều hành duyệt và nhận mật khẩu tạm trực tiếp.
2. **Đặt mật khẩu mới lần đầu / khi hết hạn** – hộp thoại tự mở; luật mật khẩu; hết hạn sau 90 ngày; không dùng lại 5 mật khẩu gần nhất.
3. **Tự đổi mật khẩu** – nút "Đổi mật khẩu" góc trên phải.
4. **(Điều hành) Duyệt yêu cầu đăng ký** và **Nhật ký đăng nhập** – thao tác Duyệt/Từ chối, lọc nhật ký, xuất Excel.
Trong bảng xử lý sự cố, thêm dòng: "Đăng nhập báo sai tên/mật khẩu hoặc bị khóa" → "Kiểm tra lại mật khẩu; nếu đã sai 5 lần, chờ 15 phút hoặc nhờ điều hành mở khóa / đặt lại mật khẩu."

- [ ] **Step 3: Spec gốc**

Trong `docs/superpowers/specs/2026-10-17-transport-management-design.md`, thay 3 chỗ `"Transport#2026"` (mục 7.1 và 8.2) bằng `"<mật khẩu do người cài nhập>"`, và thêm dưới tiêu đề mục 7:
> Bổ sung bảo mật (profile mật khẩu, đăng ký tài khoản, nhật ký đăng nhập): xem `2026-10-18-auth-security-design.md`.

Run: `git grep -n "Transport#2026"`
Expected: không còn kết quả.

- [ ] **Step 4: Kiểm tra toàn bộ**

Run: `dotnet build TransportManagement.slnx` rồi `dotnet test --project tests/Transport.UnitTests`
Expected: 0 warning, tất cả PASS.

- [ ] **Step 5: Nhờ người dùng chạy thử thật** (DB đã cài ở Task 3)

Checklist (người dùng làm, báo kết quả):
1. Đăng nhập `operator01/Operator#01` → hộp thoại "Đặt mật khẩu mới" → đặt mật khẩu hợp lệ → vào app.
2. Đăng xuất. Ở màn hình đăng nhập bấm "Đăng ký tài khoản", gửi `newuser01` → thấy thông báo đã gửi.
3. Đăng nhập operator → trang "Yêu cầu đăng ký" → Duyệt với mật khẩu tạm → đăng xuất → đăng nhập `newuser01` bằng mật khẩu tạm → bị bắt đổi → vào app.
4. Nhập sai mật khẩu 5 lần với `seller02` → thông báo luôn là câu chung; lần thứ 6 nhập đúng vẫn không vào được (bị khóa 15 phút).
5. Trang "Nhật ký đăng nhập" hiện các lần thành công/thất bại ở trên.
6. Nút "Đổi mật khẩu" đổi được mật khẩu; đăng xuất và đăng nhập lại bằng mật khẩu mới.

- [ ] **Step 6: Commit**

```bash
node .gitnexus/run.cjs detect-changes --scope all --repo .
git add README.md docs
git commit -m "docs: registration, password policy and sign-in log"
```
