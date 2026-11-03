# Báo cáo thực hiện Task 1: Tầng Oracle – Tự đăng ký tạo tài khoản bán vé ngay

**Trạng thái:** `DONE_WITH_CONCERNS` (Các bước 1 đến 5 đã hoàn thành chính xác 100%. Bước 6 kiểm thử trực tiếp trên CSDL cần người dùng thực thi do yêu cầu mật khẩu SYS và TRANSPORT).

---

## 1. Tóm tắt các thay đổi đã thực hiện

### Bước 1: Cập nhật kịch bản kiểm thử (`database/09_test_scenarios.sql`)
- Đã thêm biến `v_before NUMBER;` trong khối `DECLARE`.
- Cập nhật thủ tục `cleanup`: Xóa các tài khoản `TEST_REG_01` .. `TEST_REG_09` và DROP các user tương ứng; loại bỏ lệnh xóa bảng `registration_requests`.
- Viết lại toàn bộ kịch bản 12:
  - `12a`: Kiểm tra username không hợp lệ bị từ chối (-20062).
  - `12b`: Kiểm tra SĐT không hợp lệ bị từ chối (-20062).
  - `12c`: Đăng ký tài khoản hợp lệ tạo ngay tài khoản nhân viên bán vé (`ROLE_TICKET_SELLER`, trạng thái ACTIVE) và user Oracle.
  - `12d`: Đăng ký trùng username bị từ chối (-1920 / ORA-01920).
  - `12e`: Mật khẩu yếu bị verify function chặn (-28003 / ORA-28003).
  - `12f`: Giới hạn chống spam tối đa 5 tài khoản / giờ cho mỗi caller (-20061).
- Cập nhật phần hướng dẫn kiểm tra phân quyền thủ công ở cuối file để kiểm tra `transport.pkg_staff.create_staff` bị chặn đối với `TRANSPORT_REGISTRAR`.

### Bước 2: Sửa `pkg_staff` (`database/04_packages.sql`)
- Thêm tham số `p_expire IN NUMBER DEFAULT 1` vào `pkg_staff.create_staff` ở cả package specification và package body (giữ nguyên tương thích với các lệnh gọi cũ).
- Thêm mệnh đề `PASSWORD EXPIRE` linh hoạt: chỉ hết hạn khi `p_expire = 1` (điều hành tạo tài khoản cấp mật khẩu tạm). Khi `p_expire = 0` (người dùng tự đăng ký), mật khẩu không bị hết hạn ngay.

### Bước 3: Thay thế `pkg_registration_public`, xóa `pkg_registration` (`database/04_packages.sql`)
- Thay thế `pkg_registration_public` với thủ tục `register_account(p_username, p_password, p_full_name, p_phone)`.
  - Validate dữ liệu đầu vào.
  - Kiểm tra chống spam: đếm số bản ghi trong `staff` được tạo bởi `USER` trong 1 giờ qua (giới hạn tối đa 5).
  - Gọi `pkg_staff.create_staff(p_username, p_password, v_name, p_phone, 'ROLE_TICKET_SELLER', p_expire => 0)`.
- Xóa hoàn toàn package `pkg_registration` (gồm `approve_request` và `reject_request`).

### Bước 4: Xóa bảng, sửa grants, views, audit, uninstall
- `database/02_tables.sql`: Xóa định nghĩa bảng `registration_requests` và index `idx_reg_status`.
- `database/07_grants.sql`:
  - Xóa các grant `SELECT ON registration_requests` và `EXECUTE ON pkg_registration` cấp cho `role_operator`.
  - Cập nhật chú thích cấp quyền execute `pkg_registration_public` cho `transport_registrar`.
- `database/05_views.sql`: Cập nhật view `v_login_audit` để theo dõi `PKG_REGISTRATION_PUBLIC` thay cho `PKG_REGISTRATION`.
- `database/10_audit.sql`: Cập nhật Unified Audit policy `pol_transport_account_admin` để audit `EXECUTE ON transport.pkg_registration_public`.
- `database/00_uninstall.sql`: Cập nhật khối dọn user test để drop tất cả user `TEST_REG_%`.
- `database/01_sys_setup.sql`: Cập nhật comment mô tả quyền gọi `REGISTER_ACCOUNT`.

### Bước 5: Kiểm tra tham chiếu cũ
- Đã chạy kiểm tra:
  ```bash
  git grep -i -E "registration_requests|pkg_registration\b|submit_request|approve_request|reject_request" -- database/
  ```
  Kết quả: **0 dòng tham chiếu cũ còn sót lại** trong toàn bộ thư mục `database/`.

---

## 2. Hướng dẫn thực thi Bước 6 dành cho Người dùng

Vì tác vụ yêu cầu mật khẩu người quản trị CSDL (`SYS` và `TRANSPORT`), người dùng vui lòng mở Command Prompt (`cmd`) tại thư mục `database` và thực thi:

```bat
cd D:\DuAn\OracleDB_QuanLyVanTai\database
install_all.bat
sqlplus -S -L transport/"<mật khẩu TRANSPORT>"@//localhost:1521/XEPDB1 @09_test_scenarios.sql
sqlplus -S -L "sys/<mật khẩu SYS>@//localhost:1521/XEPDB1 as sysdba" @11_security_checks.sql
```

**Kết quả kỳ vọng:**
1. `install_all.bat` chạy hoàn tất sạch sẽ, không có lỗi biên dịch PL/SQL (`user_errors` rỗng).
2. `09_test_scenarios.sql` in ra `ALL TESTS PASSED`, bao gồm các test case `PASS  12c` đến `PASS  12f` và `PASS  13`.
3. `11_security_checks.sql` in ra `ALL CHECKS PASSED`.

Sau khi Bước 6 được người dùng xác nhận đạt yêu cầu, có thể tiến hành commit Bước 7:
```bash
node .gitnexus/run.cjs detect-changes --scope all --repo .
git add database/
git commit -m "feat(db): self-registration creates a ticket seller account without approval"
```
