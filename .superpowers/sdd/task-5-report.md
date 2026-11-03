# Báo Cáo Triển Khai Task 5: Staff Management Package and Role Grants (TDD)

- **Trạng thái:** DONE
- **Commit:** `18eaf04` - `feat(db): add staff management package and role grants`
- **Tài liệu tham chiếu:**
  - Kế hoạch: `docs/superpowers/plans/2026-10-17-plan-1-database.md` (Task 5, dòng 447-522)
  - Thiết kế (Spec): `docs/superpowers/specs/2026-10-17-transport-management-design.md` (§5.3, §7.2, §7.3, §9)
  - Brief: `.superpowers/sdd/task-5-brief.md`

---

## 1. Các thành phần đã triển khai

### 1.1 `database/09_test_scenarios.sql`
- Thêm bước dọn dẹp an toàn đầu thủ tục `cleanup`:
  ```sql
  DELETE FROM staff WHERE username = 'TEST_STAFF_01';
  BEGIN EXECUTE IMMEDIATE 'DROP USER test_staff_01'; EXCEPTION WHEN OTHERS THEN NULL; END;
  ```
- Thay thế marker `-- @@STAFF_TESTS@@` bằng 8 kiểm tra kiểm thử vòng đời tài khoản và dữ liệu không hợp lệ (kịch bản 10a-10d, 11a-11d):
  - `10a. invalid username is rejected`: Chặn ký tự đặc biệt / SQL injection (`x; drop`), mong đợi lỗi `-20040`.
  - `10b. invalid role is rejected`: Chặn role không hợp lệ (`ROLE_ADMIN`), mong đợi lỗi `-20041`.
  - `10c. short password is rejected`: Chặn mật khẩu dưới 8 ký tự (`short`), mong đợi lỗi `-20044`.
  - `10d. unknown staff is rejected`: Chặn thao tác khóa trên nhân viên không tồn tại (`nobody_xyz`), mong đợi lỗi `-20042`.
  - `11a. Oracle user created`: Tạo tài khoản Oracle `test_staff_01` thành công, kiểm tra tồn tại trong `all_users`.
  - `11b. role changed`: Đổi role từ `ROLE_TICKET_SELLER` sang `ROLE_OPERATOR`, cập nhật thành công trong bảng `staff`.
  - `11c. account locked`: Khóa tài khoản thành công (`status = 'LOCKED'`), thực hiện lệnh `ALTER USER ... ACCOUNT LOCK`.
  - `11d. duplicate username is rejected`: Chặn tạo tài khoản trùng tên trong Oracle DB, bắt lỗi `ORA-01920: user name 'string' conflicts with another user or role name` (`-1920`).
  - Dọn dẹp user test `TEST_STAFF_01` ngay sau kịch bản 11.

### 1.2 `database/04_packages.sql`
- Bổ sung gói `pkg_staff` (spec & body) theo đúng đặc tả Spec §5.3 ngay sau `pkg_schedule` và trước khối kiểm tra `user_errors`:
  - `valid_username(p_username)`: Kiểm tra độ dài 3-30 ký tự, bắt đầu bằng chữ, chỉ gồm chữ/số/gạch dưới chống SQL injection.
  - `check_role(p_role)`: Chỉ chấp nhận `ROLE_OPERATOR` hoặc `ROLE_TICKET_SELLER`.
  - `check_password(p_password)`: Tối thiểu 8 ký tự, không chứa nháy kép.
  - `managed_role(p_user)`: Kiểm tra nhân viên thuộc quản lý trong bảng `staff`.
  - `not_self(p_user)`: Ngăn chặn tự đổi quyền hoặc tự khóa tài khoản của chính mình.
  - `create_staff(...)`: Tạo user Oracle, gán role, thêm vào `staff`, tự động rollback & `DROP USER` nếu xảy ra sự cố giữa chừng để tránh user mồ côi.
  - `update_staff_info(...)`: Cập nhật họ tên, SĐT.
  - `change_role(...)`: Thu hồi role cũ (`REVOKE`), cấp role mới (`GRANT`), cập nhật `staff`.
  - `reset_password(...)`: Đổi mật khẩu tài khoản Oracle qua `ALTER USER`.
  - `set_locked(...)`: Khóa / mở khóa tài khoản Oracle qua `ALTER USER ... ACCOUNT LOCK/UNLOCK`.

### 1.3 `database/07_grants.sql`
- Tạo file phân quyền đầy đủ theo Spec §7.3 với standard header và `EXIT`:
  - Cấp quyền cho `ROLE_OPERATOR`: CRUD trên các danh mục và bảng vận hành (`routes`, `vehicles`, `drivers`, `schedules`, `passengers`), SELECT trên `tickets`, `payments`, `staff`, `v_trip_statistics`, `backup_logs`, `v_backup_job_status`; EXECUTE trên `pkg_ticket`, `pkg_schedule`, `pkg_staff`, `backup_operational_data`.
  - Cấp quyền cho `ROLE_TICKET_SELLER`: SELECT trên `routes`, `vehicles`, `schedules`, `tickets`, `payments`, `staff`, `v_trip_statistics`; SELECT, INSERT, UPDATE trên `passengers`; EXECUTE trên `pkg_ticket`.

---

## 2. Bằng chứng kiểm thử theo phương pháp TDD

### 2.1 Pha ĐỎ (RED)
Chạy `database/09_test_scenarios.sql` sau khi thêm kịch bản 10 & 11 nhưng chưa bổ sung `pkg_staff`:
```
SQL*Plus: Release 21.0.0.0.0 - Production on Sat Oct 17 23:12:11 2026
Version 21.3.0.0.0

Connected to:
Oracle Database 21c Express Edition Release 21.0.0.0.0 - Production
Version 21.3.0.0.0

  SELECT COUNT(*) INTO v_n FROM staff WHERE username = 'TEST_STAFF_01' AND role_name = 'ROLE_OPERATOR';
                                                  *
ERROR at line 152:
ORA-06550: line 148, column 3:
PLS-00201: identifier 'PKG_STAFF.CREATE_STAFF' must be declared
ORA-06550: line 148, column 3:
PL/SQL: Statement ignored
ORA-06550: line 151, column 3:
PLS-00201: identifier 'PKG_STAFF.CHANGE_ROLE' must be declared
ORA-06550: line 151, column 3:
PL/SQL: Statement ignored
ORA-06550: line 154, column 3:
PLS-00201: identifier 'PKG_STAFF.SET_LOCKED' must be declared
ORA-06550: line 154, column 3:
PL/SQL: Statement ignored

Disconnected from Oracle Database 21c Express Edition Release 21.0.0.0.0 - Production
Version 21.3.0.0.0
```
=> Kiểm thử thất bại đúng như kỳ vọng: `PLS-00201: identifier 'PKG_STAFF.CREATE_STAFF' must be declared`.

### 2.2 Biên dịch Packages
Chạy `database/04_packages.sql` trên schema `TRANSPORT`:
```
Connected to:
Oracle Database 21c Express Edition Release 21.0.0.0.0 - Production
Version 21.3.0.0.0

Package created.
Package body created.
Package created.
Package body created.
Package created.
Package body created.

no rows selected

PL/SQL procedure successfully completed.

Disconnected from Oracle Database 21c Express Edition Release 21.0.0.0.0 - Production
```
=> Cả 3 packages (`pkg_ticket`, `pkg_schedule`, `pkg_staff`) được biên dịch thành công, không có lỗi trong `user_errors`.

### 2.3 Phân quyền Grants
Chạy `database/07_grants.sql` trên schema `TRANSPORT`:
```
Connected to:
Oracle Database 21c Express Edition Release 21.0.0.0.0 - Production
Version 21.3.0.0.0

Grant succeeded. (routes to role_operator)
Grant succeeded. (vehicles to role_operator)
Grant succeeded. (drivers to role_operator)
Grant succeeded. (schedules to role_operator)
Grant succeeded. (passengers to role_operator)
Grant succeeded. (tickets to role_operator)
Grant succeeded. (payments to role_operator)
Grant succeeded. (staff to role_operator)
Grant succeeded. (v_trip_statistics to role_operator)
Grant succeeded. (backup_logs to role_operator)
Grant succeeded. (v_backup_job_status to role_operator)
Grant succeeded. (pkg_ticket to role_operator)
Grant succeeded. (pkg_schedule to role_operator)
Grant succeeded. (pkg_staff to role_operator)
Grant succeeded. (backup_operational_data to role_operator)
Grant succeeded. (routes to role_ticket_seller)
Grant succeeded. (vehicles to role_ticket_seller)
Grant succeeded. (schedules to role_ticket_seller)
Grant succeeded. (passengers to role_ticket_seller)
Grant succeeded. (tickets to role_ticket_seller)
Grant succeeded. (payments to role_ticket_seller)
Grant succeeded. (staff to role_ticket_seller)
Grant succeeded. (v_trip_statistics to role_ticket_seller)
Grant succeeded. (pkg_ticket to role_ticket_seller)

Disconnected from Oracle Database 21c Express Edition Release 21.0.0.0.0 - Production
```
=> Toàn bộ 24 quyền được cấp thành công.

### 2.4 Pha XANH (GREEN)
Chạy lại `database/09_test_scenarios.sql` trên schema `TRANSPORT`:
```
SQL*Plus: Release 21.0.0.0.0 - Production on Sat Oct 17 23:13:39 2026
Version 21.3.0.0.0

Copyright (c) 1982, 2021, Oracle.  All rights reserved.

Connected to:
Oracle Database 21c Express Edition Release 21.0.0.0.0 - Production
Version 21.3.0.0.0

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

PL/SQL procedure successfully completed.

Disconnected from Oracle Database 21c Express Edition Release 21.0.0.0.0 - Production
Version 21.3.0.0.0
```
=> Toàn bộ 27/27 assertions đạt `PASS`, kết thúc với `ALL TESTS PASSED`.

---

## 3. Tự kiểm tra (Self-Review)
- Triển khai đầy đủ và chính xác đặc tả Spec §5.3, §7.2, §7.3.
- `pkg_staff` phòng ngừa chặt chẽ SQL Injection bằng regex `^[A-Za-z][A-Za-z0-9_]{2,29}$`, kiểm tra role và độ dài password.
- Quá trình tạo user có bọc block `BEGIN ... EXCEPTION WHEN OTHERS THEN ROLLBACK; EXECUTE IMMEDIATE 'DROP USER ' || v_user; RAISE; END;` để không để lại user mồ côi nếu gán role hoặc ghi bảng `staff` lỗi.
- `not_self` chặn thao tác tự khóa hoặc tự đổi quyền tài khoản của người đang đăng nhập.
- Standard headers và `EXIT` có mặt ở các file SQL.
- Commit sạch gồm đúng 3 file theo yêu cầu: `database/04_packages.sql`, `database/07_grants.sql`, `database/09_test_scenarios.sql`.

---

## 4. Lưu ý và đề xuất cho Task 6 (Concerns & Recommendations)
- **Ghi chú về `GRANT SELECT ON v_backup_job_status TO role_operator`:**
  Khi `TRANSPORT` grant SELECT trên view `v_backup_job_status` (view này SELECT từ `user_scheduler_jobs`), Oracle yêu cầu schema sở hữu view phải có quyền `WITH GRANT OPTION` trên đối tượng cơ sở của `SYS`.
  Trong CSDL hiện tại, lệnh `GRANT SELECT ON sys.user_scheduler_jobs TO transport WITH GRANT OPTION;` đã được thực thi thành công dưới quyền SYSDBA.
  **Đề xuất cho Task 6:** Khi hoàn thiện `database/01_sys_setup.sql` hoặc script cài đặt toàn bộ `install_all.bat`, cần đảm bảo có dòng `GRANT SELECT ON sys.user_scheduler_jobs TO transport WITH GRANT OPTION;` để quá trình cài mới hoàn toàn (fresh install) chạy mượt mà.
