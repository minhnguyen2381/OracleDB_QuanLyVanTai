# Quản lý vận tải và đặt vé xe (Đề tài 11 – HaUI)

Ứng dụng desktop quản lý tuyến xe, phương tiện, tài xế, lịch chạy, hành khách, đặt vé và thanh toán.

**Trọng tâm công nghệ Oracle Database 21c XE:**
- Trigger kiểm tra ghế đã đặt (`TRG_TICKETS_CHECK_SEAT` + khóa `FOR UPDATE` chống tranh chấp vé đồng thời).
- PL/SQL Package đặt và hủy vé tự động (`PKG_TICKET`, `PKG_SCHEDULE`).
- View thống kê chuyến xe thời gian thực (`V_TRIP_STATISTICS`).
- Phân quyền bảo mật đa người dùng: Người điều hành (`ROLE_OPERATOR`) và Nhân viên bán vé (`ROLE_TICKET_SELLER`) gắn với user Oracle thật.
- Sao lưu và phục hồi dữ liệu: Thủ tục sao lưu vận hành (`BACKUP_OPERATIONAL_DATA`), Job tự động hàng đêm (`JOB_NIGHTLY_BACKUP` qua DBMS_SCHEDULER) và tiện ích Oracle Data Pump (`expdp` / `impdp`).

---

## Công nghệ sử dụng

- **Hệ quản trị CSDL:** Oracle Database 21c Express Edition (XE), PDB `XEPDB1`, PL/SQL Packages, Triggers, Views, Roles, Grants, DBMS_SCHEDULER, Oracle Data Pump.
- **Ứng dụng Desktop:** C# WPF (.NET 10 LTS), Clean Architecture (Domain $\rightarrow$ Application $\rightarrow$ Infrastructure $\leftarrow$ Presentation), mô hình MVVM (CommunityToolkit.Mvvm 8.4), Generic Host Dependency Injection, giao diện Fluent Theme hiện đại, thư viện kết nối ODP.NET Core + Dapper, xuất báo cáo ClosedXML (.xlsx), hỗ trợ đa ngôn ngữ Song ngữ Tiếng Việt & Tiếng Anh.
- **Kiểm thử tự động:** xUnit v3 (toàn diện Unit Tests và Oracle Integration Tests), bộ kịch bản kiểm thử CSDL PL/SQL `09_test_scenarios.sql`.

---

## Hướng dẫn chạy nhanh (Quick Start)

### 1. Cài đặt CSDL tự động 1-Click
```bat
cd database
install_all.bat
cd ..
```
*Nhập `Y` để xác nhận và nhập mật khẩu quản trị SYS của bạn khi được nhắc. Script sẽ hỏi mật khẩu SYS và yêu cầu bạn **tự đặt mật khẩu cho chủ schema TRANSPORT** (không dùng ký tự `"` hoặc `%`). Hãy ghi nhớ mật khẩu này để chạy `backup.bat` / `restore.bat`.*

### 2. Khởi chạy ứng dụng
```bat
dotnet run --project src/Transport.Presentation.Wpf
```

### 3. Tài khoản demo trải nghiệm

| Tài khoản | Mật khẩu | Quyền hạn |
|---|---|---|
| `operator01` | `Operator#01` | **Người điều hành** (`ROLE_OPERATOR`) – Toàn quyền hệ thống, quản lý danh mục, nhân viên, sao lưu |
| `seller01` | `Seller#001` | **Nhân viên bán vé** (`ROLE_TICKET_SELLER`) – Tra cứu, chọn ghế sơ đồ, bán vé, in vé, hủy vé |
| `seller02` | `Seller#002` | **Nhân viên bán vé** (`ROLE_TICKET_SELLER`) – Tài khoản phụ bán vé tại quầy |

> Lần đăng nhập đầu tiên, ứng dụng bắt đặt mật khẩu mới (mật khẩu trên chỉ dùng được một lần). Mật khẩu mới phải ≥ 8 ký tự, có chữ hoa, chữ thường, chữ số, ký tự đặc biệt và không chứa tên đăng nhập. Sai 5 lần liên tiếp tài khoản bị khóa 15 phút.

---

## Bảo mật hệ thống

- **Chính sách mật khẩu & Profile:** Ràng buộc độ phức tạp qua hàm `staff_password_verify` trong `profile_staff`, bắt buộc đổi mật khẩu tạm lần đầu (`PASSWORD EXPIRE`), giới hạn 5 lần sai mật khẩu khóa 15 phút, lưu vết 5 mật khẩu gần nhất không được trùng (`PASSWORD_REUSE_MAX 5`).
- **Tài khoản đăng ký REGISTRAR:** `TRANSPORT_REGISTRAR` (mật khẩu `Register#Only1` trong `appsettings.json`) được cấu hình công khai có chủ đích theo nguyên tắc đặc quyền tối thiểu (Least Privilege): chỉ sở hữu quyền `CREATE SESSION` và `EXECUTE` duy nhất stored procedure `PKG_REGISTRATION_PUBLIC.SUBMIT_REQUEST` để gửi yêu cầu đăng ký tài khoản mới; không có quyền đọc bất kỳ bảng dữ liệu nào trong CSDL.
- **Nhật ký đăng nhập & quản trị tài khoản:** Sử dụng Oracle Unified Auditing với 2 audit policies `pol_transport_logon` và `pol_transport_account_admin`, cung cấp view `V_LOGIN_AUDIT` cho người điều hành theo dõi thời gian thực.
- **Kiểm tra cấu hình bảo mật:** Chạy script kiểm thử bảo mật tự động `database/11_security_checks.sql` (bằng tài khoản SYS) để xác minh 100% các tiêu chí bảo mật đạt chuẩn.
- *Ghi chú:* Các script `.bat` truyền mật khẩu qua dòng lệnh `sqlplus`/`expdp`, tiến trình khác trên cùng máy có thể thấy – chỉ chạy trên máy cài đặt tin cậy.

---

## Cấu trúc dự án

```
OracleDB_QuanLyVanTai/
├── database/                    # Toàn bộ mã nguồn CSDL Oracle 21c XE
│   ├── 00_uninstall.sql         # Script dọn dẹp sạch sẽ schema và users
│   ├── 01_sys_setup.sql         # Tạo user TRANSPORT, roles, directory BACKUP_DIR
│   ├── 02_tables.sql            # Tạo các bảng nghiệp vụ chính và REGISTRATION_REQUESTS
│   ├── 03_triggers.sql          # Triggers nghiệp vụ chặn trùng ghế, hủy chuyến
│   ├── 04_packages.sql          # Packages PKG_TICKET, PKG_SCHEDULE, PKG_STAFF, PKG_REGISTRATION
│   ├── 05_views.sql             # Views V_TRIP_STATISTICS, V_LOGIN_AUDIT
│   ├── 06_backup.sql            # Thủ tục sao lưu, bảng snapshot *_BK, Nightly Job
│   ├── 07_grants.sql            # Phân quyền bảo mật cho Roles và REGISTRAR
│   ├── 08_seed_data.sql         # Dữ liệu mẫu (xe, tuyến, tài xế, lịch chạy, tài khoản)
│   ├── 09_test_scenarios.sql    # Kịch bản kiểm thử tự động PL/SQL (PASS/FAIL)
│   ├── 10_audit.sql             # Cấu hình Oracle Unified Auditing
│   ├── 11_security_checks.sql   # Kiểm thử cấu hình bảo mật CSDL (SYS)
│   ├── install_all.bat          # Cài đặt tự động 1-click
│   ├── backup.bat               # Script sao lưu Data Pump (.dmp)
│   └── restore.bat              # Script phục hồi Data Pump (.dmp)
├── src/                         # Mã nguồn ứng dụng WPF Clean Architecture
│   ├── Transport.Domain/        # Thực thể nghiệp vụ, Enums, Error Codes
│   ├── Transport.Application/   # Use Cases, Interfaces, Services, Models
│   ├── Transport.Infrastructure/# Triển khai Oracle Repositories, ClosedXML Excel
│   └── Transport.Presentation.Wpf/ # Giao diện WPF, MVVM, Song ngữ, Fluent Theme
├── tests/                       # Dự án kiểm thử tự động
│   └── Transport.UnitTests/     # 217 test cases (209 Unit tests & 8 Live Oracle tests)
├── docs/                        # Tài liệu hướng dẫn và hình ảnh minh họa
│   ├── user-guide.md            # Sổ tay hướng dẫn sử dụng chi tiết
│   ├── images/                  # 15 ảnh chụp màn hình giao diện thực tế
│   └── superpowers/             # Hồ sơ thiết kế kiến trúc và kế hoạch triển khai
├── scripts/                     # Scripts hỗ trợ chụp ảnh và tự động hóa
└── README.md                    # Tài liệu giới thiệu tổng quan dự án
```

---

## Tài liệu chi tiết

- [📖 Sổ tay hướng dẫn sử dụng chi tiết](docs/user-guide.md)
- [📐 Tài liệu đặc tả và thiết kế hệ thống](docs/superpowers/specs/2026-10-17-transport-management-design.md)
- [📋 Kế hoạch triển khai (Implementation Plans)](docs/superpowers/plans/)

---

## Chạy kiểm thử tự động (Testing)

Dự án sở hữu bộ kiểm thử tự động với 217 bài kiểm tra (100% Passed):

```bat
:: 1. Chạy toàn bộ Unit Tests (Không cần kết nối Oracle live)
dotnet test --project tests/Transport.UnitTests

:: 2. Chạy cả kiểm thử tích hợp thực tế với CSDL Oracle 21c XE
set TRANSPORT_DB_TESTS=1
dotnet test --project tests/Transport.UnitTests
```
*(Trong PowerShell: `$env:TRANSPORT_DB_TESTS="1"; dotnet test tests/Transport.UnitTests`)*
