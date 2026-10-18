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
