-- Gỡ toàn bộ CSDL vận tải để cài lại sạch (chạy bằng SYS trong XEPDB1).
-- Lỗi "không tồn tại" được bỏ qua để chạy được cả khi mới cài một phần.
WHENEVER SQLERROR CONTINUE
SET DEFINE OFF
SET SERVEROUTPUT ON SIZE UNLIMITED

-- Tắt rồi xóa audit policy trước khi xóa schema (policy tham chiếu package của TRANSPORT)
NOAUDIT POLICY pol_transport_logon;
NOAUDIT POLICY pol_transport_account_admin;
DROP AUDIT POLICY pol_transport_logon;
DROP AUDIT POLICY pol_transport_account_admin;

-- Xóa các user nhân viên có trong TRANSPORT.STAFF (dùng SQL động vì bảng có thể chưa tồn tại)
DECLARE
  TYPE t_names IS TABLE OF VARCHAR2(30);
  v_users t_names;
BEGIN
  EXECUTE IMMEDIATE 'SELECT username FROM transport.staff' BULK COLLECT INTO v_users;
  FOR i IN 1 .. v_users.COUNT LOOP
    BEGIN
      EXECUTE IMMEDIATE 'DROP USER ' || v_users(i) || ' CASCADE';
    EXCEPTION WHEN OTHERS THEN NULL;
    END;
  END LOOP;
EXCEPTION WHEN OTHERS THEN NULL;
END;
/

-- Phòng trường hợp bảng STAFF đã mất nhưng user demo/test vẫn còn
BEGIN
  FOR u IN (SELECT username FROM dba_users
             WHERE username IN ('OPERATOR01', 'SELLER01', 'SELLER02', 'TEST_STAFF_01', 'TEST_REG_01')) LOOP
    EXECUTE IMMEDIATE 'DROP USER ' || u.username || ' CASCADE';
  END LOOP;
END;
/

DROP USER transport CASCADE;
DROP USER transport_registrar CASCADE;
DROP ROLE role_operator;
DROP ROLE role_ticket_seller;
DROP DIRECTORY backup_dir;

-- Profile CASCADE chuyển user còn lại về DEFAULT; xóa hàm sau khi profile không còn dùng nó
DROP PROFILE profile_staff CASCADE;
DROP PROFILE profile_registrar CASCADE;
DROP FUNCTION staff_password_verify;

EXIT
