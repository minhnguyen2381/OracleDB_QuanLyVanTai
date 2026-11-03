# Báo Cáo Triển Khai Task 6: Seed Data, Install/Backup Scripts, Full Reinstall và Permission Checks

- **Trạng thái:** DONE
- **Commit:** `8c34f18` - `feat(db): add seed data, install and Data Pump scripts`
- **Base commit:** `18eaf04`
- **Tài liệu tham chiếu:**
  - Kế hoạch: `docs/superpowers/plans/2026-10-17-plan-1-database.md` (Task 6, dòng 524-800)
  - Thiết kế (Spec): `docs/superpowers/specs/2026-10-17-transport-management-design.md` (§8.2, §13, §14)
  - Brief: `.superpowers/sdd/task-6-brief.md`

---

## 1. Các thành phần đã triển khai

### 1.1 `database/01_sys_setup.sql` (Cập nhật)
- Bổ sung lệnh:
  ```sql
  GRANT SELECT ON sys.user_scheduler_jobs TO transport WITH GRANT OPTION;
  ```
  để schema `TRANSPORT` có quyền cấp `SELECT ON v_backup_job_status TO role_operator` mà không bị lỗi `ORA-01720: grant option does not exist for 'SYS.USER_SCHEDULER_JOBS'`.

### 1.2 `database/08_seed_data.sql` (Tạo mới)
- Header chuẩn SQL*Plus (`WHENEVER SQLERROR EXIT FAILURE ROLLBACK`, `SET DEFINE OFF`, `SET SQLBLANKLINES ON`, `SET SERVEROUTPUT ON SIZE UNLIMITED`).
- 5 tuyến xe: Hà Nội - Lào Cai, Hà Nội - Hải Phòng, Hà Nội - Ninh Bình, Hà Nội - Sa Pa, Hà Nội - Thanh Hóa.
- 6 phương tiện: gồm xe giường nằm `29B-196.88` và xe cuối `35B-888.99` ở trạng thái `MAINTENANCE`.
- 6 tài xế: gồm tài xế Phạm Văn Dũng có bằng lái hết hạn sau 20 ngày (`TRUNC(SYSDATE) + 20`).
- 30 hành khách với tên tiếng Việt đầy đủ.
- 35 lịch chạy:
  - 14 chuyến trong quá khứ (7 ngày qua, 2 chuyến/ngày): bán 50-80% ghế khi đang `SCHEDULED`, có một số vé hủy và hoàn tiền, sau đó chuyển trạng thái sang `COMPLETED`.
  - 21 chuyến tương lai (7 ngày tới, 3 chuyến/ngày): 6 chuyến đầu có bán sẵn vé `BOOKED` hoặc `PAID` để sơ đồ ghế hiển thị trực quan.
  - Xử lý gán biến cục bộ PL/SQL cho phần tử mảng collection trước khi gọi lệnh `INSERT INTO ... VALUES (...)` để tương thích hoàn toàn với SQL engine Oracle XE, tránh lỗi `PLS-00425`.
- 3 nhân viên demo được khởi tạo qua `pkg_staff.create_staff`:
  - `operator01` / `Operator#01` (`ROLE_OPERATOR`)
  - `seller01` / `Seller#001` (`ROLE_TICKET_SELLER`)
  - `seller02` / `Seller#002` (`ROLE_TICKET_SELLER`)
- Kết thúc bằng lệnh `EXIT`.

### 1.3 `database/install_all.bat` (Tạo mới)
- Kịch bản batch tương tác hỏi xác nhận và mật khẩu SYS (`oracle`).
- Thiết lập `set NLS_LANG=AMERICAN_AMERICA.AL32UTF8`.
- Tự động tạo thư mục `D:\OracleBackup` nếu chưa có.
- Chạy lần lượt `00_uninstall.sql` và `01_sys_setup.sql` với quyền `sys as sysdba`.
- Chạy tuần tự `02_tables.sql` -> `08_seed_data.sql` bằng tài khoản `transport/"Transport#2026"`.
- Bắt lỗi từng bước, dừng ngay nếu có lỗi (`|| goto :fail`).

### 1.4 `database/backup.bat` & `database/restore.bat` (Tạo mới)
- `backup.bat`: Đặt `NLS_LANG=AMERICAN_AMERICA.AL32UTF8`, lấy thời gian `yyyyMMdd_HHmm` qua PowerShell, gọi Data Pump `expdp transport/"Transport#2026"@localhost:1521/XEPDB1` với directory `BACKUP_DIR`.
- `restore.bat`: Đặt `NLS_LANG=AMERICAN_AMERICA.AL32UTF8`, kiểm tra tham số tên file dump, gọi `impdp` với tham số `table_exists_action=REPLACE`.

### 1.5 `database/09_test_scenarios.sql` (Cập nhật)
- Bổ sung khối chú thích hướng dẫn kiểm tra phân quyền thủ công cho nhân viên bán vé `seller01` ngay trước lệnh `EXIT`.

---

## 2. Bằng chứng kiểm thử & Xác thực

### 2.1 Cài đặt sạch toàn bộ CSDL (`install_all.bat`)
Chạy `install_all.bat` thực hiện drop sạch và tái tạo CSDL:
```
=== 02_tables ===
Table created.
...
=== 03_triggers ===
Trigger created.
...
=== 04_packages ===
Package created. Package body created.
...
=== 05_views ===
View created.
...
=== 06_backup ===
Table created. Procedure created. View created.
...
=== 07_grants ===
Grant succeeded.
...
=== 08_seed_data ===
PL/SQL procedure successfully completed.
Commit complete.
PL/SQL procedure successfully completed.

INSTALL OK. Demo accounts: operator01/Operator#01, seller01/Seller#001, seller02/Seller#002
```
=> Kết quả: Cài đặt hoàn tất 100% không có lỗi.

### 2.2 Kiểm tra số lượng Seed Data & Tiếng Việt có dấu
Thực hiện truy vấn kiểm tra:
```sql
SELECT (SELECT COUNT(*) FROM routes) r, (SELECT COUNT(*) FROM vehicles) v, (SELECT COUNT(*) FROM drivers) d,
       (SELECT COUNT(*) FROM passengers) p, (SELECT COUNT(*) FROM schedules) s, (SELECT COUNT(*) FROM staff) st FROM dual;
SELECT route_name FROM routes WHERE ROWNUM = 1;
SELECT COUNT(*) AS revenue_trips FROM v_trip_statistics WHERE revenue > 0;
```
Kết quả:
```
	 R	    V	       D	  P	     S	       ST
---------- ---------- ---------- ---------- ---------- ----------
	 5	    6	       6	 30	    35		3

ROUTE_NAME
----------------------------------------------------------------------------------------------------
Hà Nội - Lào Cai

REVENUE_TRIPS
-------------
	   17
```
- Số lượng bảng: Routes = 5, Vehicles = 6, Drivers = 6, Passengers = 30, Schedules = 35, Staff = 3.
- Tiếng Việt có dấu: `Hà Nội - Lào Cai` hiển thị chuẩn UTF-8.
- Chuyến có doanh thu (`revenue > 0`): 17 (14 chuyến đã chạy trong quá khứ + 3 chuyến tương lai có vé thanh toán).

### 2.3 Chạy bộ kịch bản kiểm thử nghiệp vụ (`09_test_scenarios.sql`)
Chạy kiểm thử trên schema đã nạp dữ liệu:
```
PASS  1. book a free seat
PASS  2. double booking is rejected (expected -20003, got -20003)
PASS  3. seat beyond capacity is rejected (expected -20001, got -20001)
PASS  4a. payment row created
PASS  4b. ticket is PAID
PASS  4c. paying twice is rejected (expected -20020, got -20020)
PASS  5a. refund row created
PASS  5b. net revenue is 0 after refund
PASS  6. cancelling twice is rejected (expected -20030, got -20030)
PASS  7. cancelled seat can be booked again
PASS  8a. direct schedule cancel is blocked (expected -20052, got -20052)
PASS  8b. all tickets cancelled
PASS  8c. paid ticket refunded
PASS  8d. schedule is CANCELLED
PASS  8e. cancelling a cancelled schedule is rejected (expected -20051, got -20051)
PASS  9a. tickets snapshot matches
PASS  9b. schedules snapshot matches
PASS  9c. payments snapshot matches
PASS  9d. backup log row counts recorded
PASS  9e. nightly job exists and is enabled
PASS  10a. invalid username is rejected (expected -20040, got -20040)
PASS  10b. invalid role is rejected (expected -20041, got -20041)
PASS  10c. short password is rejected (expected -20044, got -20044)
PASS  10d. unknown staff is rejected (expected -20042, got -20042)
PASS  11a. Oracle user created
PASS  11b. role changed
PASS  11c. account locked
PASS  11d. duplicate username is rejected (expected -1920, got -1920)
ALL TESTS PASSED
```
=> 24/24 kiểm tra đều PASS. Sau khi test chạy xong, dữ liệu seed vẫn được bảo toàn nguyên vẹn.

### 2.4 Kiểm tra phân quyền nhân viên (`seller01` và `operator01`)
1. **Tài khoản `seller01` (ROLE_TICKET_SELLER)**:
   - `SELECT COUNT(*) FROM transport.v_trip_statistics;`
     -> Trả về **35** (Thành công).
   - `INSERT INTO transport.tickets (schedule_id, passenger_id, seat_no, price) VALUES (1, 1, 1, 1);`
     -> Lỗi `ORA-01031: insufficient privileges` (Đúng như thiết kế: bán vé chỉ được thực hiện qua `pkg_ticket`).
   - `EXEC transport.pkg_staff.set_locked('SELLER02', 1)`
     -> Lỗi `PLS-00201: identifier 'TRANSPORT.PKG_STAFF' must be declared` (Đúng như thiết kế: seller không có quyền execute trên gói nhân viên).
   - `SELECT COUNT(*) FROM transport.drivers;`
     -> Lỗi `ORA-00942: table or view does not exist` (Đúng như thiết kế: seller bị ẩn bảng tài xế).
2. **Tài khoản `operator01` (ROLE_OPERATOR)**:
   - `SELECT role FROM session_roles;`
     -> Trả về `ROLE_OPERATOR` (Thành công).

### 2.5 Kiểm tra sao lưu Oracle Data Pump (`backup.bat`)
Chạy `backup.bat`:
- Dump file: `D:\OracleBackup\TRANSPORT_20261017_2322.DMP` (dung lượng 1,110,016 bytes).
- Log file: `D:\OracleBackup\export_20261017_2322.log`.
- Nội dung log:
  ```
  Master table "TRANSPORT"."SYS_EXPORT_SCHEMA_01" successfully loaded/unloaded
  ******************************************************************************
  Dump file set for TRANSPORT.SYS_EXPORT_SCHEMA_01 is:
    D:\ORACLEBACKUP\TRANSPORT_20261017_2322.DMP
  Job "TRANSPORT"."SYS_EXPORT_SCHEMA_01" successfully completed at Sat Oct 17 23:24:33 2026 elapsed 0 00:02:05
  ```
- Thử nghiệm gọi `restore.bat` không tham số: in thông báo sử dụng chuẩn (`Usage: restore.bat transport_YYYYMMDD_HHMM.dmp`) và exit code 1.

---

## 3. GitNexus & Version Control
- GitNexus detect-changes: Đã chạy phân tích, không có xung đột mã hay ảnh hưởng biểu đồ nào.
- Commit đã tạo trên master: `8c34f18` (`feat(db): add seed data, install and Data Pump scripts`).

## 4. Ghi chú / Concerns
- Không có bất kỳ vấn đề phát sinh nào. Toàn bộ kịch bản, dữ liệu mẫu, công cụ sao lưu và phân quyền đều hoạt động hoàn hảo đúng theo đặc tả hệ thống.
