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
    v_veh   NUMBER := v_vehicles(MOD(v_k, v_vehicles.COUNT) + 1);
    v_drv   NUMBER := v_drivers(MOD(v_k, v_drivers.COUNT) + 1);
    v_id    NUMBER;
  BEGIN
    SELECT duration_min, base_price INTO v_dur, v_price FROM routes WHERE route_id = v_route;
    INSERT INTO schedules (route_id, vehicle_id, driver_id, departure_time, arrival_time, ticket_price)
    VALUES (v_route, v_veh, v_drv, p_dep, p_dep + v_dur / 1440, v_price)
    RETURNING schedule_id INTO v_id;
    v_k := v_k + 1;
    RETURN v_id;
  END add_schedule;

  -- Bán p_count vé (ghế 1..p_count); p_paid = 1 thì ghi thanh toán; mỗi vé thứ 5 bị hủy + hoàn tiền nếu p_with_cancel = 1
  PROCEDURE sell(p_sched NUMBER, p_count NUMBER, p_paid NUMBER, p_with_cancel NUMBER, p_booked_at DATE) IS
    v_tp            NUMBER;
    v_cancelled     BOOLEAN;
    v_status        VARCHAR2(20);
    v_cancel_time   DATE;
    v_cancel_reason VARCHAR2(200);
    v_p_id          NUMBER;
    v_method        VARCHAR2(20);
  BEGIN
    -- INSERT ... SELECT không hỗ trợ RETURNING INTO nên lấy giá vé trước rồi INSERT ... VALUES
    SELECT ticket_price INTO v_tp FROM schedules WHERE schedule_id = p_sched;
    FOR s IN 1 .. p_count LOOP
      v_seller    := CASE WHEN MOD(s, 2) = 0 THEN 'SELLER01' ELSE 'SELLER02' END;
      v_cancelled := p_with_cancel = 1 AND MOD(s, 5) = 0;
      IF v_cancelled THEN
        v_status        := 'CANCELLED';
        v_cancel_time   := p_booked_at + 1;
        v_cancel_reason := 'Khách đổi kế hoạch';
      ELSIF p_paid = 1 THEN
        v_status        := 'PAID';
        v_cancel_time   := NULL;
        v_cancel_reason := NULL;
      ELSE
        v_status        := 'BOOKED';
        v_cancel_time   := NULL;
        v_cancel_reason := NULL;
      END IF;

      v_p_id := v_pass(MOD(p_sched * 7 + s, v_pass.COUNT) + 1);

      INSERT INTO tickets (schedule_id, passenger_id, seat_no, price, booked_at, sold_by, status, cancelled_at, cancel_reason)
      VALUES (p_sched, v_p_id, s, v_tp, p_booked_at, v_seller,
              v_status, v_cancel_time, v_cancel_reason)
      RETURNING ticket_id INTO v_ticket;
      IF p_paid = 1 THEN
        v_method := v_methods(MOD(s, 3) + 1);
        INSERT INTO payments (ticket_id, amount, method, payment_type, paid_at, received_by)
        VALUES (v_ticket, v_tp, v_method, 'PAYMENT', p_booked_at, v_seller);
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
