-- Video demo: worksheet kết nối seller01 (ROLE_TICKET_SELLER).
-- Chạy dòng DEFINE với cùng mã chuyến như worksheet TRANSPORT.

DEFINE sid = 0
SET SERVEROUTPUT ON

-- ===== DEMO 2b: quầy thứ hai đặt ghế 11 trong lúc phiên A đang giữ khoá chuyến =====

-- [2b-B1] Câu này treo cho tới khi phiên A commit, sau đó báo ORA-20003
DECLARE
  v_ticket_id NUMBER;
BEGIN
  transport.pkg_ticket.book_ticket(&sid, 'Khách quầy 2', '0988000002', NULL, 11, v_ticket_id);
  DBMS_OUTPUT.PUT_LINE('Booked ticket ' || v_ticket_id);
END;
/


-- ===== DEMO 3: nhân viên bán vé chỉ đi qua đúng các cửa được cấp =====

-- [3a] Role đang có trong phiên
SELECT role FROM session_roles;

-- [3b] Không được xem tài xế -> ORA-00942
SELECT COUNT(*) FROM transport.drivers;

-- [3c] Được SELECT vé nhưng không được INSERT thẳng -> ORA-01031
INSERT INTO transport.tickets (schedule_id, passenger_id, seat_no, price) VALUES (&sid, 1, 20, 100000);

-- [3d] Không có quyền EXECUTE package quản trị nhân viên -> PLS-00201
EXEC transport.pkg_staff.set_locked('SELLER02', 1)

-- [3e] Cửa được mở: đọc view thống kê
SELECT COUNT(*) AS schedules_visible FROM transport.v_trip_statistics;
