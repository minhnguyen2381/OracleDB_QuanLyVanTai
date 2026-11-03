# Task 4 Report: Tài liệu (Documentation)

- **Trạng thái:** Hoàn thành (PASSED)
- **Commit:** `352eb1b` (`docs: self-registration without approval`)
- **Ngày thực hiện:** 2026-10-18

---

## Các tài liệu đã cập nhật

### 1. `README.md`
- **Dòng 52:** Cập nhật mô tả tài khoản `TRANSPORT_REGISTRAR` phản ánh đúng luồng tự đăng ký qua `PKG_REGISTRATION_PUBLIC.REGISTER_ACCOUNT` (quyền tối thiểu `CREATE SESSION` và `EXECUTE`, tạo tài khoản bán vé trực tiếp không cần duyệt, giới hạn 5 tài khoản/giờ).
- **Dòng 66 & 68:** Cập nhật cây thư mục `database/` (bỏ `REGISTRATION_REQUESTS` khỏi `02_tables.sql`, đổi `PKG_REGISTRATION` thành `PKG_REGISTRATION_PUBLIC` trong `04_packages.sql`).

### 2. `docs/user-guide.md`
- **Mục 4.3:** Viết lại hướng dẫn tự đăng ký tài khoản từ màn hình đăng nhập (form gồm Tên đăng nhập, Mật khẩu, Nhập lại mật khẩu, Họ tên, Số điện thoại; tài khoản được tạo ngay với quyền bán vé và đăng nhập trực tiếp).
- **Mục 4.4:** Cập nhật quy định đổi mật khẩu tạm lần đầu (chỉ áp dụng với tài khoản do Operator tạo/reset mật khẩu; tài khoản tự đăng ký không bị yêu cầu đổi mật khẩu do người dùng tự đặt).
- **Mục 4.6:** Đổi tiêu đề thành `(Người điều hành) Xem nhật ký đăng nhập`, gỡ bỏ toàn bộ tiểu mục a) Duyệt yêu cầu đăng ký (vì không còn bảng/quy trình chờ duyệt), giữ lại tiểu mục Tra cứu nhật ký đăng nhập và cập nhật hành động theo dõi (`LOGON`, hoặc `EXECUTE` khi quản trị tài khoản / tự đăng ký).

### 3. `docs/superpowers/specs/2026-10-18-auth-security-design.md`
- Thêm khối ghi chú cập nhật cơ chế tự đăng ký theo Plan 6 (thay thế bảng `REGISTRATION_REQUESTS` bằng `PKG_REGISTRATION_PUBLIC.register_account`, thêm tham số `p_expire` vào `pkg_staff.create_staff`, cập nhật audit policy và view `V_LOGIN_AUDIT`, mô tả các mã lỗi và đánh đổi).

### 4. `docs/superpowers/plans/2026-10-18-plan-6-self-registration.md`
- Đánh dấu hoàn thành toàn bộ các bước trong Task 4 (`Step 1`, `Step 2`, `Step 3`, `Step 4`).

---

## Kiểm tra GitNexus
- Đã chạy `node .gitnexus/run.cjs detect-changes --scope all --repo .`.
- Kết quả: 3 files thay đổi tài liệu, mức độ rủi ro: Low, 0 affected processes.
