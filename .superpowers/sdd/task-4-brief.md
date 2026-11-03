### Task 4: Tài liệu

**Files:**
- Modify: `README.md:52,66,68`
- Modify: `docs/user-guide.md` (§4.3, §4.4 gạch đầu dòng đầu, §4.6)
- Modify: `docs/superpowers/specs/2026-10-18-auth-security-design.md`

- [ ] **Step 1: README**

Dòng 52 thay bằng:

```markdown
- **Tài khoản đăng ký REGISTRAR:** `TRANSPORT_REGISTRAR` (mật khẩu `Register#Only1` trong `appsettings.json`) được công khai có chủ đích theo nguyên tắc đặc quyền tối thiểu: chỉ có `CREATE SESSION` và `EXECUTE` trên `PKG_REGISTRATION_PUBLIC.REGISTER_ACCOUNT`. Thủ tục này luôn tạo tài khoản **bán vé** (không bao giờ là điều hành), tối đa 5 tài khoản mỗi giờ; REGISTRAR không đọc được bảng nào.
```

Dòng 66: `│   ├── 02_tables.sql            # Tạo các bảng nghiệp vụ chính`
Dòng 68: `│   ├── 04_packages.sql          # Packages PKG_TICKET, PKG_SCHEDULE, PKG_STAFF, PKG_REGISTRATION_PUBLIC`

- [ ] **Step 2: Hướng dẫn sử dụng**

Thay nội dung §4.3 (từ dòng dưới heading đến trước `### 4.4`) bằng:

```markdown
Khi chưa có tài khoản, nhân viên tự đăng ký ngay tại màn hình đăng nhập:
1. Nhấn nút **Đăng ký tài khoản** ở cuối form đăng nhập.
2. Hộp thoại **Đăng ký tài khoản** xuất hiện:
   - **Tên đăng nhập**: Tối thiểu 3 ký tự, bắt đầu bằng chữ cái, chỉ chứa chữ cái, chữ số và dấu gạch dưới (`_`).
   - **Mật khẩu** và **Nhập lại mật khẩu**: theo quy tắc mật khẩu an toàn ở mục 4.4; hai ô phải khớp thì nút **Đăng ký** mới bật.
   - **Họ và tên**: Họ tên đầy đủ (tối đa 100 ký tự).
   - **Số điện thoại**: 10 chữ số, bắt đầu bằng 0, không bắt buộc.
3. Nhấn **Đăng ký**. Tài khoản Oracle được tạo ngay qua tài khoản kỹ thuật `TRANSPORT_REGISTRAR`, với quyền **Nhân viên bán vé**.
4. Hộp thoại đóng, tên đăng nhập được điền sẵn; nhập mật khẩu vừa đặt để đăng nhập (không phải đổi mật khẩu lần đầu).

Lưu ý: tên đăng nhập đã tồn tại sẽ bị báo lỗi; mỗi giờ hệ thống chỉ nhận tối đa 5 tài khoản tự đăng ký. Người điều hành có thể nâng quyền hoặc khóa tài khoản ở trang **Nhân viên**.
```

Ở §4.4, đổi gạch đầu dòng đầu thành:

```markdown
- **Bắt buộc đổi mật khẩu tạm**: Tài khoản do người điều hành tạo hoặc được đặt lại mật khẩu có trạng thái hết hạn ngay (`PASSWORD EXPIRE`). Trong lần đăng nhập đầu tiên bằng mật khẩu tạm, hộp thoại **Đặt mật khẩu mới** sẽ tự động bật lên. Tài khoản tự đăng ký không bị áp dụng vì mật khẩu do chính người dùng đặt.
```

Ở §4.6, đổi heading thành `### 4.6 (Người điều hành) Xem nhật ký đăng nhập`. Xóa toàn bộ tiểu mục `#### a) Duyệt yêu cầu đăng ký tài khoản ...` (5 bước). Đổi `#### b) Tra cứu nhật ký đăng nhập (Menu 'Nhật ký đăng nhập')` thành `#### Tra cứu nhật ký đăng nhập (Menu 'Nhật ký đăng nhập')`. Trong bước 3, sửa "Hành động (`LOGON`)" thành "Hành động (`LOGON`, hoặc `EXECUTE` khi quản trị tài khoản hay tự đăng ký)".

- [ ] **Step 3: Ghi thay đổi vào spec auth-security**

Trong `docs/superpowers/specs/2026-10-18-auth-security-design.md`, chèn ngay sau dòng `Ngày: 2026-10-18. ...`:

```markdown
> **Cập nhật 2026-10-18 – tự đăng ký (Plan 6, `docs/superpowers/plans/2026-10-18-plan-6-self-registration.md`).** Bỏ luồng duyệt vì không có người duyệt. Các mục sau được **thay thế**:
> - §2.2–2.3: không còn bảng `REGISTRATION_REQUESTS` và package `PKG_REGISTRATION`. `PKG_REGISTRATION_PUBLIC.register_account(p_username, p_password, p_full_name, p_phone)` kiểm tra dữ liệu (`-20062`), giới hạn 5 tài khoản/giờ cho mỗi user gọi theo `STAFF.CREATED_BY` (`-20061 Too many registrations. Try again later.`), rồi gọi `pkg_staff.create_staff(..., 'ROLE_TICKET_SELLER', p_expire => 0)`. Role luôn là bán vé, không `PASSWORD EXPIRE`.
> - §2.4: `create_staff` có thêm `p_expire IN NUMBER DEFAULT 1`.
> - §2.5: audit policy và `v_login_audit` theo dõi `PKG_STAFF` và `PKG_REGISTRATION_PUBLIC`.
> - §3: bỏ `-20060`; `-20061` ứng với `TooManyRegistrations`.
> - §4.1: form đăng ký có mật khẩu và nhập lại mật khẩu; đăng ký xong điền sẵn username và đăng nhập được ngay. §4.4: bỏ trang "Yêu cầu đăng ký".
> - Đánh đổi chấp nhận: trùng username báo ORA-01920, nên có thể dò được username.
```

- [ ] **Step 4: Commit**

```bash
node .gitnexus/run.cjs detect-changes --scope all --repo .
git add README.md docs/user-guide.md docs/superpowers/specs/2026-10-18-auth-security-design.md docs/superpowers/plans/2026-10-18-plan-6-self-registration.md
git commit -m "docs: self-registration without approval"
```
