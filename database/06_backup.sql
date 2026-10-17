WHENEVER SQLERROR EXIT FAILURE ROLLBACK
SET DEFINE OFF
SET SQLBLANKLINES ON
SET SERVEROUTPUT ON SIZE UNLIMITED

-- Nhật ký các lần sao lưu
CREATE TABLE backup_logs (
  log_id        NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  backup_at     TIMESTAMP DEFAULT SYSTIMESTAMP NOT NULL,
  performed_by  VARCHAR2(30) DEFAULT USER NOT NULL,  -- 'TRANSPORT' = job tự động
  schedule_rows NUMBER,
  ticket_rows   NUMBER,
  payment_rows  NUMBER
);

-- Bảng backup: cấu trúc như bảng gốc + backup_id, backup_at ở cuối
CREATE TABLE schedules_bk AS SELECT s.*, 0 AS backup_id, SYSTIMESTAMP AS backup_at FROM schedules s WHERE 1 = 0;
CREATE TABLE tickets_bk   AS SELECT t.*, 0 AS backup_id, SYSTIMESTAMP AS backup_at FROM tickets   t WHERE 1 = 0;
CREATE TABLE payments_bk  AS SELECT p.*, 0 AS backup_id, SYSTIMESTAMP AS backup_at FROM payments  p WHERE 1 = 0;

-- Chụp toàn bộ lịch chạy, vé, thanh toán; giữ 7 bản gần nhất
CREATE OR REPLACE PROCEDURE backup_operational_data IS
  c_keep  CONSTANT PLS_INTEGER := 7;
  v_id    backup_logs.log_id%TYPE;
  v_sched NUMBER;
  v_tick  NUMBER;
  v_pay   NUMBER;
BEGIN
  INSERT INTO backup_logs (performed_by) VALUES (USER) RETURNING log_id INTO v_id;

  INSERT INTO schedules_bk SELECT s.*, v_id, SYSTIMESTAMP FROM schedules s; v_sched := SQL%ROWCOUNT;
  INSERT INTO tickets_bk   SELECT t.*, v_id, SYSTIMESTAMP FROM tickets   t; v_tick  := SQL%ROWCOUNT;
  INSERT INTO payments_bk  SELECT p.*, v_id, SYSTIMESTAMP FROM payments  p; v_pay   := SQL%ROWCOUNT;

  UPDATE backup_logs
     SET schedule_rows = v_sched, ticket_rows = v_tick, payment_rows = v_pay
   WHERE log_id = v_id;

  -- Xóa các bản cũ hơn 7 lần sao lưu gần nhất
  DELETE FROM schedules_bk WHERE backup_id <= v_id - c_keep;
  DELETE FROM tickets_bk   WHERE backup_id <= v_id - c_keep;
  DELETE FROM payments_bk  WHERE backup_id <= v_id - c_keep;
  COMMIT;
EXCEPTION
  WHEN OTHERS THEN ROLLBACK; RAISE;
END;
/

-- Sao lưu tự động lúc 2h sáng mỗi ngày
BEGIN
  DBMS_SCHEDULER.CREATE_JOB(
    job_name        => 'JOB_NIGHTLY_BACKUP',
    job_type        => 'STORED_PROCEDURE',
    job_action      => 'BACKUP_OPERATIONAL_DATA',
    repeat_interval => 'FREQ=DAILY;BYHOUR=2;BYMINUTE=0',
    enabled         => TRUE,
    comments        => 'Nightly snapshot of schedules, tickets and payments');
END;
/

-- Trạng thái job sao lưu đêm (điều hành không có quyền đọc user_scheduler_jobs của TRANSPORT)
CREATE OR REPLACE VIEW v_backup_job_status AS
SELECT job_name, enabled, state, last_start_date, next_run_date, failure_count
  FROM user_scheduler_jobs
 WHERE job_name = 'JOB_NIGHTLY_BACKUP';

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
