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
