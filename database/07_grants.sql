WHENEVER SQLERROR EXIT FAILURE ROLLBACK
SET DEFINE OFF
SET SQLBLANKLINES ON
SET SERVEROUTPUT ON SIZE UNLIMITED

-- Điều hành: quản lý danh mục, lịch chạy, nhân viên, sao lưu
GRANT SELECT, INSERT, UPDATE, DELETE ON routes     TO role_operator;
GRANT SELECT, INSERT, UPDATE, DELETE ON vehicles   TO role_operator;
GRANT SELECT, INSERT, UPDATE, DELETE ON drivers    TO role_operator;
GRANT SELECT, INSERT, UPDATE, DELETE ON schedules  TO role_operator;
GRANT SELECT, INSERT, UPDATE, DELETE ON passengers TO role_operator;
GRANT SELECT ON tickets           TO role_operator;
GRANT SELECT ON payments          TO role_operator;
GRANT SELECT ON staff             TO role_operator;
GRANT SELECT ON v_trip_statistics TO role_operator;
GRANT SELECT ON backup_logs       TO role_operator;
GRANT SELECT ON v_backup_job_status TO role_operator;
GRANT EXECUTE ON pkg_ticket              TO role_operator;
GRANT EXECUTE ON pkg_schedule            TO role_operator;
GRANT EXECUTE ON pkg_staff               TO role_operator;
GRANT EXECUTE ON backup_operational_data TO role_operator;
GRANT SELECT ON registration_requests  TO role_operator;
GRANT EXECUTE ON pkg_registration      TO role_operator;
GRANT SELECT ON v_login_audit          TO role_operator;

-- Bán vé: xem lịch, quản lý khách, bán/thanh toán/hủy vé qua package
GRANT SELECT ON routes            TO role_ticket_seller;
GRANT SELECT ON vehicles          TO role_ticket_seller;
GRANT SELECT ON schedules         TO role_ticket_seller;
GRANT SELECT, INSERT, UPDATE ON passengers TO role_ticket_seller;
GRANT SELECT ON tickets           TO role_ticket_seller;
GRANT SELECT ON payments          TO role_ticket_seller;
GRANT SELECT ON staff             TO role_ticket_seller;
GRANT SELECT ON v_trip_statistics TO role_ticket_seller;
GRANT EXECUTE ON pkg_ticket       TO role_ticket_seller;

-- Người chưa có tài khoản: chỉ được gửi yêu cầu đăng ký, không đọc được bảng nào
GRANT EXECUTE ON pkg_registration_public TO transport_registrar;

EXIT
