WHENEVER SQLERROR EXIT FAILURE ROLLBACK
SET DEFINE OFF
SET SQLBLANKLINES ON
SET SERVEROUTPUT ON SIZE UNLIMITED

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

EXIT
