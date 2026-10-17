-- Kịch bản kiểm thử nghiệp vụ (chạy bằng TRANSPORT). In PASS/FAIL từng kịch bản.
-- Tự tạo dữ liệu test riêng (biển số 99Z-999.99, SĐT 0999xxxxxx) và tự dọn ở cuối.
WHENEVER SQLERROR EXIT FAILURE ROLLBACK
SET DEFINE OFF
SET SQLBLANKLINES ON
SET SERVEROUTPUT ON SIZE UNLIMITED

DECLARE
  g_fail    PLS_INTEGER := 0;
  v_route   NUMBER;
  v_vehicle NUMBER;
  v_driver  NUMBER;
  v_sched   NUMBER;
  v_t1      NUMBER;
  v_t2      NUMBER;
  v_n       NUMBER;
  v_m       NUMBER;
  v_log     NUMBER;

  -- Ghi nhận kết quả một kiểm tra
  PROCEDURE ok(p_name VARCHAR2, p_cond BOOLEAN) IS
  BEGIN
    IF p_cond THEN
      DBMS_OUTPUT.PUT_LINE('PASS  ' || p_name);
    ELSE
      g_fail := g_fail + 1;
      DBMS_OUTPUT.PUT_LINE('FAIL  ' || p_name);
    END IF;
  END ok;

  -- Chạy một khối PL/SQL động và kiểm tra đúng mã lỗi mong đợi
  PROCEDURE expect_error(p_name VARCHAR2, p_code NUMBER, p_block VARCHAR2) IS
  BEGIN
    EXECUTE IMMEDIATE p_block;
    ok(p_name || ' (expected ' || p_code || ', got no error)', FALSE);
  EXCEPTION
    WHEN OTHERS THEN
      ok(p_name || ' (expected ' || p_code || ', got ' || SQLCODE || ')', SQLCODE = p_code);
  END expect_error;

  -- Dọn toàn bộ dữ liệu test (gọi cả ở đầu để chạy lại được sau một lần lỗi)
  PROCEDURE cleanup IS
  BEGIN
    DELETE FROM staff WHERE username = 'TEST_STAFF_01';
    BEGIN EXECUTE IMMEDIATE 'DROP USER test_staff_01'; EXCEPTION WHEN OTHERS THEN NULL; END;
    DELETE FROM payments WHERE ticket_id IN (
      SELECT t.ticket_id FROM tickets t JOIN schedules s ON s.schedule_id = t.schedule_id
        JOIN vehicles v ON v.vehicle_id = s.vehicle_id WHERE v.plate_number = '99Z-999.99');
    DELETE FROM tickets WHERE schedule_id IN (
      SELECT s.schedule_id FROM schedules s JOIN vehicles v ON v.vehicle_id = s.vehicle_id
       WHERE v.plate_number = '99Z-999.99');
    DELETE FROM schedules WHERE vehicle_id IN (SELECT vehicle_id FROM vehicles WHERE plate_number = '99Z-999.99');
    DELETE FROM passengers WHERE phone LIKE '0999%';
    DELETE FROM vehicles WHERE plate_number = '99Z-999.99';
    DELETE FROM drivers  WHERE license_no = 'TEST-LIC-1';
    DELETE FROM routes   WHERE route_name = 'TEST Route';
    COMMIT;
  END cleanup;
BEGIN
  cleanup;

  -- Dữ liệu test: xe 16 chỗ, chuyến khởi hành sau 2 ngày
  INSERT INTO routes (route_name, origin, destination, distance_km, duration_min, base_price)
  VALUES ('TEST Route', 'TEST A', 'TEST B', 10, 60, 100000) RETURNING route_id INTO v_route;
  INSERT INTO vehicles (plate_number, vehicle_type, seat_count, status)
  VALUES ('99Z-999.99', 'Test 16', 16, 'ACTIVE') RETURNING vehicle_id INTO v_vehicle;
  INSERT INTO drivers (full_name, id_card, phone, license_no, license_class, license_expiry, status)
  VALUES ('Test Driver', '999999999999', '0999999999', 'TEST-LIC-1', 'D', TRUNC(SYSDATE) + 365, 'ACTIVE')
  RETURNING driver_id INTO v_driver;
  INSERT INTO schedules (route_id, vehicle_id, driver_id, departure_time, arrival_time, ticket_price)
  VALUES (v_route, v_vehicle, v_driver, TRUNC(SYSDATE) + 2 + 8/24, TRUNC(SYSDATE) + 2 + 9/24, 100000)
  RETURNING schedule_id INTO v_sched;
  COMMIT;

  -- 1. Đặt vé hợp lệ
  pkg_ticket.book_ticket(v_sched, 'Test Passenger', '0999000001', NULL, 1, v_t1);
  SELECT COUNT(*) INTO v_n FROM tickets WHERE ticket_id = v_t1 AND status = 'BOOKED' AND sold_by = USER;
  ok('1. book a free seat', v_n = 1);

  -- 2. Đặt trùng ghế
  expect_error('2. double booking is rejected', -20003,
    'DECLARE x NUMBER; BEGIN pkg_ticket.book_ticket(' || v_sched || ', ''P2'', ''0999000002'', NULL, 1, x); END;');

  -- 3. Ghế vượt sức chứa
  expect_error('3. seat beyond capacity is rejected', -20001,
    'DECLARE x NUMBER; BEGIN pkg_ticket.book_ticket(' || v_sched || ', ''P3'', ''0999000003'', NULL, 17, x); END;');

  -- 4. Thanh toán
  pkg_ticket.pay_ticket(v_t1, 'CASH');
  SELECT COUNT(*) INTO v_n FROM payments WHERE ticket_id = v_t1 AND payment_type = 'PAYMENT';
  ok('4a. payment row created', v_n = 1);
  SELECT COUNT(*) INTO v_n FROM tickets WHERE ticket_id = v_t1 AND status = 'PAID';
  ok('4b. ticket is PAID', v_n = 1);
  expect_error('4c. paying twice is rejected', -20020,
    'BEGIN pkg_ticket.pay_ticket(' || v_t1 || ', ''CASH''); END;');

  -- 5. Hủy vé đã thanh toán -> hoàn tiền, doanh thu ròng = 0
  pkg_ticket.cancel_ticket(v_t1, 'test cancel');
  SELECT COUNT(*) INTO v_n FROM payments WHERE ticket_id = v_t1 AND payment_type = 'REFUND';
  ok('5a. refund row created', v_n = 1);
  SELECT revenue INTO v_n FROM v_trip_statistics WHERE schedule_id = v_sched;
  ok('5b. net revenue is 0 after refund', v_n = 0);

  -- 6. Hủy lần 2
  expect_error('6. cancelling twice is rejected', -20030,
    'BEGIN pkg_ticket.cancel_ticket(' || v_t1 || ', ''again''); END;');

  -- 7. Ghế đã hủy được đặt lại
  pkg_ticket.book_ticket(v_sched, 'Test Passenger', '0999000001', NULL, 1, v_t2);
  ok('7. cancelled seat can be booked again', v_t2 IS NOT NULL AND v_t2 <> v_t1);

  -- 8. Hủy chuyến: không cho UPDATE trực tiếp khi còn vé; cancel_schedule hủy vé + hoàn tiền
  pkg_ticket.pay_ticket(v_t2, 'CARD');
  expect_error('8a. direct schedule cancel is blocked', -20052,
    'UPDATE schedules SET status = ''CANCELLED'' WHERE schedule_id = ' || v_sched);
  pkg_schedule.cancel_schedule(v_sched, 'test');
  SELECT COUNT(*) INTO v_n FROM tickets WHERE schedule_id = v_sched AND status <> 'CANCELLED';
  ok('8b. all tickets cancelled', v_n = 0);
  SELECT COUNT(*) INTO v_n FROM payments WHERE ticket_id = v_t2 AND payment_type = 'REFUND';
  ok('8c. paid ticket refunded', v_n = 1);
  SELECT COUNT(*) INTO v_n FROM schedules WHERE schedule_id = v_sched AND status = 'CANCELLED';
  ok('8d. schedule is CANCELLED', v_n = 1);
  expect_error('8e. cancelling a cancelled schedule is rejected', -20051,
    'BEGIN pkg_schedule.cancel_schedule(' || v_sched || ', ''again''); END;');

  -- 9. Sao lưu: số dòng bản sao lưu mới nhất khớp bảng gốc
  backup_operational_data;
  SELECT MAX(log_id) INTO v_log FROM backup_logs;
  SELECT COUNT(*) INTO v_n FROM tickets_bk WHERE backup_id = v_log;
  SELECT COUNT(*) INTO v_m FROM tickets;
  ok('9a. tickets snapshot matches', v_n = v_m);
  SELECT COUNT(*) INTO v_n FROM schedules_bk WHERE backup_id = v_log;
  SELECT COUNT(*) INTO v_m FROM schedules;
  ok('9b. schedules snapshot matches', v_n = v_m);
  SELECT COUNT(*) INTO v_n FROM payments_bk WHERE backup_id = v_log;
  SELECT COUNT(*) INTO v_m FROM payments;
  ok('9c. payments snapshot matches', v_n = v_m);
  SELECT ticket_rows INTO v_n FROM backup_logs WHERE log_id = v_log;
  SELECT COUNT(*) INTO v_m FROM tickets;
  ok('9d. backup log row counts recorded', v_n = v_m);
  SELECT COUNT(*) INTO v_n FROM v_backup_job_status WHERE enabled = 'TRUE';
  ok('9e. nightly job exists and is enabled', v_n = 1);

  -- 10. PKG_STAFF chặn dữ liệu sai
  expect_error('10a. invalid username is rejected', -20040,
    'BEGIN pkg_staff.create_staff(''x; drop'', ''Password1'', ''X'', NULL, ''ROLE_OPERATOR''); END;');
  expect_error('10b. invalid role is rejected', -20041,
    'BEGIN pkg_staff.create_staff(''test_staff_01'', ''Password1'', ''X'', NULL, ''ROLE_ADMIN''); END;');
  expect_error('10c. short password is rejected', -20044,
    'BEGIN pkg_staff.create_staff(''test_staff_01'', ''short'', ''X'', NULL, ''ROLE_OPERATOR''); END;');
  expect_error('10d. unknown staff is rejected', -20042,
    'BEGIN pkg_staff.set_locked(''nobody_xyz'', 1); END;');

  -- 11. Vòng đời nhân viên: tạo, đổi role, khóa
  pkg_staff.create_staff('test_staff_01', 'TestPass#1', 'Test Staff', NULL, 'ROLE_TICKET_SELLER');
  SELECT COUNT(*) INTO v_n FROM all_users WHERE username = 'TEST_STAFF_01';
  ok('11a. Oracle user created', v_n = 1);
  pkg_staff.change_role('test_staff_01', 'ROLE_OPERATOR');
  SELECT COUNT(*) INTO v_n FROM staff WHERE username = 'TEST_STAFF_01' AND role_name = 'ROLE_OPERATOR';
  ok('11b. role changed', v_n = 1);
  pkg_staff.set_locked('test_staff_01', 1);
  SELECT COUNT(*) INTO v_n FROM staff WHERE username = 'TEST_STAFF_01' AND status = 'LOCKED';
  ok('11c. account locked', v_n = 1);
  expect_error('11d. duplicate username is rejected', -1920,
    'BEGIN pkg_staff.create_staff(''test_staff_01'', ''TestPass#1'', ''X'', NULL, ''ROLE_OPERATOR''); END;');
  -- Dọn user test
  DELETE FROM staff WHERE username = 'TEST_STAFF_01';
  COMMIT;
  EXECUTE IMMEDIATE 'DROP USER test_staff_01';

  cleanup;
  DBMS_OUTPUT.PUT_LINE(CASE WHEN g_fail = 0 THEN 'ALL TESTS PASSED'
                            ELSE g_fail || ' TEST(S) FAILED' END);
END;
/

-- ===== Kiểm tra phân quyền thủ công (không tự chạy) =====
-- Đăng nhập seller01/Seller#001 rồi chạy:
--   INSERT INTO transport.tickets (schedule_id, passenger_id, seat_no, price) VALUES (1, 1, 1, 1);
--     -> ORA-01031: insufficient privileges (chỉ bán vé được qua PKG_TICKET)
--   EXEC transport.pkg_staff.set_locked('SELLER02', 1)
--     -> PLS-00201: identifier 'TRANSPORT.PKG_STAFF' must be declared (seller không có quyền EXECUTE)
--   SELECT COUNT(*) FROM transport.drivers;
--     -> ORA-00942: table or view does not exist (seller không được xem tài xế)

EXIT
