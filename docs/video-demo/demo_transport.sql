-- Video demo: worksheet chính, kết nối TRANSPORT (chủ schema).
-- Bôi đen từng khối rồi Ctrl+Enter. Autocommit của SQL Developer phải TẮT.
-- Trước khi quay: chạy khối [0], chọn một chuyến khởi hành ngày mai, sửa số ở DEFINE rồi chạy dòng DEFINE.
-- Hành khách demo dùng SĐT 0988000001 (bán trên app ở Demo 1); mọi dòng demo gắn với SĐT 098800000x để dọn ở khối [9].

DEFINE sid = 0

-- [0] Các chuyến còn mở bán
SELECT schedule_id, route_name, departure_time, seat_count, seats_sold, seats_available
  FROM v_trip_statistics
 WHERE schedule_status = 'SCHEDULED' AND departure_time > SYSDATE
 ORDER BY departure_time;


-- ===== DEMO 1: bán vé qua package, trigger chặn khi bỏ qua package =====

-- [1a] Vé vừa bán trên app: sold_by là user Oracle của nhân viên bán vé
SELECT t.ticket_id, t.seat_no, t.status, t.price, t.sold_by, p.full_name, p.phone
  FROM tickets t JOIN passengers p ON p.passenger_id = t.passenger_id
 WHERE t.schedule_id = &sid
 ORDER BY t.ticket_id DESC FETCH FIRST 3 ROWS ONLY;

SELECT payment_id, ticket_id, amount, method, payment_type, received_by
  FROM payments
 WHERE ticket_id IN (SELECT ticket_id FROM tickets WHERE schedule_id = &sid AND seat_no = 10);

-- [1b] Chèn thẳng vào bảng ghế 10 đã bán -> ORA-20003
INSERT INTO tickets (schedule_id, passenger_id, seat_no, price)
VALUES (&sid, (SELECT passenger_id FROM passengers WHERE phone = '0988000001'), 10, 100000);

-- [1c] Ghế 99 không có trên xe -> ORA-20001
INSERT INTO tickets (schedule_id, passenger_id, seat_no, price)
VALUES (&sid, (SELECT passenger_id FROM passengers WHERE phone = '0988000001'), 99, 100000);


-- ===== DEMO 2a: KHÔNG khoá chuyến -> hai phiên cùng bán được ghế 12 =====
-- Phiên A là worksheet này; phiên B mở bằng Ctrl+Shift+N (unshared worksheet, cùng kết nối TRANSPORT).

-- [2a-A1] Phiên A: chèn ghế 12, CHƯA commit
INSERT INTO tickets (schedule_id, passenger_id, seat_no, price)
VALUES (&sid, (SELECT passenger_id FROM passengers WHERE phone = '0988000001'), 12, 100000);

-- [2a-B1] Dán sang phiên B (sau khi chạy DEFINE sid ở phiên B): cũng chèn ghế 12 -> vẫn "1 row inserted"
-- INSERT INTO tickets (schedule_id, passenger_id, seat_no, price)
-- VALUES (&sid, (SELECT passenger_id FROM passengers WHERE phone = '0988000001'), 12, 100000);
-- COMMIT;

-- [2a-A2] Phiên A commit, rồi kiểm tra: ghế 12 có hai vé hiệu lực
COMMIT;

SELECT seat_no, COUNT(*) AS valid_tickets
  FROM tickets
 WHERE schedule_id = &sid AND status <> 'CANCELLED'
 GROUP BY seat_no HAVING COUNT(*) > 1;

-- [2a-A3] Dọn hai vé trùng trước khi sang 2b
DELETE FROM tickets WHERE schedule_id = &sid AND seat_no = 12;
COMMIT;


-- ===== DEMO 2b: book_ticket khoá dòng chuyến -> các quầy xếp hàng =====

-- [2b-A1] Phiên A giữ khoá dòng chuyến, đúng câu lệnh đầu tiên của book_ticket
SELECT schedule_id, ticket_price, departure_time
  FROM schedules WHERE schedule_id = &sid FOR UPDATE;

-- (Chuyển sang worksheet seller01 chạy [2b-B1]: phiên đó sẽ treo. Xem worksheet SYS khối [2b-S1].)

-- [2b-A2] Phiên A bán ghế 11 rồi commit -> nhả khoá; phiên seller01 chạy tiếp và nhận ORA-20003
INSERT INTO tickets (schedule_id, passenger_id, seat_no, price)
VALUES (&sid, (SELECT passenger_id FROM passengers WHERE phone = '0988000001'), 11, 100000);
COMMIT;

-- [2b-A3] Ghế 11 chỉ có một vé
SELECT ticket_id, seat_no, status, sold_by
  FROM tickets WHERE schedule_id = &sid AND seat_no = 11;


-- ===== DEMO 3: kiểm toán đăng nhập (sau khi đăng nhập sai một lần trên app) =====

-- [3a] 1017 = sai mật khẩu
SELECT event_time, username, result, return_code
  FROM v_login_audit
 WHERE action_name = 'LOGON'
 ORDER BY event_time DESC FETCH FIRST 5 ROWS ONLY;


-- ===== DEMO 4: view thống kê và kế hoạch thực thi =====

-- [4a] Thống kê theo chuyến (màn Thống kê của app đọc view này)
SELECT schedule_id, route_name, departure_time, seats_sold, seats_available,
       occupancy_pct, tickets_cancelled, revenue
  FROM v_trip_statistics
 ORDER BY departure_time DESC FETCH FIRST 10 ROWS ONLY;

-- [4b] Câu truy vấn sơ đồ ghế của app -> INDEX RANGE SCAN trên IDX_TICKETS_SCHEDULE
EXPLAIN PLAN FOR
SELECT seat_no FROM tickets WHERE schedule_id = &sid AND status <> 'CANCELLED';

SELECT plan_table_output FROM TABLE(DBMS_XPLAN.DISPLAY(NULL, NULL, 'BASIC +ROWS +COST'));


-- ===== DEMO 5: sao lưu =====

-- [5a] Job chạy lúc 2 giờ sáng hằng ngày
SELECT job_name, enabled, state, last_start_date, next_run_date, failure_count
  FROM v_backup_job_status;

-- [5b] Chạy tay một lần
EXEC backup_operational_data

-- [5c] Nhật ký sao lưu, dòng mới nhất do lần chạy tay vừa rồi
SELECT log_id, backup_at, performed_by, schedule_rows, ticket_rows, payment_rows
  FROM backup_logs ORDER BY log_id DESC FETCH FIRST 3 ROWS ONLY;

-- [5d] Bản chụp mới nhất khớp số vé đang có
SELECT (SELECT COUNT(*) FROM tickets) AS live_tickets,
       (SELECT COUNT(*) FROM tickets_bk
         WHERE backup_id = (SELECT MAX(log_id) FROM backup_logs)) AS snapshot_tickets
  FROM dual;


-- ===== [9] Dọn dữ liệu demo để quay lại (không cần nếu cài lại bằng install_all.bat) =====
DELETE FROM payments
 WHERE ticket_id IN (SELECT t.ticket_id FROM tickets t
                       JOIN passengers p ON p.passenger_id = t.passenger_id
                      WHERE p.phone LIKE '098800000_');
DELETE FROM tickets
 WHERE passenger_id IN (SELECT passenger_id FROM passengers WHERE phone LIKE '098800000_');
DELETE FROM passengers WHERE phone LIKE '098800000_';
COMMIT;
