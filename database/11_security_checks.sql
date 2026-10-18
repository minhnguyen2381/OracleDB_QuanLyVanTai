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

  -- 3. REGISTRAR: không role, không quyền hệ thống nào ngoài CREATE SESSION, chỉ EXECUTE một package
  SELECT COUNT(*) INTO v_n FROM dba_role_privs WHERE grantee = 'TRANSPORT_REGISTRAR';
  ok('3a. registrar has no role', v_n = 0);
  SELECT COUNT(*) INTO v_n FROM dba_sys_privs WHERE grantee = 'TRANSPORT_REGISTRAR' AND privilege <> 'CREATE SESSION';
  ok('3b. registrar has no system privilege besides CREATE SESSION', v_n = 0);
  SELECT COUNT(*) INTO v_n FROM dba_tab_privs
   WHERE grantee = 'TRANSPORT_REGISTRAR' AND NOT (privilege = 'EXECUTE' AND table_name = 'PKG_REGISTRATION_PUBLIC');
  ok('3c. registrar can only execute PKG_REGISTRATION_PUBLIC', v_n = 0);

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
