WHENEVER SQLERROR EXIT FAILURE ROLLBACK
SET DEFINE OFF
SET SQLBLANKLINES ON
SET SERVEROUTPUT ON SIZE UNLIMITED

-- Thống kê từng chuyến: ghế đã bán, ghế trống, tỷ lệ lấp đầy, vé hủy, doanh thu thực (đã trừ hoàn tiền).
-- Có thêm route_id, origin, destination, ticket_price để màn Bán vé lọc theo tuyến và khoảng giá;
-- vehicle_id, driver_id để màn Lịch chạy đọc từ view (seller không có quyền đọc bảng DRIVERS).
CREATE OR REPLACE VIEW v_trip_statistics AS
SELECT s.schedule_id,
       s.route_id,
       s.vehicle_id,
       s.driver_id,
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
 GROUP BY s.schedule_id, s.route_id, s.vehicle_id, s.driver_id, r.route_name, r.origin, r.destination,
          s.departure_time, s.arrival_time, s.ticket_price, s.status,
          v.plate_number, v.vehicle_type, d.full_name, v.seat_count;

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
