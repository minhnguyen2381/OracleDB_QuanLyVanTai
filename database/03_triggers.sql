WHENEVER SQLERROR EXIT FAILURE ROLLBACK
SET DEFINE OFF
SET SQLBLANKLINES ON
SET SERVEROUTPUT ON SIZE UNLIMITED

-- Kiểm tra ghế hợp lệ và chưa bị đặt.
-- Gom các dòng ở AFTER EACH ROW, kiểm tra ở AFTER STATEMENT để tránh lỗi mutating table.
CREATE OR REPLACE TRIGGER trg_tickets_check_seat
FOR INSERT OR UPDATE OF seat_no, schedule_id, status ON tickets
COMPOUND TRIGGER
  TYPE t_id_list IS TABLE OF tickets.ticket_id%TYPE;
  g_ids t_id_list := t_id_list();

  AFTER EACH ROW IS
  BEGIN
    -- Vé bị hủy thì không cần kiểm tra
    IF :NEW.status <> 'CANCELLED' THEN
      g_ids.EXTEND;
      g_ids(g_ids.LAST) := :NEW.ticket_id;
    END IF;
  END AFTER EACH ROW;

  AFTER STATEMENT IS
    v_seat_no    tickets.seat_no%TYPE;
    v_sched_id   tickets.schedule_id%TYPE;
    v_capacity   vehicles.seat_count%TYPE;
    v_sched_stat schedules.status%TYPE;
    v_taken      NUMBER;
  BEGIN
    FOR i IN 1 .. g_ids.COUNT LOOP
      SELECT t.seat_no, t.schedule_id, v.seat_count, s.status
        INTO v_seat_no, v_sched_id, v_capacity, v_sched_stat
        FROM tickets t
        JOIN schedules s ON s.schedule_id = t.schedule_id
        JOIN vehicles  v ON v.vehicle_id  = s.vehicle_id
       WHERE t.ticket_id = g_ids(i);

      -- Ghế phải nằm trong sức chứa của xe
      IF v_seat_no NOT BETWEEN 1 AND v_capacity THEN
        RAISE_APPLICATION_ERROR(-20001, 'Seat ' || v_seat_no || ' does not exist (vehicle has ' || v_capacity || ' seats).');
      END IF;

      -- Chỉ bán/thanh toán vé cho chuyến chưa chạy
      IF v_sched_stat <> 'SCHEDULED' THEN
        RAISE_APPLICATION_ERROR(-20002, 'Schedule is not open for sale (status ' || v_sched_stat || ').');
      END IF;

      -- Mỗi ghế chỉ có đúng 1 vé đang hiệu lực
      SELECT COUNT(*) INTO v_taken
        FROM tickets
       WHERE schedule_id = v_sched_id AND seat_no = v_seat_no AND status <> 'CANCELLED';
      IF v_taken > 1 THEN
        RAISE_APPLICATION_ERROR(-20003, 'Seat ' || v_seat_no || ' is already booked.');
      END IF;
    END LOOP;
  END AFTER STATEMENT;
END;
/

-- Không cho chuyển chuyến sang CANCELLED nếu còn vé hiệu lực
CREATE OR REPLACE TRIGGER trg_schedules_check_cancel
BEFORE UPDATE OF status ON schedules
FOR EACH ROW
WHEN (NEW.status = 'CANCELLED' AND OLD.status <> 'CANCELLED')
DECLARE
  v_active NUMBER;
BEGIN
  SELECT COUNT(*) INTO v_active
    FROM tickets
   WHERE schedule_id = :NEW.schedule_id AND status <> 'CANCELLED';
  IF v_active > 0 THEN
    RAISE_APPLICATION_ERROR(-20052, 'Schedule has active tickets; use PKG_SCHEDULE.CANCEL_SCHEDULE.');
  END IF;
END;
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
