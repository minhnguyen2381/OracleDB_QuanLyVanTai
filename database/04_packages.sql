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
