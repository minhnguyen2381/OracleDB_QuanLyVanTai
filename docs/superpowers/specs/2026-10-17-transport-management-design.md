# Thiết kế: Hệ thống quản lý vận tải và đặt vé xe (Đề tài 11 – HaUI)

**Ngày:** 2026-10-17
**Công nghệ:** C# WPF (.NET 8) + Oracle Database 21c XE (PDB `XEPDB1`)
**Hướng tiếp cận:** App mỏng – toàn bộ nghiệp vụ nằm trong Oracle (trigger, package, view, role)

## 0. Yêu cầu đề tài và nơi đáp ứng

| Yêu cầu | Đáp ứng |
|---|---|
| Quản lý tuyến xe | Bảng `ROUTES`, màn hình Tuyến xe |
| Quản lý phương tiện | Bảng `VEHICLES`, màn hình Phương tiện |
| Quản lý tài xế | Bảng `DRIVERS`, màn hình Tài xế |
| Quản lý lịch chạy | Bảng `SCHEDULES`, `PKG_SCHEDULE`, màn hình Lịch chạy |
| Quản lý hành khách | Bảng `PASSENGERS`, màn hình Hành khách |
| Đặt vé | Bảng `TICKETS`, `PKG_TICKET.BOOK_TICKET`, màn hình Bán vé (sơ đồ ghế) |
| Thanh toán | Bảng `PAYMENTS`, `PKG_TICKET.PAY_TICKET` |
| Trigger kiểm tra ghế đã đặt | `TRG_TICKETS_CHECK_SEAT` (+ khóa `FOR UPDATE` trong `BOOK_TICKET` chống tranh chấp) |
| Procedure đặt và hủy vé | `PKG_TICKET.BOOK_TICKET`, `PKG_TICKET.CANCEL_TICKET` |
| View thống kê chuyến xe | `V_TRIP_STATISTICS`, màn hình Thống kê + Tổng quan |
| Phân quyền điều hành / nhân viên bán vé | `ROLE_OPERATOR`, `ROLE_TICKET_SELLER`, user Oracle thật, `PKG_STAFF` |
| Sao lưu dữ liệu lịch chạy và đặt vé | `BACKUP_OPERATIONAL_DATA` + `JOB_NIGHTLY_BACKUP` + `expdp/impdp` |
| Bổ sung | In vé, xuất Excel, tìm chuyến theo khoảng giá, quản lý nhân viên trong app, đa ngôn ngữ VI/EN, tài liệu hướng dẫn sử dụng |

## 1. Quy ước

- Mọi định danh SQL (bảng, cột, constraint, index, trigger, package, procedure, view, role, job, giá trị trạng thái) **bằng tiếng Anh**.
- Nội dung thông báo lỗi `RAISE_APPLICATION_ERROR` **bằng tiếng Anh**. Frontend dịch theo mã lỗi.
- Chỉ comment trong SQL viết **tiếng Việt**.
- Dữ liệu người dùng nhập (tên khách, tên tuyến, loại xe...) là dữ liệu tự do, có thể tiếng Việt.
- Khóa chính dùng `GENERATED ALWAYS AS IDENTITY`.
- Thời điểm khởi hành/đến dùng `DATE`; thời điểm ghi nhận giao dịch dùng `TIMESTAMP`.

## 2. Kiến trúc tổng thể

```
┌───────────────────────────── WPF App (TransportApp) ─────────────────────────────┐
│ LoginWindow → MainWindow (sidebar + Frame) → Pages                               │
│ Db.cs (ADO.NET, Oracle.ManagedDataAccess.Core) │ Loc.cs (i18n + dịch mã lỗi)      │
│ ExcelExporter.cs (ClosedXML) │ TicketPrinter.cs (FlowDocument + PrintDialog)      │
└──────────────┬───────────────────────────────────────────────────────────────────┘
               │ đăng nhập bằng user Oracle của nhân viên (OPERATOR01, SELLER01…)
               │ ALTER SESSION SET CURRENT_SCHEMA = TRANSPORT
┌──────────────▼───────────── Oracle 21c XE / XEPDB1 ──────────────────────────────┐
│ ROLE_OPERATOR / ROLE_TICKET_SELLER  (GRANT trên object của TRANSPORT)            │
│ Schema TRANSPORT: bảng, trigger, PKG_TICKET, PKG_SCHEDULE, PKG_STAFF,            │
│                   V_TRIP_STATISTICS, bảng *_BK, BACKUP_LOGS, JOB_NIGHTLY_BACKUP  │
│ Directory BACKUP_DIR → D:\OracleBackup (expdp/impdp)                             │
└──────────────────────────────────────────────────────────────────────────────────┘
```

Nguyên tắc: app **không bao giờ** INSERT/UPDATE trực tiếp vào `TICKETS`, `PAYMENTS`, `STAFF` – chỉ gọi package. Hai role cũng không có quyền DML trên các bảng này, nên Oracle tự chặn mọi đường vòng.

## 3. Lược đồ CSDL

```
ROUTES ──< SCHEDULES >── VEHICLES
               │    └───< DRIVERS
               │
            TICKETS >── PASSENGERS
               │
            PAYMENTS

STAFF (danh sách nhân viên = user Oracle)     BACKUP_LOGS ──< SCHEDULES_BK / TICKETS_BK / PAYMENTS_BK
```

### 3.1 `02_tables.sql` (chạy bằng TRANSPORT)

```sql
-- Tuyến xe
CREATE TABLE routes (
  route_id     NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  route_name   VARCHAR2(100) NOT NULL CONSTRAINT uq_routes_name UNIQUE,
  origin       VARCHAR2(50)  NOT NULL,                 -- Điểm đi
  destination  VARCHAR2(50)  NOT NULL,                 -- Điểm đến
  distance_km  NUMBER(5)     CONSTRAINT ck_routes_distance CHECK (distance_km > 0),
  duration_min NUMBER(4)     CONSTRAINT ck_routes_duration CHECK (duration_min > 0),
  base_price   NUMBER(12)    NOT NULL CONSTRAINT ck_routes_price CHECK (base_price > 0),
  CONSTRAINT ck_routes_endpoints CHECK (origin <> destination)
);

-- Phương tiện
CREATE TABLE vehicles (
  vehicle_id   NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  plate_number VARCHAR2(15) NOT NULL CONSTRAINT uq_vehicles_plate UNIQUE,  -- Biển số, VD 29B-196.88
  vehicle_type VARCHAR2(50) NOT NULL,                                     -- VD: Giường nằm 40 chỗ
  seat_count   NUMBER(2)    NOT NULL CONSTRAINT ck_vehicles_seats CHECK (seat_count BETWEEN 1 AND 60),
  status       VARCHAR2(12) DEFAULT 'ACTIVE' NOT NULL
               CONSTRAINT ck_vehicles_status CHECK (status IN ('ACTIVE','MAINTENANCE','RETIRED'))
);

-- Tài xế
CREATE TABLE drivers (
  driver_id      NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  full_name      VARCHAR2(100) NOT NULL,
  id_card        VARCHAR2(12)  NOT NULL CONSTRAINT uq_drivers_id_card UNIQUE,   -- CCCD
  phone          VARCHAR2(15)  NOT NULL,
  license_no     VARCHAR2(20)  NOT NULL CONSTRAINT uq_drivers_license UNIQUE,
  -- D1/D2/D theo Luật TTATGTĐB 2024, E là hạng bằng cũ còn hiệu lực
  license_class  VARCHAR2(2)   NOT NULL CONSTRAINT ck_drivers_class CHECK (license_class IN ('D1','D2','D','E')),
  license_expiry DATE          NOT NULL,
  status         VARCHAR2(10)  DEFAULT 'ACTIVE' NOT NULL
                 CONSTRAINT ck_drivers_status CHECK (status IN ('ACTIVE','INACTIVE'))
);

-- Lịch chạy (chuyến xe)
CREATE TABLE schedules (
  schedule_id    NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  route_id       NUMBER     NOT NULL CONSTRAINT fk_schedules_route   REFERENCES routes(route_id),
  vehicle_id     NUMBER     NOT NULL CONSTRAINT fk_schedules_vehicle REFERENCES vehicles(vehicle_id),
  driver_id      NUMBER     NOT NULL CONSTRAINT fk_schedules_driver  REFERENCES drivers(driver_id),
  departure_time DATE       NOT NULL,
  arrival_time   DATE       NOT NULL,
  ticket_price   NUMBER(12) NOT NULL CONSTRAINT ck_schedules_price CHECK (ticket_price > 0),
  status         VARCHAR2(10) DEFAULT 'SCHEDULED' NOT NULL
                 CONSTRAINT ck_schedules_status CHECK (status IN ('SCHEDULED','RUNNING','COMPLETED','CANCELLED')),
  CONSTRAINT ck_schedules_time CHECK (arrival_time > departure_time)
);
CREATE INDEX idx_schedules_departure ON schedules(departure_time);
CREATE INDEX idx_schedules_route     ON schedules(route_id);

-- Hành khách (tìm theo SĐT khi bán vé)
CREATE TABLE passengers (
  passenger_id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  full_name    VARCHAR2(100) NOT NULL,
  phone        VARCHAR2(15)  NOT NULL CONSTRAINT uq_passengers_phone UNIQUE,
  id_card      VARCHAR2(12),
  email        VARCHAR2(100),
  created_at   TIMESTAMP DEFAULT SYSTIMESTAMP NOT NULL
);

-- Vé xe: mỗi dòng là một ghế trên một chuyến
CREATE TABLE tickets (
  ticket_id     NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  schedule_id   NUMBER NOT NULL CONSTRAINT fk_tickets_schedule  REFERENCES schedules(schedule_id),
  passenger_id  NUMBER NOT NULL CONSTRAINT fk_tickets_passenger REFERENCES passengers(passenger_id),
  seat_no       NUMBER(2)  NOT NULL,
  price         NUMBER(12) NOT NULL CONSTRAINT ck_tickets_price CHECK (price > 0),  -- Giá chốt lúc bán
  booked_at     TIMESTAMP DEFAULT SYSTIMESTAMP NOT NULL,
  sold_by       VARCHAR2(30) DEFAULT USER NOT NULL,    -- User Oracle của nhân viên bán vé
  status        VARCHAR2(10) DEFAULT 'BOOKED' NOT NULL
                CONSTRAINT ck_tickets_status CHECK (status IN ('BOOKED','PAID','CANCELLED')),
  cancelled_at  TIMESTAMP,
  cancel_reason VARCHAR2(200),
  CONSTRAINT ck_tickets_cancel CHECK (status <> 'CANCELLED' OR cancelled_at IS NOT NULL)
);
CREATE INDEX idx_tickets_schedule  ON tickets(schedule_id);
CREATE INDEX idx_tickets_passenger ON tickets(passenger_id);
-- Không dùng unique index cho (schedule_id, seat_no): index sẽ báo ORA-00001 trước khi trigger kịp kiểm tra.
-- Tranh chấp 2 quầy cùng lúc được chặn bằng FOR UPDATE trên dòng lịch chạy trong PKG_TICKET.BOOK_TICKET,
-- và không role nào có quyền INSERT trực tiếp vào TICKETS.

-- Thanh toán và hoàn tiền (số tiền luôn dương, chiều tiền xác định bởi payment_type)
CREATE TABLE payments (
  payment_id   NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  ticket_id    NUMBER NOT NULL CONSTRAINT fk_payments_ticket REFERENCES tickets(ticket_id),
  amount       NUMBER(12) NOT NULL CONSTRAINT ck_payments_amount CHECK (amount > 0),
  method       VARCHAR2(10) NOT NULL CONSTRAINT ck_payments_method CHECK (method IN ('CASH','TRANSFER','CARD')),
  payment_type VARCHAR2(10) NOT NULL CONSTRAINT ck_payments_type   CHECK (payment_type IN ('PAYMENT','REFUND')),
  paid_at      TIMESTAMP DEFAULT SYSTIMESTAMP NOT NULL,
  received_by  VARCHAR2(30) DEFAULT USER NOT NULL
);
CREATE INDEX idx_payments_ticket ON payments(ticket_id);

-- Nhân viên: mỗi dòng ứng với một user Oracle; cũng là danh sách trắng cho PKG_STAFF
CREATE TABLE staff (
  username   VARCHAR2(30) PRIMARY KEY,
  full_name  VARCHAR2(100) NOT NULL,
  phone      VARCHAR2(15),
  role_name  VARCHAR2(30) NOT NULL CONSTRAINT ck_staff_role CHECK (role_name IN ('ROLE_OPERATOR','ROLE_TICKET_SELLER')),
  status     VARCHAR2(10) DEFAULT 'ACTIVE' NOT NULL CONSTRAINT ck_staff_status CHECK (status IN ('ACTIVE','LOCKED')),
  created_at TIMESTAMP DEFAULT SYSTIMESTAMP NOT NULL,
  created_by VARCHAR2(30) DEFAULT USER NOT NULL
);
```

Ghi chú thiết kế:
- Ghế là số 1..`vehicles.seat_count`; không có bảng ghế riêng. Sơ đồ ghế vẽ từ con số này.
- Hủy vé không xóa dòng mà chuyển `CANCELLED` để giữ lịch sử thống kê.
- Không xóa nhân viên, chỉ khóa (vì `sold_by`, `received_by` còn tham chiếu username).

## 4. Trigger – `03_triggers.sql`

### 4.1 `TRG_TICKETS_CHECK_SEAT` – kiểm tra ghế đã đặt

Dạng compound trigger để tránh ORA-04091 (mutating table) khi đọc lại bảng `TICKETS`.

```sql
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
```

### 4.2 `TRG_SCHEDULES_CHECK_CANCEL` – chặn hủy chuyến khi còn vé

Điều hành có quyền UPDATE `SCHEDULES`; trigger này buộc việc hủy chuyến có vé phải đi qua `PKG_SCHEDULE.CANCEL_SCHEDULE` (để hoàn tiền).

```sql
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
```

## 5. Package – `04_packages.sql`

Mỗi procedure tự `COMMIT` khi thành công, `ROLLBACK` + ném lại lỗi khi thất bại. Các package chạy theo quyền chủ sở hữu (definer rights); hàm `USER` vẫn trả về user đang đăng nhập nên `sold_by`, `received_by`, `created_by` ghi đúng nhân viên.

### 5.1 `PKG_TICKET` – đặt, thanh toán, hủy vé

```sql
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
```

### 5.2 `PKG_SCHEDULE` – hủy chuyến (chỉ điều hành)

```sql
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
```

### 5.3 `PKG_STAFF` – quản lý nhân viên (tạo user Oracle trong app)

TRANSPORT được SYS cấp trực tiếp `CREATE USER`, `ALTER USER` và hai role kèm `WITH ADMIN OPTION` (xem mục 7.1). Package chỉ cho thao tác trên user có trong bảng `STAFF` nên không thể đụng tới SYS/TRANSPORT. Lệnh DDL trong PL/SQL tự commit nên thứ tự các bước được sắp để lỗi giữa chừng không để lại user mồ côi.

```sql
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
```

## 6. View thống kê – `05_views.sql`

```sql
-- Thống kê từng chuyến: ghế đã bán, ghế trống, tỷ lệ lấp đầy, vé hủy, doanh thu thực (đã trừ hoàn tiền).
-- Có thêm route_id, origin, destination, ticket_price để màn Bán vé lọc theo tuyến và khoảng giá.
CREATE OR REPLACE VIEW v_trip_statistics AS
SELECT s.schedule_id,
       s.route_id,
       r.route_name,
       r.origin,
       r.destination,
       s.departure_time,
       s.arrival_time,
       s.ticket_price,
       s.status                                                AS schedule_status,
       v.plate_number,
       v.vehicle_type,
       d.full_name                                             AS driver_name,
       v.seat_count,
       COUNT(CASE WHEN t.status <> 'CANCELLED' THEN 1 END)     AS seats_sold,
       v.seat_count - COUNT(CASE WHEN t.status <> 'CANCELLED' THEN 1 END) AS seats_available,
       ROUND(100 * COUNT(CASE WHEN t.status <> 'CANCELLED' THEN 1 END) / v.seat_count, 1) AS occupancy_pct,
       COUNT(CASE WHEN t.status = 'CANCELLED' THEN 1 END)      AS tickets_cancelled,
       NVL(SUM(p.net_amount), 0)                               AS revenue
  FROM schedules s
  JOIN routes   r ON r.route_id   = s.route_id
  JOIN vehicles v ON v.vehicle_id = s.vehicle_id
  JOIN drivers  d ON d.driver_id  = s.driver_id
  LEFT JOIN tickets t ON t.schedule_id = s.schedule_id
  -- Gộp thanh toán theo vé trước khi join để doanh thu không bị nhân đôi
  LEFT JOIN (SELECT ticket_id,
                    SUM(CASE payment_type WHEN 'PAYMENT' THEN amount ELSE -amount END) AS net_amount
               FROM payments
              GROUP BY ticket_id) p ON p.ticket_id = t.ticket_id
 GROUP BY s.schedule_id, s.route_id, r.route_name, r.origin, r.destination,
          s.departure_time, s.arrival_time, s.ticket_price, s.status,
          v.plate_number, v.vehicle_type, d.full_name, v.seat_count;
```

## 7. Phân quyền

### 7.1 `01_sys_setup.sql` (chạy bằng SYS trong `XEPDB1`)

```sql
-- Chủ schema: chỉ dùng để tạo/bảo trì object, app không đăng nhập bằng user này
CREATE USER transport IDENTIFIED BY "Transport#2026" QUOTA UNLIMITED ON users;
GRANT CREATE SESSION, CREATE TABLE, CREATE VIEW, CREATE PROCEDURE,
      CREATE TRIGGER, CREATE SEQUENCE, CREATE JOB TO transport;
-- Quyền hệ thống cấp trực tiếp (không qua role) để PKG_STAFF dùng được trong definer rights
GRANT CREATE USER, ALTER USER, DROP USER TO transport;

CREATE ROLE role_operator;
CREATE ROLE role_ticket_seller;
GRANT CREATE SESSION TO role_operator, role_ticket_seller;
-- Cho TRANSPORT quyền cấp/thu hồi hai role này (PKG_STAFF cần)
GRANT role_operator, role_ticket_seller TO transport WITH ADMIN OPTION;

-- Thư mục cho Data Pump
CREATE OR REPLACE DIRECTORY backup_dir AS 'D:\OracleBackup';
GRANT READ, WRITE ON DIRECTORY backup_dir TO transport;
```

Lưu ý bảo mật: TRANSPORT có `ALTER USER` (đổi được mật khẩu bất kỳ user nào), nên mật khẩu TRANSPORT chỉ DBA giữ; app không bao giờ dùng tài khoản này. `DROP USER` chỉ dùng để dọn user mồ côi trong `create_staff`.

### 7.2 Ma trận quyền

| Đối tượng | ROLE_OPERATOR (điều hành) | ROLE_TICKET_SELLER (bán vé) |
|---|---|---|
| routes, schedules | SELECT, INSERT, UPDATE, DELETE | SELECT |
| vehicles | SELECT, INSERT, UPDATE, DELETE | SELECT |
| drivers | SELECT, INSERT, UPDATE, DELETE | — |
| passengers | SELECT, INSERT, UPDATE, DELETE | SELECT, INSERT, UPDATE |
| tickets, payments | SELECT | SELECT |
| staff | SELECT | SELECT (hiện tên người đăng nhập) |
| v_trip_statistics | SELECT | SELECT |
| backup_logs, v_backup_job_status | SELECT | — |
| pkg_ticket | EXECUTE | EXECUTE |
| pkg_schedule, pkg_staff, backup_operational_data | EXECUTE | — |

### 7.3 `07_grants.sql` (chạy bằng TRANSPORT)

```sql
-- Điều hành: quản lý danh mục, lịch chạy, nhân viên, sao lưu
GRANT SELECT, INSERT, UPDATE, DELETE ON routes     TO role_operator;
GRANT SELECT, INSERT, UPDATE, DELETE ON vehicles   TO role_operator;
GRANT SELECT, INSERT, UPDATE, DELETE ON drivers    TO role_operator;
GRANT SELECT, INSERT, UPDATE, DELETE ON schedules  TO role_operator;
GRANT SELECT, INSERT, UPDATE, DELETE ON passengers TO role_operator;
GRANT SELECT ON tickets           TO role_operator;
GRANT SELECT ON payments          TO role_operator;
GRANT SELECT ON staff             TO role_operator;
GRANT SELECT ON v_trip_statistics TO role_operator;
GRANT SELECT ON backup_logs       TO role_operator;
GRANT SELECT ON v_backup_job_status TO role_operator;
GRANT EXECUTE ON pkg_ticket              TO role_operator;
GRANT EXECUTE ON pkg_schedule            TO role_operator;
GRANT EXECUTE ON pkg_staff               TO role_operator;
GRANT EXECUTE ON backup_operational_data TO role_operator;

-- Bán vé: xem lịch, quản lý khách, bán/thanh toán/hủy vé qua package
GRANT SELECT ON routes            TO role_ticket_seller;
GRANT SELECT ON vehicles          TO role_ticket_seller;
GRANT SELECT ON schedules         TO role_ticket_seller;
GRANT SELECT, INSERT, UPDATE ON passengers TO role_ticket_seller;
GRANT SELECT ON tickets           TO role_ticket_seller;
GRANT SELECT ON payments          TO role_ticket_seller;
GRANT SELECT ON staff             TO role_ticket_seller;
GRANT SELECT ON v_trip_statistics TO role_ticket_seller;
GRANT EXECUTE ON pkg_ticket       TO role_ticket_seller;
```

App xác định quyền bằng `SELECT role FROM session_roles` sau khi đăng nhập.

## 8. Sao lưu

### 8.1 `06_backup.sql` – bảng backup + procedure + job (chạy bằng TRANSPORT)

```sql
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
```

Màn hình Sao lưu đọc trạng thái job từ `user_scheduler_jobs` qua một view nhỏ để điều hành xem được:

```sql
-- Trạng thái job sao lưu đêm (điều hành không có quyền đọc user_scheduler_jobs của TRANSPORT)
CREATE OR REPLACE VIEW v_backup_job_status AS
SELECT job_name, enabled, state, last_start_date, next_run_date, failure_count
  FROM user_scheduler_jobs
 WHERE job_name = 'JOB_NIGHTLY_BACKUP';
```

### 8.2 Data Pump – `backup.bat`, `restore.bat`

Xuất toàn bộ schema TRANSPORT ra `.dmp`; đây là cách khôi phục thật. Bảng `*_bk` dùng để xem lại/đối chiếu, không có procedure khôi phục từ `*_bk` (khóa chính `GENERATED ALWAYS` không cho chèn lại ID cũ; `impdp` đã lo khôi phục).

```bat
:: backup.bat – xuất schema TRANSPORT, tên file kèm thời gian
@echo off
for /f %%i in ('powershell -NoProfile -Command "Get-Date -Format yyyyMMdd_HHmm"') do set TS=%%i
expdp transport/"Transport#2026"@localhost:1521/XEPDB1 schemas=TRANSPORT directory=BACKUP_DIR dumpfile=transport_%TS%.dmp logfile=export_%TS%.log
```

```bat
:: restore.bat <tên_file.dmp> – khôi phục, ghi đè các bảng hiện có
@echo off
if "%~1"=="" (echo Usage: restore.bat transport_YYYYMMDD_HHMM.dmp & exit /b 1)
impdp transport/"Transport#2026"@localhost:1521/XEPDB1 schemas=TRANSPORT directory=BACKUP_DIR dumpfile=%1 table_exists_action=REPLACE
```

## 9. Bảng mã lỗi (hợp đồng giữa SQL và frontend)

| Mã | Nguồn | Message |
|---|---|---|
| -20001 | trg_tickets_check_seat | `Seat {n} does not exist (vehicle has {cap} seats).` |
| -20002 | trg_tickets_check_seat | `Schedule is not open for sale (status {s}).` |
| -20003 | trg_tickets_check_seat | `Seat {n} is already booked.` |
| -20010 | pkg_ticket.book_ticket | `Schedule has already departed; booking not allowed.` |
| -20011 | pkg_ticket.book_ticket | `Schedule not found.` |
| -20020 | pkg_ticket.pay_ticket | `Only BOOKED tickets can be paid.` |
| -20021 | pkg_ticket.pay_ticket | `Ticket not found.` |
| -20030 | pkg_ticket.cancel_ticket | `Ticket is already cancelled.` |
| -20031 | pkg_ticket.cancel_ticket | `Schedule has already departed; cancellation not allowed.` |
| -20032 | pkg_ticket.cancel_ticket | `Ticket not found.` |
| -20040 | pkg_staff | `Invalid username.` |
| -20041 | pkg_staff | `Invalid role.` |
| -20042 | pkg_staff | `Staff not found.` |
| -20043 | pkg_staff | `You cannot lock or change your own account.` |
| -20044 | pkg_staff | `Password must be at least 8 characters and must not contain double quotes.` |
| -20050 | pkg_schedule.cancel_schedule | `Schedule not found.` |
| -20051 | pkg_schedule.cancel_schedule | `Only SCHEDULED trips can be cancelled.` |
| -20052 | trg_schedules_check_cancel | `Schedule has active tickets; use PKG_SCHEDULE.CANCEL_SCHEDULE.` |

Lỗi Oracle hệ thống được frontend dịch thêm:

| ORA | Hiển thị |
|---|---|
| 1017 | Sai tài khoản hoặc mật khẩu |
| 28000 | Tài khoản đã bị khóa |
| 1031 | Bạn không có quyền thực hiện thao tác này |
| 1 | Dữ liệu bị trùng (biển số, SĐT, CCCD, số bằng lái…) |
| 2292 | Không xóa được vì đang có dữ liệu liên quan |
| 1920 | Tên đăng nhập đã tồn tại |
| 12541, 12514 | Không kết nối được Oracle, kiểm tra service |

## 10. Dữ liệu mẫu – `08_seed_data.sql` (chạy bằng TRANSPORT)

- 5 tuyến: Hà Nội – Lào Cai, Hà Nội – Hải Phòng, Hà Nội – Ninh Bình, Hà Nội – Sa Pa, Hà Nội – Thanh Hóa.
- 6 xe (có **29B-196.88**), sức chứa 16/29/40/45; 1 xe `MAINTENANCE`.
- 6 tài xế, 1 tài xế bằng lái hết hạn trong < 30 ngày (để thấy cảnh báo).
- 30 hành khách.
- ~20 lịch chạy trong 7 ngày tới (`SCHEDULED`) + ~10 chuyến trong 7 ngày qua.
- Vé + thanh toán cho các chuyến đã qua: chèn vé khi chuyến còn `SCHEDULED`, sau đó cập nhật chuyến sang `COMPLETED` (để không bị `trg_tickets_check_seat` chặn); có vài vé `CANCELLED` kèm `REFUND`.
- Một số vé `BOOKED`/`PAID` trên chuyến tương lai để sơ đồ ghế có ghế đỏ.
- Nhân viên demo tạo bằng chính package:

```sql
BEGIN
  pkg_staff.create_staff('operator01', 'Operator#01', 'Nguyễn Văn Điều', '0912000001', 'ROLE_OPERATOR');
  pkg_staff.create_staff('seller01',   'Seller#001',  'Trần Thị Bán',    '0912000002', 'ROLE_TICKET_SELLER');
  pkg_staff.create_staff('seller02',   'Seller#002',  'Lê Văn Vé',       '0912000003', 'ROLE_TICKET_SELLER');
END;
/
```

## 11. Cài đặt và gỡ cài đặt

- `install_all.bat`: hỏi mật khẩu SYS, chạy `01_sys_setup.sql` bằng SYS, rồi `02` → `08` bằng TRANSPORT qua `sqlplus`, dừng khi có lỗi (`WHENEVER SQLERROR EXIT FAILURE` ở đầu mỗi file).
- `00_uninstall.sql` (SYS): drop các user có trong `transport.staff`, `DROP USER transport CASCADE`, drop 2 role, drop directory – để cài lại sạch.

## 12. Desktop App (WPF)

### 12.1 Công nghệ

- .NET 8 WPF, code-behind + `DataTable` bind vào `DataGrid` (không dùng framework MVVM – app mỏng).
- NuGet: `Oracle.ManagedDataAccess.Core`, `MaterialDesignThemes`, `ScottPlot.WPF`, `ClosedXML`.
- Màu: navy HaUI `#0B3D91` (sidebar, primary), vàng `#F5B700` (accent), nền `#F4F6FA`.

### 12.2 Cấu trúc

```
src/TransportApp/
├── App.xaml / App.xaml.cs        -- Theme MaterialDesign, nạp Strings.vi.xaml mặc định
├── appsettings.json              -- { "DataSource": "localhost:1521/XEPDB1", "Language": "vi" }
├── Data/
│   ├── Db.cs                     -- Kết nối duy nhất của phiên; Query(), Exec(), ExecProc()
│   └── Session.cs                -- Username, FullName, IsOperator
├── Localization/
│   ├── Strings.vi.xaml
│   ├── Strings.en.xaml
│   └── Loc.cs                    -- SetLanguage(), T(key), Error(OracleException)
├── Services/
│   ├── ExcelExporter.cs          -- Export(DataTable, title) → SaveFileDialog → .xlsx
│   └── TicketPrinter.cs          -- Dựng FlowDocument vé → PrintDialog
└── Views/
    ├── LoginWindow.xaml
    ├── MainWindow.xaml           -- Sidebar + top bar + Frame + Snackbar
    └── Pages/
        DashboardPage, SellTicketPage, TicketsPage, PassengersPage, RoutesPage,
        VehiclesPage, DriversPage, SchedulesPage, StatisticsPage, StaffPage, BackupPage
```

`Db.cs` là nơi duy nhất nói chuyện với Oracle. Kết nối mở lúc đăng nhập, giữ suốt phiên, sau khi mở chạy `ALTER SESSION SET CURRENT_SCHEMA = TRANSPORT`. Mọi câu SQL dùng bind parameter (`:p_name`), không nối chuỗi.

### 12.3 Màn hình

| # | Màn hình | Điều hành | Bán vé | Nội dung |
|---|---|---|---|---|
| 1 | Đăng nhập | ✔ | ✔ | Username/password Oracle, chọn VI/EN |
| 2 | Tổng quan | ✔ | ✔ | 4 thẻ hôm nay (số chuyến, vé bán, doanh thu, tỷ lệ lấp đầy TB) + biểu đồ cột doanh thu 7 ngày (từ `v_trip_statistics`) |
| 3 | Bán vé | ✔ | ✔ | Lọc: điểm đi, điểm đến, ngày, **giá từ/đến** → danh sách chuyến (giờ, xe, giá, ghế trống). Chọn chuyến → **sơ đồ ghế** (4 ghế/hàng, lối đi giữa; xanh lá trống, đỏ đã đặt, xanh dương đang chọn). Form khách: nhập SĐT → tự điền nếu đã có. Nút **Đặt vé** → `book_ticket`; hộp thoại **Thanh toán** (CASH/TRANSFER/CARD) → `pay_ticket` → hỏi **In vé** |
| 4 | Quản lý vé | ✔ | ✔ | Tìm theo mã vé / SĐT / chuyến / trạng thái. Nút Thanh toán, Hủy (bắt buộc lý do), **In vé**, **Xuất Excel** |
| 5 | Hành khách | ✔ | ✔ (không xóa) | CRUD, tìm kiếm, lịch sử vé của khách, Xuất Excel |
| 6 | Tuyến xe | ✔ | chỉ xem | CRUD, Xuất Excel |
| 7 | Phương tiện | ✔ | ẩn | CRUD, đổi trạng thái, Xuất Excel |
| 8 | Tài xế | ✔ | ẩn | CRUD, tô vàng bằng lái hết hạn < 30 ngày, tô đỏ đã hết hạn, Xuất Excel |
| 9 | Lịch chạy | ✔ | chỉ xem | CRUD; combobox chỉ liệt kê xe `ACTIVE` và tài xế `ACTIVE` còn hạn bằng tại ngày chạy; giá vé mặc định lấy `base_price` của tuyến; giờ đến mặc định = giờ đi + `duration_min`; nút **Hủy chuyến** → `pkg_schedule.cancel_schedule`; đổi trạng thái RUNNING/COMPLETED; Xuất Excel |
| 10 | Thống kê | ✔ | ✔ | Lọc khoảng ngày + tuyến; DataGrid `v_trip_statistics`; tổng doanh thu, tổng vé; biểu đồ cột doanh thu theo tuyến; Xuất Excel |
| 11 | Nhân viên | ✔ | ẩn | Danh sách `staff`; Thêm (username, mật khẩu, họ tên, SĐT, role) → `create_staff`; Sửa thông tin; Đổi role; Đặt lại mật khẩu; Khóa/Mở khóa; Xuất Excel |
| 12 | Sao lưu | ✔ | ẩn | Nút **Sao lưu ngay** → `backup_operational_data`; bảng `backup_logs`; trạng thái job từ `v_backup_job_status`; hướng dẫn ngắn dùng `backup.bat`/`restore.bat` |

Bố cục: sidebar trái navy (logo + menu, mục không có quyền bị ẩn); thanh trên cùng: tên trang, họ tên + nhãn role, nút đổi ngôn ngữ, nút đăng xuất; thông báo dùng Snackbar.

### 12.4 In vé

`TicketPrinter.Print(ticketId)` truy vấn vé (join chuyến, tuyến, xe, khách), dựng `FlowDocument` khổ A6 dọc gồm: tiêu đề "VÉ XE KHÁCH", mã vé lớn, tuyến, giờ khởi hành, biển số, số ghế, tên + SĐT khách, giá, trạng thái thanh toán, người bán, thời điểm in. Gọi `PrintDialog` – chọn "Microsoft Print to PDF" để ra file. Văn bản trên vé theo ngôn ngữ đang chọn.

### 12.5 Xuất Excel

`ExcelExporter.Export(DataTable dt, string title)`: `SaveFileDialog` → ClosedXML: dòng 1 tiêu đề in đậm, dòng 2 thời điểm xuất + người xuất, từ dòng 4 là bảng (`InsertTable`), định dạng cột số tiền `#,##0`, cột ngày `dd/MM/yyyy HH:mm`, `AdjustToContents()`. Tiêu đề cột dùng chuỗi đã dịch.

### 12.6 Đa ngôn ngữ

- `Strings.vi.xaml`, `Strings.en.xaml` (ResourceDictionary). XAML dùng `{DynamicResource Key}` nên đổi ngôn ngữ có hiệu lực ngay, không cần khởi động lại.
- `Loc.Error(OracleException ex)`: tra khóa `Err_{ex.Number}` (VD `Err_20003` = "Ghế {0} đã có người đặt."); tham số (số ghế…) do màn hình truyền vào vì màn hình đã biết; không có khóa → hiện nguyên message tiếng Anh của Oracle (bỏ tiền tố `ORA-xxxxx:`).
- Ngôn ngữ mặc định đọc từ `appsettings.json`; chọn ở màn đăng nhập hoặc nút trên top bar.

## 13. Kiểm thử

- `09_test_scenarios.sql` (TRANSPORT): tự tạo 1 chuyến test ở tương lai trên xe 16 chỗ, chạy các kịch bản, in `PASS`/`FAIL` bằng `DBMS_OUTPUT`, cuối cùng dọn dữ liệu test:
  1. Đặt vé hợp lệ → có vé `BOOKED`, `sold_by = USER`.
  2. Đặt trùng ghế → lỗi -20003.
  3. Đặt ghế 17 trên xe 16 chỗ → lỗi -20001.
  4. Thanh toán → vé `PAID`, có 1 dòng `PAYMENT`; thanh toán lần 2 → -20020.
  5. Hủy vé đã thanh toán → vé `CANCELLED`, có dòng `REFUND`, `revenue` của chuyến trong view = 0.
  6. Hủy lần 2 → -20030.
  7. Ghế đã hủy đặt lại được.
  8. UPDATE trực tiếp chuyến còn vé sang `CANCELLED` → -20052; `cancel_schedule` → mọi vé `CANCELLED`, có `REFUND`.
  9. `backup_operational_data` → số dòng `*_bk` của lần sao lưu mới bằng số dòng bảng gốc; `backup_logs` có dòng mới.
  10. `pkg_staff.create_staff` với username `'x; drop'` → -20040; role sai → -20041; mật khẩu ngắn → -20044.
- Cuối file: hướng dẫn kiểm tra phân quyền thủ công – đăng nhập `seller01`, chạy `INSERT INTO transport.tickets ...` → ORA-01031; `EXEC transport.pkg_staff.set_locked('SELLER02',1)` → ORA-01031 (seller không có quyền).
- App: checklist thao tác tay cho từng màn hình (trong plan), chạy với cả `operator01` và `seller01`.

## 14. Tài liệu hướng dẫn sử dụng

`docs/user-guide.md` (tiếng Việt) + ảnh trong `docs/images/` chụp từ app thật:
1. Yêu cầu hệ thống.
2. Cài Oracle 21c XE, kiểm tra `XEPDB1`.
3. Cài CSDL bằng `install_all.bat`; tạo thư mục `D:\OracleBackup`.
4. Cấu hình `appsettings.json`, chạy app (`dotnet run` hoặc file `.exe` đã publish).
5. Bảng tài khoản demo (`operator01`/`Operator#01`, `seller01`/`Seller#001`, `seller02`/`Seller#002`) và quyền của từng loại.
6. Hướng dẫn từng màn hình (mục 12.3) kèm ảnh.
7. Quy trình bán vé đầy đủ: tìm chuyến → chọn ghế → đặt → thanh toán → in vé; hủy vé/hoàn tiền.
8. Quản lý nhân viên.
9. Sao lưu và khôi phục (nút trong app, job đêm, `backup.bat`/`restore.bat`).
10. Xử lý lỗi thường gặp (bảng lỗi ở mục 9).

## 15. Cấu trúc repo

```
OracleDB_QuanLyVanTai/
├── database/
│   ├── 00_uninstall.sql
│   ├── 01_sys_setup.sql
│   ├── 02_tables.sql
│   ├── 03_triggers.sql
│   ├── 04_packages.sql
│   ├── 05_views.sql
│   ├── 06_backup.sql
│   ├── 07_grants.sql
│   ├── 08_seed_data.sql
│   ├── 09_test_scenarios.sql
│   ├── install_all.bat
│   ├── backup.bat
│   └── restore.bat
├── src/TransportApp/        (mục 12.2)
├── docs/
│   ├── user-guide.md
│   ├── images/
│   └── superpowers/specs/2026-10-17-transport-management-design.md
├── .gitignore
└── README.md               -- giới thiệu ngắn + link user-guide
```

## 16. Ngoài phạm vi

- Chặn một xe/tài xế bị xếp vào hai chuyến trùng giờ (cần compound trigger trên `SCHEDULES`; đề tài không yêu cầu).
- Chặn đổi xe của chuyến sang xe ít ghế hơn số ghế đã bán.
- Chính sách hoàn tiền theo thời gian (hiện hoàn 100%).
- Khôi phục từ bảng `*_bk` (khôi phục thật dùng `impdp`).
- Khách tự đặt vé online – app chỉ dành cho nhân viên tại quầy.
