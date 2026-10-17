WHENEVER SQLERROR EXIT FAILURE ROLLBACK
SET DEFINE OFF
SET SQLBLANKLINES ON
SET SERVEROUTPUT ON SIZE UNLIMITED

-- Chủ schema: chỉ dùng để tạo/bảo trì object, app không đăng nhập bằng user này
CREATE USER transport IDENTIFIED BY "Transport#2026" DEFAULT TABLESPACE users QUOTA UNLIMITED ON users;
GRANT CREATE SESSION, CREATE TABLE, CREATE VIEW, CREATE PROCEDURE,
      CREATE TRIGGER, CREATE SEQUENCE, CREATE JOB TO transport;
-- Quyền hệ thống cấp trực tiếp (không qua role) để PKG_STAFF dùng được trong definer rights
GRANT CREATE USER, ALTER USER, DROP USER TO transport;
GRANT SELECT ON sys.dba_scheduler_jobs TO transport WITH GRANT OPTION;

CREATE ROLE role_operator;
CREATE ROLE role_ticket_seller;
GRANT CREATE SESSION TO role_operator, role_ticket_seller;
-- Cho TRANSPORT quyền cấp/thu hồi hai role này (PKG_STAFF cần)
GRANT role_operator, role_ticket_seller TO transport WITH ADMIN OPTION;

-- Thư mục cho Data Pump
CREATE OR REPLACE DIRECTORY backup_dir AS 'D:\OracleBackup';
GRANT READ, WRITE ON DIRECTORY backup_dir TO transport;

EXIT
