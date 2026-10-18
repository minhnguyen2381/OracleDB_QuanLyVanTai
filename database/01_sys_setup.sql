WHENEVER SQLERROR EXIT FAILURE ROLLBACK
-- Bật thay biến để nhận mật khẩu TRANSPORT từ install_all.bat (tham số &1); không in giá trị ra màn hình
SET DEFINE ON
SET VERIFY OFF
SET SQLBLANKLINES ON
SET SERVEROUTPUT ON SIZE UNLIMITED

-- Chủ schema: chỉ dùng để tạo/bảo trì object, app không đăng nhập bằng user này.
-- Mật khẩu do người cài nhập lúc chạy install_all.bat, không ghi trong mã nguồn.
CREATE USER transport IDENTIFIED BY "&1" DEFAULT TABLESPACE users QUOTA UNLIMITED ON users;
SET DEFINE OFF
GRANT CREATE SESSION, CREATE TABLE, CREATE VIEW, CREATE PROCEDURE,
      CREATE TRIGGER, CREATE SEQUENCE, CREATE JOB TO transport;
-- Quyền hệ thống cấp trực tiếp (không qua role) để PKG_STAFF dùng được trong definer rights
GRANT CREATE USER, ALTER USER, DROP USER TO transport;
GRANT SELECT ON sys.dba_scheduler_jobs TO transport WITH GRANT OPTION;
-- Cho view V_LOGIN_AUDIT đọc nhật ký Unified Auditing và cấp lại cho ROLE_OPERATOR
GRANT SELECT ON sys.unified_audit_trail TO transport WITH GRANT OPTION;

CREATE ROLE role_operator;
CREATE ROLE role_ticket_seller;
GRANT CREATE SESSION TO role_operator, role_ticket_seller;
-- Cho TRANSPORT quyền cấp/thu hồi hai role này (PKG_STAFF cần)
GRANT role_operator, role_ticket_seller TO transport WITH ADMIN OPTION;

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
GRANT EXECUTE ON staff_password_verify TO PUBLIC;

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

-- User nhận đăng ký: mật khẩu công khai có chủ đích (nằm trong appsettings.json của app),
-- chỉ được gọi PKG_REGISTRATION_PUBLIC.REGISTER_ACCOUNT (cấp ở 07_grants.sql)
CREATE USER transport_registrar IDENTIFIED BY "Register#Only1" PROFILE profile_registrar;
GRANT CREATE SESSION TO transport_registrar;

-- Thư mục cho Data Pump
CREATE OR REPLACE DIRECTORY backup_dir AS 'D:\OracleBackup';
GRANT READ, WRITE ON DIRECTORY backup_dir TO transport;

EXIT
