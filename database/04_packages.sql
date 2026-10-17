WHENEVER SQLERROR EXIT FAILURE ROLLBACK
SET DEFINE OFF
SET SQLBLANKLINES ON
SET SERVEROUTPUT ON SIZE UNLIMITED

CREATE OR REPLACE PACKAGE pkg_ticket AS
  -- Đặt vé: tìm hành khách theo SĐT, chưa có thì tạo mới
  PROCEDURE book_ticket(p_schedule_id IN NUMBER, p_full_name IN VARCHAR2, p_phone IN VARCHAR2,
                        p_id_card IN VARCHAR2, p_seat_no IN NUMBER, o_ticket_id OUT NUMBER);
  -- Thanh toán vé đang ở trạng thái BOOKED
  PROCEDURE pay_ticket(p_ticket_id IN NUMBER, p_method IN VARCHAR2);
  -- Hủy vé; vé đã thanh toán thì ghi thêm dòng hoàn tiền 100%
  PROCEDURE cancel_ticket(p_ticket_id IN NUMBER, p_reason IN VARCHAR2);
END pkg_ticket;
/

CREATE OR REPLACE PACKAGE BODY pkg_ticket AS

  PROCEDURE book_ticket(p_schedule_id IN NUMBER, p_full_name IN VARCHAR2, p_phone IN VARCHAR2,
                        p_id_card IN VARCHAR2, p_seat_no IN NUMBER, o_ticket_id OUT NUMBER) IS
    v_price     schedules.ticket_price%TYPE;
    v_departure schedules.departure_time%TYPE;
    v_pass_id   passengers.passenger_id%TYPE;
  BEGIN
    -- Khóa dòng lịch chạy để các quầy bán cùng chuyến xếp hàng, không tranh ghế
    SELECT ticket_price, departure_time INTO v_price, v_departure
      FROM schedules WHERE schedule_id = p_schedule_id FOR UPDATE;

    IF v_departure <= SYSDATE THEN
      RAISE_APPLICATION_ERROR(-20010, 'Schedule has already departed; booking not allowed.');
    END IF;

    -- Tìm hoặc tạo hành khách theo số điện thoại
    MERGE INTO passengers p
    USING (SELECT p_phone AS phone FROM dual) x ON (p.phone = x.phone)
    WHEN MATCHED THEN UPDATE SET p.full_name = p_full_name, p.id_card = NVL(p_id_card, p.id_card)
    WHEN NOT MATCHED THEN INSERT (full_name, phone, id_card) VALUES (p_full_name, p_phone, p_id_card);
    SELECT passenger_id INTO v_pass_id FROM passengers WHERE phone = p_phone;

    -- Trigger trg_tickets_check_seat kiểm tra ghế
    INSERT INTO tickets (schedule_id, passenger_id, seat_no, price)
    VALUES (p_schedule_id, v_pass_id, p_seat_no, v_price)
    RETURNING ticket_id INTO o_ticket_id;
    COMMIT;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN ROLLBACK; RAISE_APPLICATION_ERROR(-20011, 'Schedule not found.');
    WHEN OTHERS THEN ROLLBACK; RAISE;
  END book_ticket;

  PROCEDURE pay_ticket(p_ticket_id IN NUMBER, p_method IN VARCHAR2) IS
    v_status tickets.status%TYPE;
    v_price  tickets.price%TYPE;
  BEGIN
    SELECT status, price INTO v_status, v_price FROM tickets WHERE ticket_id = p_ticket_id FOR UPDATE;
    IF v_status <> 'BOOKED' THEN
      RAISE_APPLICATION_ERROR(-20020, 'Only BOOKED tickets can be paid.');
    END IF;
    INSERT INTO payments (ticket_id, amount, method, payment_type)
    VALUES (p_ticket_id, v_price, p_method, 'PAYMENT');
    UPDATE tickets SET status = 'PAID' WHERE ticket_id = p_ticket_id;
    COMMIT;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN ROLLBACK; RAISE_APPLICATION_ERROR(-20021, 'Ticket not found.');
    WHEN OTHERS THEN ROLLBACK; RAISE;
  END pay_ticket;

  PROCEDURE cancel_ticket(p_ticket_id IN NUMBER, p_reason IN VARCHAR2) IS
    v_status    tickets.status%TYPE;
    v_price     tickets.price%TYPE;
    v_departure schedules.departure_time%TYPE;
  BEGIN
    SELECT t.status, t.price, s.departure_time INTO v_status, v_price, v_departure
      FROM tickets t JOIN schedules s ON s.schedule_id = t.schedule_id
     WHERE t.ticket_id = p_ticket_id
       FOR UPDATE OF t.status;

    IF v_status = 'CANCELLED' THEN
      RAISE_APPLICATION_ERROR(-20030, 'Ticket is already cancelled.');
    ELSIF v_departure <= SYSDATE THEN
      RAISE_APPLICATION_ERROR(-20031, 'Schedule has already departed; cancellation not allowed.');
    END IF;

    -- Vé đã thanh toán thì hoàn lại toàn bộ tiền mặt
    IF v_status = 'PAID' THEN
      INSERT INTO payments (ticket_id, amount, method, payment_type)
      VALUES (p_ticket_id, v_price, 'CASH', 'REFUND');
    END IF;

    UPDATE tickets
       SET status = 'CANCELLED', cancelled_at = SYSTIMESTAMP, cancel_reason = p_reason
     WHERE ticket_id = p_ticket_id;
    COMMIT;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN ROLLBACK; RAISE_APPLICATION_ERROR(-20032, 'Ticket not found.');
    WHEN OTHERS THEN ROLLBACK; RAISE;
  END cancel_ticket;

END pkg_ticket;
/

CREATE OR REPLACE PACKAGE pkg_schedule AS
  -- Hủy chuyến: hủy toàn bộ vé còn hiệu lực, hoàn tiền vé đã thanh toán, rồi hủy chuyến
  PROCEDURE cancel_schedule(p_schedule_id IN NUMBER, p_reason IN VARCHAR2);
END pkg_schedule;
/

CREATE OR REPLACE PACKAGE BODY pkg_schedule AS

  PROCEDURE cancel_schedule(p_schedule_id IN NUMBER, p_reason IN VARCHAR2) IS
    v_status schedules.status%TYPE;
  BEGIN
    SELECT status INTO v_status FROM schedules WHERE schedule_id = p_schedule_id FOR UPDATE;
    IF v_status <> 'SCHEDULED' THEN
      RAISE_APPLICATION_ERROR(-20051, 'Only SCHEDULED trips can be cancelled.');
    END IF;

    -- Hoàn tiền các vé đã thanh toán
    INSERT INTO payments (ticket_id, amount, method, payment_type)
    SELECT ticket_id, price, 'CASH', 'REFUND'
      FROM tickets WHERE schedule_id = p_schedule_id AND status = 'PAID';

    UPDATE tickets
       SET status = 'CANCELLED', cancelled_at = SYSTIMESTAMP,
           cancel_reason = SUBSTR('Trip cancelled: ' || p_reason, 1, 200)
     WHERE schedule_id = p_schedule_id AND status <> 'CANCELLED';

    -- Lúc này không còn vé hiệu lực nên trg_schedules_check_cancel cho qua
    UPDATE schedules SET status = 'CANCELLED' WHERE schedule_id = p_schedule_id;
    COMMIT;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN ROLLBACK; RAISE_APPLICATION_ERROR(-20050, 'Schedule not found.');
    WHEN OTHERS THEN ROLLBACK; RAISE;
  END cancel_schedule;

END pkg_schedule;
/

CREATE OR REPLACE PACKAGE pkg_staff AS
  -- Tạo user Oracle, cấp role, ghi vào STAFF
  PROCEDURE create_staff(p_username IN VARCHAR2, p_password IN VARCHAR2, p_full_name IN VARCHAR2,
                         p_phone IN VARCHAR2, p_role IN VARCHAR2);
  -- Sửa họ tên, SĐT
  PROCEDURE update_staff_info(p_username IN VARCHAR2, p_full_name IN VARCHAR2, p_phone IN VARCHAR2);
  -- Đổi role (thu role cũ, cấp role mới)
  PROCEDURE change_role(p_username IN VARCHAR2, p_role IN VARCHAR2);
  -- Đặt lại mật khẩu
  PROCEDURE reset_password(p_username IN VARCHAR2, p_password IN VARCHAR2);
  -- Khóa (p_locked = 1) hoặc mở khóa (p_locked = 0) tài khoản
  PROCEDURE set_locked(p_username IN VARCHAR2, p_locked IN NUMBER);
END pkg_staff;
/

CREATE OR REPLACE PACKAGE BODY pkg_staff AS

  -- Username: bắt đầu bằng chữ, 3-30 ký tự chữ/số/gạch dưới (chặn SQL injection trong EXECUTE IMMEDIATE)
  FUNCTION valid_username(p_username IN VARCHAR2) RETURN VARCHAR2 IS
  BEGIN
    IF p_username IS NULL OR NOT REGEXP_LIKE(p_username, '^[A-Za-z][A-Za-z0-9_]{2,29}$') THEN
      RAISE_APPLICATION_ERROR(-20040, 'Invalid username.');
    END IF;
    RETURN UPPER(p_username);
  END valid_username;

  PROCEDURE check_role(p_role IN VARCHAR2) IS
  BEGIN
    IF p_role IS NULL OR p_role NOT IN ('ROLE_OPERATOR', 'ROLE_TICKET_SELLER') THEN
      RAISE_APPLICATION_ERROR(-20041, 'Invalid role.');
    END IF;
  END check_role;

  -- Mật khẩu tối thiểu 8 ký tự, không chứa dấu nháy kép (vì được bọc trong "...")
  PROCEDURE check_password(p_password IN VARCHAR2) IS
  BEGIN
    IF p_password IS NULL OR LENGTH(p_password) < 8 OR INSTR(p_password, '"') > 0 THEN
      RAISE_APPLICATION_ERROR(-20044, 'Password must be at least 8 characters and must not contain double quotes.');
    END IF;
  END check_password;

  -- Chỉ được thao tác trên user có trong STAFF; trả về role hiện tại
  FUNCTION managed_role(p_user IN VARCHAR2) RETURN VARCHAR2 IS
    v_role staff.role_name%TYPE;
  BEGIN
    SELECT role_name INTO v_role FROM staff WHERE username = p_user;
    RETURN v_role;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN RAISE_APPLICATION_ERROR(-20042, 'Staff not found.');
  END managed_role;

  PROCEDURE not_self(p_user IN VARCHAR2) IS
  BEGIN
    IF p_user = USER THEN
      RAISE_APPLICATION_ERROR(-20043, 'You cannot lock or change your own account.');
    END IF;
  END not_self;

  PROCEDURE create_staff(p_username IN VARCHAR2, p_password IN VARCHAR2, p_full_name IN VARCHAR2,
                         p_phone IN VARCHAR2, p_role IN VARCHAR2) IS
    v_user VARCHAR2(30) := valid_username(p_username);
  BEGIN
    check_role(p_role);
    check_password(p_password);
    EXECUTE IMMEDIATE 'CREATE USER ' || v_user || ' IDENTIFIED BY "' || p_password || '"';
    BEGIN
      EXECUTE IMMEDIATE 'GRANT ' || p_role || ' TO ' || v_user;
      INSERT INTO staff (username, full_name, phone, role_name) VALUES (v_user, p_full_name, p_phone, p_role);
      COMMIT;
    EXCEPTION
      WHEN OTHERS THEN
        -- Dọn user vừa tạo để không để lại tài khoản mồ côi
        ROLLBACK;
        EXECUTE IMMEDIATE 'DROP USER ' || v_user;
        RAISE;
    END;
  END create_staff;

  PROCEDURE update_staff_info(p_username IN VARCHAR2, p_full_name IN VARCHAR2, p_phone IN VARCHAR2) IS
    v_user VARCHAR2(30) := valid_username(p_username);
    v_role staff.role_name%TYPE := managed_role(v_user);
  BEGIN
    UPDATE staff SET full_name = p_full_name, phone = p_phone WHERE username = v_user;
    COMMIT;
  END update_staff_info;

  PROCEDURE change_role(p_username IN VARCHAR2, p_role IN VARCHAR2) IS
    v_user     VARCHAR2(30) := valid_username(p_username);
    v_old_role staff.role_name%TYPE := managed_role(v_user);
  BEGIN
    check_role(p_role);
    not_self(v_user);
    IF v_old_role <> p_role THEN
      EXECUTE IMMEDIATE 'REVOKE ' || v_old_role || ' FROM ' || v_user;
      EXECUTE IMMEDIATE 'GRANT '  || p_role     || ' TO '   || v_user;
      UPDATE staff SET role_name = p_role WHERE username = v_user;
      COMMIT;
    END IF;
  END change_role;

  PROCEDURE reset_password(p_username IN VARCHAR2, p_password IN VARCHAR2) IS
    v_user VARCHAR2(30) := valid_username(p_username);
    v_role staff.role_name%TYPE := managed_role(v_user);
  BEGIN
    check_password(p_password);
    EXECUTE IMMEDIATE 'ALTER USER ' || v_user || ' IDENTIFIED BY "' || p_password || '"';
  END reset_password;

  PROCEDURE set_locked(p_username IN VARCHAR2, p_locked IN NUMBER) IS
    v_user VARCHAR2(30) := valid_username(p_username);
    v_role staff.role_name%TYPE := managed_role(v_user);
  BEGIN
    not_self(v_user);
    EXECUTE IMMEDIATE 'ALTER USER ' || v_user || CASE WHEN p_locked = 1 THEN ' ACCOUNT LOCK' ELSE ' ACCOUNT UNLOCK' END;
    UPDATE staff SET status = CASE WHEN p_locked = 1 THEN 'LOCKED' ELSE 'ACTIVE' END WHERE username = v_user;
    COMMIT;
  END set_locked;

END pkg_staff;
/

-- Dừng nếu có object bị lỗi biên dịch
SELECT name, type, line, text FROM user_errors ORDER BY name, sequence;
DECLARE
  v_errors NUMBER;
BEGIN
  SELECT COUNT(*) INTO v_errors FROM user_errors;
  IF v_errors > 0 THEN
    RAISE_APPLICATION_ERROR(-20999, 'Compilation errors: ' || v_errors);
  END IF;
END;
/

EXIT
