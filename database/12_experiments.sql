-- Thực nghiệm cho báo cáo: TN1 (chế độ ghi log) và TN3 (chỉ mục, kế hoạch thực thi).
-- Chạy trên máy chủ bằng xác thực hệ điều hành:  sqlplus -S -L "/ as sysdba" @12_experiments.sql
-- Không sửa dữ liệu thật. TN3 tạo bảng tạm TRANSPORT.EXP_TICKETS (450.000 dòng) và xoá hẳn ở cuối.
-- Nếu kịch bản dừng giữa chừng: DROP TABLE transport.exp_tickets PURGE;
WHENEVER SQLERROR EXIT FAILURE ROLLBACK
SET SERVEROUTPUT ON SIZE UNLIMITED
SET LINESIZE 200 PAGESIZE 100 FEEDBACK OFF TRIMSPOOL ON
ALTER SESSION SET CONTAINER = XEPDB1;

PROMPT === Moi truong ===
SELECT banner_full FROM v$version;
SELECT log_mode, flashback_on FROM v$database;
SELECT COUNT(*) AS redo_groups, MAX(bytes) / 1024 / 1024 AS redo_mb FROM v$log;
SELECT name, value FROM v$parameter WHERE name IN ('cpu_count', 'sga_target', 'pga_aggregate_target');

PROMPT === So dong bang that (truoc) ===
SELECT (SELECT COUNT(*) FROM transport.schedules)  AS schedules,
       (SELECT COUNT(*) FROM transport.tickets)    AS tickets,
       (SELECT COUNT(*) FROM transport.payments)   AS payments,
       (SELECT COUNT(*) FROM transport.passengers) AS passengers
  FROM dual;

PROMPT === TN3: tao bang EXP_TICKETS ===
BEGIN
  EXECUTE IMMEDIATE 'DROP TABLE transport.exp_tickets PURGE';
EXCEPTION WHEN OTHERS THEN IF SQLCODE != -942 THEN RAISE; END IF;
END;
/
-- Cùng các cột chính của TICKETS. 10.000 chuyến x 45 ghế; 10% vé đã huỷ.
-- Dòng được chèn theo thứ tự ngẫu nhiên (seed cố định) để mô phỏng vé của nhiều chuyến bán xen kẽ.
CREATE TABLE transport.exp_tickets (
  ticket_id    NUMBER PRIMARY KEY,
  schedule_id  NUMBER       NOT NULL,
  passenger_id NUMBER       NOT NULL,
  seat_no      NUMBER(2)    NOT NULL,
  price        NUMBER(12)   NOT NULL,
  booked_at    TIMESTAMP    NOT NULL,
  sold_by      VARCHAR2(30) NOT NULL,
  status       VARCHAR2(10) NOT NULL
);
EXEC DBMS_RANDOM.SEED(42)
INSERT /*+ APPEND */ INTO transport.exp_tickets
SELECT ROWNUM, schedule_id, passenger_id, seat_no, 250000, booked_at, 'SELLER01', status
  FROM (SELECT CEIL(n / 45) AS schedule_id, MOD(n - 1, 45) + 1 AS seat_no,
               MOD(n * 7919, 50000) + 1 AS passenger_id,
               SYSTIMESTAMP - NUMTODSINTERVAL(MOD(n, 525600), 'MINUTE') AS booked_at,
               CASE WHEN MOD(n, 10) = 0 THEN 'CANCELLED' WHEN MOD(n, 3) = 0 THEN 'BOOKED' ELSE 'PAID' END AS status
          FROM (SELECT LEVEL AS n FROM dual CONNECT BY LEVEL <= 450000)
         ORDER BY DBMS_RANDOM.VALUE);
COMMIT;
EXEC DBMS_STATS.GATHER_TABLE_STATS('TRANSPORT', 'EXP_TICKETS')
SELECT num_rows, blocks FROM dba_tables WHERE owner = 'TRANSPORT' AND table_name IN ('EXP_TICKETS', 'TICKETS');

PROMPT === TN3: truy van so do ghe (giong ung dung) tren tung phuong an ===
DECLARE
  TYPE t_num IS TABLE OF NUMBER;
  v_real t_num;
  v_ids  t_num := t_num();
  c_runs CONSTANT PLS_INTEGER := 500;

  FUNCTION logical_reads RETURN NUMBER IS
    v NUMBER;
  BEGIN
    SELECT m.value INTO v FROM v$mystat m JOIN v$statname s ON s.statistic# = m.statistic#
     WHERE s.name = 'session logical reads';
    RETURN v;
  END;

  PROCEDURE run_variant(p_label VARCHAR2, p_table VARCHAR2, p_ids t_num) IS
    v_sql   VARCHAR2(200) := 'SELECT seat_no FROM ' || p_table
                             || ' WHERE schedule_id = :id AND status <> ''CANCELLED''';
    v_seats t_num;
    v_rows  NUMBER := 0;
    v_t0    PLS_INTEGER;
    v_r0    NUMBER;
    v_cs    PLS_INTEGER;
    v_reads NUMBER;
  BEGIN
    DBMS_OUTPUT.PUT_LINE('--- ' || p_label);
    EXECUTE IMMEDIATE 'EXPLAIN PLAN FOR ' || REPLACE(v_sql, ':id', TO_CHAR(p_ids(1)));
    FOR r IN (SELECT plan_table_output AS l FROM TABLE(DBMS_XPLAN.DISPLAY(NULL, NULL, 'BASIC +ROWS +COST'))) LOOP
      DBMS_OUTPUT.PUT_LINE(r.l);
    END LOOP;
    EXECUTE IMMEDIATE v_sql BULK COLLECT INTO v_seats USING p_ids(1);  -- parse + làm nóng bộ đệm
    v_r0 := logical_reads;
    v_t0 := DBMS_UTILITY.GET_TIME;
    FOR i IN 1 .. p_ids.COUNT LOOP
      EXECUTE IMMEDIATE v_sql BULK COLLECT INTO v_seats USING p_ids(i);
      v_rows := v_rows + v_seats.COUNT;
    END LOOP;
    v_cs    := DBMS_UTILITY.GET_TIME - v_t0;
    v_reads := logical_reads - v_r0;
    DBMS_OUTPUT.PUT_LINE('queries=' || p_ids.COUNT || ' rows_per_query=' || ROUND(v_rows / p_ids.COUNT, 1)
      || ' logical_reads_per_query=' || ROUND(v_reads / p_ids.COUNT, 1)
      || ' ms_per_query=' || TO_CHAR(v_cs * 10 / p_ids.COUNT, 'FM990.000'));
  END;

  PROCEDURE show_index(p_name VARCHAR2) IS
  BEGIN
    FOR r IN (SELECT index_name, blevel, leaf_blocks, clustering_factor FROM dba_indexes
               WHERE owner = 'TRANSPORT' AND index_name = p_name) LOOP
      DBMS_OUTPUT.PUT_LINE('index ' || r.index_name || ' blevel=' || r.blevel || ' leaf_blocks=' || r.leaf_blocks
        || ' clustering_factor=' || r.clustering_factor);
    END LOOP;
  END;
BEGIN
  -- V0: bảng TICKETS thật, lần lượt các chuyến hiện có
  SELECT schedule_id BULK COLLECT INTO v_real FROM transport.schedules ORDER BY schedule_id;
  FOR i IN 1 .. c_runs LOOP
    v_ids.EXTEND; v_ids(i) := v_real(MOD(i - 1, v_real.COUNT) + 1);
  END LOOP;
  show_index('IDX_TICKETS_SCHEDULE');
  run_variant('V0 TICKETS that, chi muc idx_tickets_schedule', 'transport.tickets', v_ids);

  -- V1-V3: bảng EXP_TICKETS, 500 chuyến chọn giả ngẫu nhiên
  FOR i IN 1 .. c_runs LOOP
    v_ids(i) := MOD(i * 7919, 10000) + 1;
  END LOOP;
  run_variant('V1 EXP_TICKETS khong co chi muc tren schedule_id', 'transport.exp_tickets', v_ids);

  EXECUTE IMMEDIATE 'CREATE INDEX transport.exp_idx_schedule ON transport.exp_tickets(schedule_id)';
  show_index('EXP_IDX_SCHEDULE');
  run_variant('V2 EXP_TICKETS chi muc (schedule_id)', 'transport.exp_tickets', v_ids);

  EXECUTE IMMEDIATE 'DROP INDEX transport.exp_idx_schedule';
  EXECUTE IMMEDIATE 'CREATE INDEX transport.exp_idx_cover ON transport.exp_tickets(schedule_id, status, seat_no)';
  show_index('EXP_IDX_COVER');
  run_variant('V3 EXP_TICKETS chi muc phu (schedule_id, status, seat_no)', 'transport.exp_tickets', v_ids);
END;
/

PROMPT === Don dep ===
DROP TABLE transport.exp_tickets PURGE;
SELECT COUNT(*) AS exp_tables_left FROM dba_tables WHERE owner = 'TRANSPORT' AND table_name LIKE 'EXP%';

PROMPT === So dong bang that (sau) ===
SELECT (SELECT COUNT(*) FROM transport.schedules)  AS schedules,
       (SELECT COUNT(*) FROM transport.tickets)    AS tickets,
       (SELECT COUNT(*) FROM transport.payments)   AS payments,
       (SELECT COUNT(*) FROM transport.passengers) AS passengers
  FROM dual;
EXIT
