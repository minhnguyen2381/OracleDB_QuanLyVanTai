# Plan 1 – Oracle Database Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the complete Oracle schema `TRANSPORT` (tables, triggers, packages, views, backup, roles, seed data, test scenarios, install/backup scripts) for the bus transport & ticketing system.

**Architecture:** All business rules live in Oracle: compound trigger checks seats, `PKG_TICKET`/`PKG_SCHEDULE`/`PKG_STAFF` are the only write paths for tickets/payments/staff, two roles receive object grants, a scheduler job snapshots operational data nightly, Data Pump scripts export/import the schema. A PL/SQL test script prints PASS/FAIL per scenario.

**Tech Stack:** Oracle Database 21c XE (PDB `XEPDB1`, already installed at `D:\OracleDB\dbhomeXE`, listener on `localhost:1521`), SQL*Plus 21.3, Data Pump (`expdp`/`impdp`), Windows batch.

**Spec:** `docs/superpowers/specs/2026-10-17-transport-management-design.md` – sections 3–11 and 13 contain the exact SQL. Where a step says *"copy verbatim from spec §X"*, copy that code block character-for-character.

## Global Constraints

- Every SQL identifier (table, column, constraint, index, trigger, package, procedure, view, role, job, status value) is **English**. Error messages in `RAISE_APPLICATION_ERROR` are **English**. **Only comments are Vietnamese.**
- Error codes and messages must match spec §9 exactly (they are the contract with the frontend).
- Schema owner `TRANSPORT`, password `Transport#2026`. Roles `ROLE_OPERATOR`, `ROLE_TICKET_SELLER`. Demo staff: `operator01`/`Operator#01`, `seller01`/`Seller#001`, `seller02`/`Seller#002`.
- Connect string: `//localhost:1521/XEPDB1`. Backup directory: `D:\OracleBackup` (Oracle directory object `BACKUP_DIR`).
- All `.sql`/`.bat` files are saved as **UTF-8 without BOM**. Before running SQL*Plus always set `NLS_LANG=AMERICAN_AMERICA.AL32UTF8` (otherwise Vietnamese seed text is corrupted).
- Every `.sql` file starts with the standard header below and ends with `EXIT`:

```sql
WHENEVER SQLERROR EXIT FAILURE ROLLBACK
SET DEFINE OFF
SET SQLBLANKLINES ON
SET SERVEROUTPUT ON SIZE UNLIMITED
```

- SYS password is **not** stored anywhere. Before Task 1, ask the user for it and keep it in the shell variable `ORACLE_SYS_PASSWORD` for the session.

### Shell helpers used in every task (Git Bash)

```bash
export NLS_LANG=AMERICAN_AMERICA.AL32UTF8
DB="//localhost:1521/XEPDB1"
run_sys()       { sqlplus -S -L "sys/${ORACLE_SYS_PASSWORD}@${DB} as sysdba" "@$1"; }
run_transport() { sqlplus -S -L "transport/\"Transport#2026\"@${DB}" "@$1"; }
```

Run them from the repo root `D:/DuAn/OracleDB_QuanLyVanTai`.

---

### Task 1: SYS setup and uninstall scripts

**Files:**
- Create: `database/00_uninstall.sql`
- Create: `database/01_sys_setup.sql`

**Interfaces:**
- Produces: user `TRANSPORT` (with `CREATE USER`, `ALTER USER`, `DROP USER`, `CREATE JOB`, admin option on both roles), roles `ROLE_OPERATOR`, `ROLE_TICKET_SELLER`, directory `BACKUP_DIR`.

- [ ] **Step 1: Verify the database character set is AL32UTF8**

Run:
```bash
echo "SELECT value FROM nls_database_parameters WHERE parameter = 'NLS_CHARACTERSET';" | sqlplus -S -L "sys/${ORACLE_SYS_PASSWORD}@${DB} as sysdba"
```
Expected: `AL32UTF8`. If not, stop and tell the user (Vietnamese text would not store correctly).

- [ ] **Step 2: Create the backup folder**

Run: `mkdir -p /d/OracleBackup`

- [ ] **Step 3: Write `database/00_uninstall.sql`**

```sql
-- Gỡ toàn bộ CSDL vận tải để cài lại sạch (chạy bằng SYS trong XEPDB1).
-- Lỗi "không tồn tại" được bỏ qua để chạy được cả khi mới cài một phần.
WHENEVER SQLERROR CONTINUE
SET DEFINE OFF
SET SERVEROUTPUT ON SIZE UNLIMITED

-- Xóa các user nhân viên có trong TRANSPORT.STAFF (dùng SQL động vì bảng có thể chưa tồn tại)
DECLARE
  TYPE t_names IS TABLE OF VARCHAR2(30);
  v_users t_names;
BEGIN
  EXECUTE IMMEDIATE 'SELECT username FROM transport.staff' BULK COLLECT INTO v_users;
  FOR i IN 1 .. v_users.COUNT LOOP
    BEGIN
      EXECUTE IMMEDIATE 'DROP USER ' || v_users(i) || ' CASCADE';
    EXCEPTION WHEN OTHERS THEN NULL;
    END;
  END LOOP;
EXCEPTION WHEN OTHERS THEN NULL;
END;
/

-- Phòng trường hợp bảng STAFF đã mất nhưng user demo/test vẫn còn
BEGIN
  FOR u IN (SELECT username FROM dba_users
             WHERE username IN ('OPERATOR01', 'SELLER01', 'SELLER02', 'TEST_STAFF_01')) LOOP
    EXECUTE IMMEDIATE 'DROP USER ' || u.username || ' CASCADE';
  END LOOP;
END;
/

DROP USER transport CASCADE;
DROP ROLE role_operator;
DROP ROLE role_ticket_seller;
DROP DIRECTORY backup_dir;

EXIT
```

- [ ] **Step 4: Write `database/01_sys_setup.sql`**

Standard header (Global Constraints), then this line so the user gets a default tablespace:

```sql
-- Chủ schema: chỉ dùng để tạo/bảo trì object, app không đăng nhập bằng user này
CREATE USER transport IDENTIFIED BY "Transport#2026" DEFAULT TABLESPACE users QUOTA UNLIMITED ON users;
```

then copy verbatim from spec §7.1 **everything after** its `CREATE USER transport ...` line (the `GRANT CREATE SESSION ...` line through `GRANT READ, WRITE ON DIRECTORY backup_dir TO transport;`), then `EXIT`.

- [ ] **Step 5: Run uninstall then setup**

Run:
```bash
run_sys database/00_uninstall.sql
run_sys database/01_sys_setup.sql && echo SETUP_OK
```
Expected: last line `SETUP_OK` (uninstall may print ORA-01918/ORA-01919 "does not exist" – that is fine on first run).

- [ ] **Step 6: Verify TRANSPORT can log in and has the privileges**

Run:
```bash
echo "SELECT privilege FROM user_sys_privs ORDER BY 1; SELECT granted_role, admin_option FROM user_role_privs ORDER BY 1;" | sqlplus -S -L "transport/\"Transport#2026\"@${DB}"
```
Expected: privileges include `ALTER USER`, `CREATE JOB`, `CREATE USER`, `DROP USER`; roles `ROLE_OPERATOR YES`, `ROLE_TICKET_SELLER YES`.

- [ ] **Step 7: Commit**

```bash
git add database/00_uninstall.sql database/01_sys_setup.sql
git commit -m "feat(db): add SYS setup and uninstall scripts"
```

---

### Task 2: Tables

**Files:**
- Create: `database/02_tables.sql`

**Interfaces:**
- Produces: tables `ROUTES, VEHICLES, DRIVERS, SCHEDULES, PASSENGERS, TICKETS, PAYMENTS, STAFF` with columns exactly as spec §3.1.

- [ ] **Step 1: Write `database/02_tables.sql`**

Standard header, then copy verbatim the whole SQL block of spec §3.1, then `EXIT`.

- [ ] **Step 2: Run it**

Run: `run_transport database/02_tables.sql && echo TABLES_OK`
Expected: `TABLES_OK`.

- [ ] **Step 3: Verify the tables and the seat CHECK constraint**

Run:
```bash
echo "SELECT table_name FROM user_tables ORDER BY 1;
INSERT INTO vehicles (plate_number, vehicle_type, seat_count) VALUES ('X', 'X', 99);" | sqlplus -S -L "transport/\"Transport#2026\"@${DB}"
```
Expected: 8 tables listed (`DRIVERS, PASSENGERS, PAYMENTS, ROUTES, SCHEDULES, STAFF, TICKETS, VEHICLES`), then `ORA-02290: check constraint (TRANSPORT.CK_VEHICLES_SEATS) violated`.

- [ ] **Step 4: Commit**

```bash
git add database/02_tables.sql
git commit -m "feat(db): add core tables"
```

---

### Task 3: Seat trigger, ticket & schedule packages, statistics view (TDD)

**Files:**
- Create: `database/09_test_scenarios.sql` (scenarios 1–8)
- Create: `database/03_triggers.sql`
- Create: `database/04_packages.sql` (`PKG_TICKET`, `PKG_SCHEDULE`)
- Create: `database/05_views.sql`

**Interfaces:**
- Consumes: tables from Task 2.
- Produces:
  - `pkg_ticket.book_ticket(p_schedule_id NUMBER, p_full_name VARCHAR2, p_phone VARCHAR2, p_id_card VARCHAR2, p_seat_no NUMBER, o_ticket_id OUT NUMBER)`
  - `pkg_ticket.pay_ticket(p_ticket_id NUMBER, p_method VARCHAR2)`
  - `pkg_ticket.cancel_ticket(p_ticket_id NUMBER, p_reason VARCHAR2)`
  - `pkg_schedule.cancel_schedule(p_schedule_id NUMBER, p_reason VARCHAR2)`
  - view `v_trip_statistics` with columns `schedule_id, route_id, vehicle_id, driver_id, route_name, origin, destination, departure_time, arrival_time, ticket_price, schedule_status, plate_number, vehicle_type, driver_name, seat_count, seats_sold, seats_available, occupancy_pct, tickets_cancelled, revenue`.

- [ ] **Step 1: Write the failing test script `database/09_test_scenarios.sql`**

```sql
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

  -- @@BACKUP_TESTS@@

  -- @@STAFF_TESTS@@

  cleanup;
  DBMS_OUTPUT.PUT_LINE(CASE WHEN g_fail = 0 THEN 'ALL TESTS PASSED'
                            ELSE g_fail || ' TEST(S) FAILED' END);
END;
/

EXIT
```

The two `-- @@...@@` comment lines are insertion markers for Tasks 4 and 5 (they are plain comments, the script runs without them being replaced).

- [ ] **Step 2: Run it to verify it fails**

Run: `run_transport database/09_test_scenarios.sql`
Expected: fails with `PLS-00201: identifier 'PKG_TICKET.BOOK_TICKET' must be declared`.

- [ ] **Step 3: Write `database/03_triggers.sql`**

Standard header, then copy verbatim spec §4.1 block and spec §4.2 block, then this compile check, then `EXIT`:

```sql
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
```

- [ ] **Step 4: Write `database/04_packages.sql`**

Standard header, then copy verbatim spec §5.1 (`pkg_ticket` spec + body) and spec §5.2 (`pkg_schedule` spec + body), then the **same compile-check block** as Step 3 (place it at the very end of the file, right before `EXIT`; Task 5 will insert `pkg_staff` above it).

- [ ] **Step 5: Write `database/05_views.sql`**

Standard header, then copy verbatim spec §6 view, then the same compile-check block, then `EXIT`.

- [ ] **Step 6: Run the three scripts**

Run:
```bash
run_transport database/03_triggers.sql && run_transport database/04_packages.sql && run_transport database/05_views.sql && echo OBJECTS_OK
```
Expected: `OBJECTS_OK` and the `user_errors` query prints `no rows selected` each time.

- [ ] **Step 7: Run the tests to verify they pass**

Run: `run_transport database/09_test_scenarios.sql`
Expected: every line starts with `PASS`, last line `ALL TESTS PASSED`.

- [ ] **Step 8: Commit**

```bash
git add database/03_triggers.sql database/04_packages.sql database/05_views.sql database/09_test_scenarios.sql
git commit -m "feat(db): add seat trigger, ticket/schedule packages and trip statistics view"
```

---

### Task 4: Backup tables, procedure and nightly job (TDD)

**Files:**
- Modify: `database/09_test_scenarios.sql` (replace marker `-- @@BACKUP_TESTS@@`)
- Create: `database/06_backup.sql`

**Interfaces:**
- Produces: procedure `backup_operational_data` (no params), tables `backup_logs`, `schedules_bk`, `tickets_bk`, `payments_bk`, job `JOB_NIGHTLY_BACKUP`, view `v_backup_job_status(job_name, enabled, state, last_start_date, next_run_date, failure_count)`.

- [ ] **Step 1: Add the failing test – replace the line `  -- @@BACKUP_TESTS@@` with**

```sql
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
```

- [ ] **Step 2: Run to verify it fails**

Run: `run_transport database/09_test_scenarios.sql`
Expected: fails with `PLS-00201: identifier 'BACKUP_OPERATIONAL_DATA' must be declared` (the whole block does not compile).

- [ ] **Step 3: Write `database/06_backup.sql`**

Standard header, then copy verbatim **both** SQL blocks of spec §8.1 (tables + procedure + job, then the `v_backup_job_status` view), then the same compile-check block as Task 3 Step 3, then `EXIT`.

- [ ] **Step 4: Run it and the tests**

Run:
```bash
run_transport database/06_backup.sql && run_transport database/09_test_scenarios.sql
```
Expected: all `PASS`, `ALL TESTS PASSED`.

- [ ] **Step 5: Commit**

```bash
git add database/06_backup.sql database/09_test_scenarios.sql
git commit -m "feat(db): add operational data backup procedure and nightly job"
```

---

### Task 5: Staff package and role grants (TDD)

**Files:**
- Modify: `database/09_test_scenarios.sql` (replace marker `-- @@STAFF_TESTS@@`)
- Modify: `database/04_packages.sql` (add `pkg_staff` before the compile-check block)
- Create: `database/07_grants.sql`

**Interfaces:**
- Produces: `pkg_staff.create_staff(p_username, p_password, p_full_name, p_phone, p_role)`, `pkg_staff.update_staff_info(p_username, p_full_name, p_phone)`, `pkg_staff.change_role(p_username, p_role)`, `pkg_staff.reset_password(p_username, p_password)`, `pkg_staff.set_locked(p_username, p_locked NUMBER)`; object grants of spec §7.3.

- [ ] **Step 1: Add the failing tests – replace the line `  -- @@STAFF_TESTS@@` with**

```sql
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
```

Also add these two lines at the **start** of the `cleanup` procedure body (so a previous failed run is cleaned up):

```sql
    DELETE FROM staff WHERE username = 'TEST_STAFF_01';
    BEGIN EXECUTE IMMEDIATE 'DROP USER test_staff_01'; EXCEPTION WHEN OTHERS THEN NULL; END;
```

- [ ] **Step 2: Run to verify it fails**

Run: `run_transport database/09_test_scenarios.sql`
Expected: fails with `PLS-00201: identifier 'PKG_STAFF.CREATE_STAFF' must be declared`.

- [ ] **Step 3: Add `pkg_staff` to `database/04_packages.sql`**

Insert spec §5.3 (`pkg_staff` spec + body) verbatim **after** the `pkg_schedule` body's `/` and **before** the compile-check block.

- [ ] **Step 4: Write `database/07_grants.sql`**

Standard header, then copy verbatim spec §7.3 block, then `EXIT`.

- [ ] **Step 5: Run packages, grants and tests**

Run:
```bash
run_transport database/04_packages.sql && run_transport database/07_grants.sql && run_transport database/09_test_scenarios.sql
```
Expected: all `PASS`, `ALL TESTS PASSED`.

- [ ] **Step 6: Commit**

```bash
git add database/04_packages.sql database/07_grants.sql database/09_test_scenarios.sql
git commit -m "feat(db): add staff management package and role grants"
```

---

### Task 6: Seed data, install/backup scripts, full reinstall and permission checks

**Files:**
- Create: `database/08_seed_data.sql`
- Create: `database/install_all.bat`
- Create: `database/backup.bat`
- Create: `database/restore.bat`
- Modify: `database/09_test_scenarios.sql` (append permission-check notes)

**Interfaces:**
- Consumes: everything from Tasks 1–5.
- Produces: demo data (5 routes, 6 vehicles incl. `29B-196.88`, 6 drivers, 30 passengers, 21 future + 14 past schedules, tickets/payments), demo staff `operator01`, `seller01`, `seller02`.

- [ ] **Step 1: Write `database/08_seed_data.sql`**

```sql
-- Dữ liệu mẫu để demo (chạy bằng TRANSPORT, sau 07_grants.sql)
WHENEVER SQLERROR EXIT FAILURE ROLLBACK
SET DEFINE OFF
SET SQLBLANKLINES ON
SET SERVEROUTPUT ON SIZE UNLIMITED

-- Tuyến xe
INSERT INTO routes (route_name, origin, destination, distance_km, duration_min, base_price) VALUES ('Hà Nội - Lào Cai',   'Hà Nội', 'Lào Cai',   300, 330, 280000);
INSERT INTO routes (route_name, origin, destination, distance_km, duration_min, base_price) VALUES ('Hà Nội - Hải Phòng', 'Hà Nội', 'Hải Phòng', 120, 150, 120000);
INSERT INTO routes (route_name, origin, destination, distance_km, duration_min, base_price) VALUES ('Hà Nội - Ninh Bình', 'Hà Nội', 'Ninh Bình',  95, 120, 100000);
INSERT INTO routes (route_name, origin, destination, distance_km, duration_min, base_price) VALUES ('Hà Nội - Sa Pa',     'Hà Nội', 'Sa Pa',     320, 360, 350000);
INSERT INTO routes (route_name, origin, destination, distance_km, duration_min, base_price) VALUES ('Hà Nội - Thanh Hóa', 'Hà Nội', 'Thanh Hóa', 150, 180, 150000);

-- Phương tiện (xe cuối đang bảo trì)
INSERT INTO vehicles (plate_number, vehicle_type, seat_count, status) VALUES ('29B-196.88', 'Giường nằm 40 chỗ', 40, 'ACTIVE');
INSERT INTO vehicles (plate_number, vehicle_type, seat_count, status) VALUES ('29B-123.45', 'Ghế ngồi 45 chỗ',   45, 'ACTIVE');
INSERT INTO vehicles (plate_number, vehicle_type, seat_count, status) VALUES ('29B-678.90', 'Limousine 16 chỗ',  16, 'ACTIVE');
INSERT INTO vehicles (plate_number, vehicle_type, seat_count, status) VALUES ('24B-012.34', 'Ghế ngồi 29 chỗ',   29, 'ACTIVE');
INSERT INTO vehicles (plate_number, vehicle_type, seat_count, status) VALUES ('15B-345.67', 'Giường nằm 40 chỗ', 40, 'ACTIVE');
INSERT INTO vehicles (plate_number, vehicle_type, seat_count, status) VALUES ('35B-888.99', 'Ghế ngồi 29 chỗ',   29, 'MAINTENANCE');

-- Tài xế (Phạm Văn Dũng có bằng sắp hết hạn để thấy cảnh báo)
INSERT INTO drivers (full_name, id_card, phone, license_no, license_class, license_expiry) VALUES ('Nguyễn Văn An',  '001085012345', '0912345601', '790123456781', 'D',  ADD_MONTHS(TRUNC(SYSDATE), 36));
INSERT INTO drivers (full_name, id_card, phone, license_no, license_class, license_expiry) VALUES ('Trần Văn Bình',  '001086023456', '0912345602', '790123456782', 'D',  ADD_MONTHS(TRUNC(SYSDATE), 24));
INSERT INTO drivers (full_name, id_card, phone, license_no, license_class, license_expiry) VALUES ('Lê Văn Cường',   '001087034567', '0912345603', '790123456783', 'D2', ADD_MONTHS(TRUNC(SYSDATE), 18));
INSERT INTO drivers (full_name, id_card, phone, license_no, license_class, license_expiry) VALUES ('Phạm Văn Dũng',  '001088045678', '0912345604', '790123456784', 'E',  TRUNC(SYSDATE) + 20);
INSERT INTO drivers (full_name, id_card, phone, license_no, license_class, license_expiry) VALUES ('Hoàng Văn Em',   '001089056789', '0912345605', '790123456785', 'D',  ADD_MONTHS(TRUNC(SYSDATE), 30));
INSERT INTO drivers (full_name, id_card, phone, license_no, license_class, license_expiry) VALUES ('Vũ Văn Phúc',    '001090067890', '0912345606', '790123456786', 'D1', ADD_MONTHS(TRUNC(SYSDATE), 12));

-- 30 hành khách
DECLARE
  TYPE t_names IS TABLE OF VARCHAR2(100);
  v_names t_names := t_names(
    'Nguyễn Thị Hoa', 'Trần Minh Tuấn', 'Lê Thu Trang', 'Phạm Quốc Huy', 'Hoàng Mai Anh',
    'Vũ Đức Thắng', 'Đặng Thùy Linh', 'Bùi Văn Hải', 'Đỗ Ngọc Ánh', 'Hồ Văn Long',
    'Ngô Thị Lan', 'Dương Minh Khang', 'Lý Thị Mai', 'Phan Văn Toàn', 'Trịnh Thu Hà',
    'Đinh Công Sơn', 'Mai Thị Hương', 'Tạ Quang Vinh', 'Cao Thị Nga', 'Lương Văn Nam',
    'Nguyễn Hoàng Yến', 'Trần Đức Anh', 'Lê Minh Châu', 'Phạm Thị Thảo', 'Hoàng Văn Kiên',
    'Vũ Thị Ngọc', 'Đặng Văn Hùng', 'Bùi Thị Diệp', 'Đỗ Minh Quân', 'Hồ Thị Thanh');
BEGIN
  FOR i IN 1 .. v_names.COUNT LOOP
    INSERT INTO passengers (full_name, phone, id_card, email)
    VALUES (v_names(i), '0987' || LPAD(i, 6, '0'), '0010950' || LPAD(i, 5, '0'),
            CASE WHEN MOD(i, 3) = 0 THEN 'khach' || i || '@example.com' END);
  END LOOP;
END;
/

-- Lịch chạy: 7 ngày tới (3 chuyến/ngày) và 7 ngày qua (2 chuyến/ngày); kèm vé + thanh toán
DECLARE
  TYPE t_ids IS TABLE OF NUMBER;
  v_routes   t_ids;
  v_vehicles t_ids;
  v_drivers  t_ids;
  v_pass     t_ids;
  v_hours    t_ids := t_ids(7, 13, 20);
  v_k        PLS_INTEGER := 0;
  v_sched    NUMBER;
  v_dep      DATE;
  v_dur      NUMBER;
  v_price    NUMBER;
  v_seats    NUMBER;
  v_sold     NUMBER;
  v_ticket   NUMBER;
  v_seller   VARCHAR2(30);
  v_methods  SYS.ODCIVARCHAR2LIST := SYS.ODCIVARCHAR2LIST('CASH', 'TRANSFER', 'CARD');

  -- Tạo một chuyến, trả về schedule_id
  FUNCTION add_schedule(p_dep DATE) RETURN NUMBER IS
    v_route NUMBER := v_routes(MOD(v_k, v_routes.COUNT) + 1);
    v_id    NUMBER;
  BEGIN
    SELECT duration_min, base_price INTO v_dur, v_price FROM routes WHERE route_id = v_route;
    INSERT INTO schedules (route_id, vehicle_id, driver_id, departure_time, arrival_time, ticket_price)
    VALUES (v_route, v_vehicles(MOD(v_k, v_vehicles.COUNT) + 1), v_drivers(MOD(v_k, v_drivers.COUNT) + 1),
            p_dep, p_dep + v_dur / 1440, v_price)
    RETURNING schedule_id INTO v_id;
    v_k := v_k + 1;
    RETURN v_id;
  END add_schedule;

  -- Bán p_count vé (ghế 1..p_count); p_paid = 1 thì ghi thanh toán; mỗi vé thứ 5 bị hủy + hoàn tiền nếu p_with_cancel = 1
  PROCEDURE sell(p_sched NUMBER, p_count NUMBER, p_paid NUMBER, p_with_cancel NUMBER, p_booked_at DATE) IS
    v_tp        NUMBER;
    v_cancelled BOOLEAN;
  BEGIN
    -- INSERT ... SELECT không hỗ trợ RETURNING INTO nên lấy giá vé trước rồi INSERT ... VALUES
    SELECT ticket_price INTO v_tp FROM schedules WHERE schedule_id = p_sched;
    FOR s IN 1 .. p_count LOOP
      v_seller    := CASE WHEN MOD(s, 2) = 0 THEN 'SELLER01' ELSE 'SELLER02' END;
      v_cancelled := p_with_cancel = 1 AND MOD(s, 5) = 0;
      INSERT INTO tickets (schedule_id, passenger_id, seat_no, price, booked_at, sold_by, status, cancelled_at, cancel_reason)
      VALUES (p_sched, v_pass(MOD(p_sched * 7 + s, v_pass.COUNT) + 1), s, v_tp, p_booked_at, v_seller,
              CASE WHEN v_cancelled THEN 'CANCELLED' WHEN p_paid = 1 THEN 'PAID' ELSE 'BOOKED' END,
              CASE WHEN v_cancelled THEN p_booked_at + 1 END,
              CASE WHEN v_cancelled THEN 'Khách đổi kế hoạch' END)
      RETURNING ticket_id INTO v_ticket;
      IF p_paid = 1 THEN
        INSERT INTO payments (ticket_id, amount, method, payment_type, paid_at, received_by)
        VALUES (v_ticket, v_tp, v_methods(MOD(s, 3) + 1), 'PAYMENT', p_booked_at, v_seller);
        IF v_cancelled THEN
          INSERT INTO payments (ticket_id, amount, method, payment_type, paid_at, received_by)
          VALUES (v_ticket, v_tp, 'CASH', 'REFUND', p_booked_at + 1, v_seller);
        END IF;
      END IF;
    END LOOP;
  END sell;
BEGIN
  SELECT route_id     BULK COLLECT INTO v_routes   FROM routes ORDER BY route_id;
  SELECT vehicle_id   BULK COLLECT INTO v_vehicles FROM vehicles WHERE status = 'ACTIVE' ORDER BY vehicle_id;
  SELECT driver_id    BULK COLLECT INTO v_drivers  FROM drivers ORDER BY driver_id;
  SELECT passenger_id BULK COLLECT INTO v_pass     FROM passengers ORDER BY passenger_id;

  -- Chuyến đã chạy: bán 50-80% ghế khi chuyến còn SCHEDULED, sau đó đánh dấu COMPLETED
  FOR d IN 1 .. 7 LOOP
    FOR h IN 1 .. 2 LOOP
      v_dep   := TRUNC(SYSDATE) - d + CASE h WHEN 1 THEN 8 ELSE 15 END / 24;
      v_sched := add_schedule(v_dep);
      SELECT v.seat_count INTO v_seats FROM schedules s JOIN vehicles v ON v.vehicle_id = s.vehicle_id
       WHERE s.schedule_id = v_sched;
      v_sold := TRUNC(v_seats * (50 + MOD(d * 13 + h * 7, 31)) / 100);
      sell(v_sched, v_sold, 1, 1, v_dep - 2);
      UPDATE schedules SET status = 'COMPLETED' WHERE schedule_id = v_sched;
    END LOOP;
  END LOOP;

  -- Chuyến sắp chạy: 6 chuyến đầu có sẵn vài vé (BOOKED hoặc PAID) để sơ đồ ghế có ghế đỏ
  FOR d IN 0 .. 6 LOOP
    FOR h IN 1 .. v_hours.COUNT LOOP
      v_sched := add_schedule(TRUNC(SYSDATE) + d + v_hours(h) / 24);
      IF v_k <= 14 + 6 THEN
        sell(v_sched, 4 + MOD(v_k, 6), MOD(v_k, 2), 0, SYSDATE - 1);
      END IF;
    END LOOP;
  END LOOP;
END;
/

COMMIT;

-- Nhân viên demo, tạo bằng chính PKG_STAFF (tự commit)
BEGIN
  pkg_staff.create_staff('operator01', 'Operator#01', 'Nguyễn Văn Điều', '0912000001', 'ROLE_OPERATOR');
  pkg_staff.create_staff('seller01',   'Seller#001',  'Trần Thị Bán',    '0912000002', 'ROLE_TICKET_SELLER');
  pkg_staff.create_staff('seller02',   'Seller#002',  'Lê Văn Vé',       '0912000003', 'ROLE_TICKET_SELLER');
END;
/

EXIT
```

- [ ] **Step 2: Write `database/install_all.bat`**

```bat
@echo off
:: Cài lại toàn bộ CSDL vận tải (XÓA dữ liệu cũ). Cần mật khẩu SYS của Oracle XE.
setlocal
cd /d "%~dp0"
set NLS_LANG=AMERICAN_AMERICA.AL32UTF8
set DB=//localhost:1521/XEPDB1

echo This will DROP and recreate schema TRANSPORT, its roles and staff users.
set /p CONFIRM=Continue? (Y/N): 
if /i not "%CONFIRM%"=="Y" exit /b 1
set /p SYS_PWD=Enter SYS password: 

if not exist D:\OracleBackup mkdir D:\OracleBackup

sqlplus -S -L "sys/%SYS_PWD%@%DB% as sysdba" @00_uninstall.sql
sqlplus -S -L "sys/%SYS_PWD%@%DB% as sysdba" @01_sys_setup.sql || goto :fail

for %%f in (02_tables 03_triggers 04_packages 05_views 06_backup 07_grants 08_seed_data) do (
  echo === %%f ===
  sqlplus -S -L transport/"Transport#2026"@%DB% @%%f.sql || goto :fail
)

echo.
echo INSTALL OK. Demo accounts: operator01/Operator#01, seller01/Seller#001, seller02/Seller#002
exit /b 0

:fail
echo.
echo INSTALL FAILED - see the error above.
exit /b 1
```

- [ ] **Step 3: Write `database/backup.bat` and `database/restore.bat`**

Copy verbatim the two `bat` blocks of spec §8.2, adding `set NLS_LANG=AMERICAN_AMERICA.AL32UTF8` as the second line of each file (right after `@echo off`).

- [ ] **Step 4: Run a full clean install**

`install_all.bat` asks for input, so drive it with stdin:

Run:
```bash
cd database && printf 'Y\n%s\n' "$ORACLE_SYS_PASSWORD" | cmd //c install_all.bat; cd ..
```
Expected: ends with `INSTALL OK. Demo accounts: ...`.

- [ ] **Step 5: Verify seed counts and Vietnamese text**

Run:
```bash
echo "SELECT (SELECT COUNT(*) FROM routes) r, (SELECT COUNT(*) FROM vehicles) v, (SELECT COUNT(*) FROM drivers) d,
       (SELECT COUNT(*) FROM passengers) p, (SELECT COUNT(*) FROM schedules) s, (SELECT COUNT(*) FROM staff) st FROM dual;
SELECT route_name FROM routes WHERE ROWNUM = 1;
SELECT COUNT(*) FROM v_trip_statistics WHERE revenue > 0;" | sqlplus -S -L "transport/\"Transport#2026\"@${DB}"
```
Expected: `5 6 6 30 35 3`; route name prints `Hà Nội - Lào Cai` with correct diacritics; revenue count `17` (14 past trips + 3 future trips whose seed tickets are PAID).

- [ ] **Step 6: Run the test scenarios against the seeded database**

Run: `run_transport database/09_test_scenarios.sql`
Expected: `ALL TESTS PASSED` (the test data is isolated and cleaned up, seed data stays intact).

- [ ] **Step 7: Verify permissions as a seller**

Run:
```bash
echo "SELECT COUNT(*) FROM transport.v_trip_statistics;
INSERT INTO transport.tickets (schedule_id, passenger_id, seat_no, price) VALUES (1, 1, 1, 1);
EXEC transport.pkg_staff.set_locked('SELLER02', 1)
SELECT COUNT(*) FROM transport.drivers;" | sqlplus -S -L "seller01/\"Seller#001\"@${DB}"
```
Expected, in order: a count (success); `ORA-01031: insufficient privileges`; `PLS-00201: identifier 'TRANSPORT.PKG_STAFF' must be declared`; `ORA-00942: table or view does not exist`.

Run:
```bash
echo "SELECT role FROM session_roles;" | sqlplus -S -L "operator01/\"Operator#01\"@${DB}"
```
Expected: `ROLE_OPERATOR`.

- [ ] **Step 8: Append the manual permission checks to `database/09_test_scenarios.sql`**

Insert this comment block immediately **before** the final `EXIT` line, so readers can repeat Step 7 by hand:

```sql
-- ===== Kiểm tra phân quyền thủ công (không tự chạy) =====
-- Đăng nhập seller01/Seller#001 rồi chạy:
--   INSERT INTO transport.tickets (schedule_id, passenger_id, seat_no, price) VALUES (1, 1, 1, 1);
--     -> ORA-01031: insufficient privileges (chỉ bán vé được qua PKG_TICKET)
--   EXEC transport.pkg_staff.set_locked('SELLER02', 1)
--     -> PLS-00201: identifier 'TRANSPORT.PKG_STAFF' must be declared (seller không có quyền EXECUTE)
--   SELECT COUNT(*) FROM transport.drivers;
--     -> ORA-00942: table or view does not exist (seller không được xem tài xế)
```

- [ ] **Step 9: Verify Data Pump backup**

Run: `cd database && cmd //c backup.bat; cd .. && ls /d/OracleBackup`
Expected: a new `transport_YYYYMMDD_HHMM.dmp` and `.log`; the log ends with `successfully completed`.

- [ ] **Step 10: Commit**

```bash
git add database/08_seed_data.sql database/install_all.bat database/backup.bat database/restore.bat database/09_test_scenarios.sql
git commit -m "feat(db): add seed data, install and Data Pump scripts"
```
