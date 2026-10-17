# Plan 4 – User Guide & README Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver `docs/user-guide.md` (Vietnamese user manual with real screenshots), `docs/images/*.png` and a root `README.md`.

**Architecture:** A small PowerShell script captures the running app window to `docs/images/<name>.png`; the guide references those images. No code changes to the app.

**Tech Stack:** Markdown, PowerShell 5.1 (`System.Drawing`, `user32.dll`).

**Spec:** `docs/superpowers/specs/2026-10-17-transport-management-design.md` §14. **Prerequisite:** Plans 1–3 completed (database installed, app working).

## Global Constraints

- Guide language: Vietnamese (with correct diacritics), UTF-8.
- Demo accounts exactly: `operator01` / `Operator#01`, `seller01` / `Seller#001`, `seller02` / `Seller#002`; SYS password is never written in the docs.
- Image links are relative: `images/<file>.png` inside `docs/user-guide.md`.
- Screenshots are taken with the Vietnamese UI except `14-english.png`.

---

### Task 1: Screenshot helper and screenshots

**Files:**
- Create: `scripts/capture-window.ps1`
- Create: `docs/images/01-login.png` … `docs/images/15-excel.png`

- [ ] **Step 1: Write `scripts/capture-window.ps1`**

```powershell
# Chụp cửa sổ chính của một tiến trình và lưu vào docs/images/<Name>.png
# Dùng: powershell -ExecutionPolicy Bypass -File scripts/capture-window.ps1 -Name 02-dashboard
param(
    [Parameter(Mandatory = $true)] [string] $Name,
    [string] $Process = "Transport.Presentation.Wpf"
)

Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class Win32Capture {
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
}
"@

# Lấy toạ độ thật (không bị Windows phóng to theo DPI)
[Win32Capture]::SetProcessDPIAware() | Out-Null

$target = Get-Process -Name $Process -ErrorAction Stop |
    Where-Object { $_.MainWindowHandle -ne [IntPtr]::Zero } |
    Select-Object -First 1
if (-not $target) { throw "No window found for process '$Process'." }

[Win32Capture]::SetForegroundWindow($target.MainWindowHandle) | Out-Null
Start-Sleep -Milliseconds 500

$rect = New-Object Win32Capture+RECT
[Win32Capture]::GetWindowRect($target.MainWindowHandle, [ref]$rect) | Out-Null
$width = $rect.Right - $rect.Left
$height = $rect.Bottom - $rect.Top

$bitmap = New-Object System.Drawing.Bitmap $width, $height
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.CopyFromScreen($rect.Left, $rect.Top, 0, 0, $bitmap.Size)

$folder = Join-Path $PSScriptRoot "..\docs\images"
New-Item -ItemType Directory -Force -Path $folder | Out-Null
$output = Join-Path $folder "$Name.png"
$bitmap.Save($output, [System.Drawing.Imaging.ImageFormat]::Png)
$graphics.Dispose()
$bitmap.Dispose()
"Saved $output ($width x $height)"
```

- [ ] **Step 2: Verify the script**

Run the app (`dotnet run --project src/Transport.Presentation.Wpf`), then:
```powershell
powershell -ExecutionPolicy Bypass -File scripts/capture-window.ps1 -Name 01-login
```
Expected: `Saved ...\docs\images\01-login.png (900 x 540)` (size may differ with DPI). Open the PNG with the Read tool and confirm it shows the login window only.

- [ ] **Step 3: Capture the screenshots**

Put the app in each state below (ask the user to click, or drive it with computer-use if available), then run the script with the given name. Maximize the main window first (1366×768 or larger).

| Name | State to capture |
|---|---|
| `01-login` | Login window, Vietnamese selected, username `operator01` typed |
| `02-dashboard` | Signed in as `operator01`, Tổng quan |
| `03-sell-seatmap` | Bán vé: tomorrow, a trip selected, one green seat selected (blue), phone looked up |
| `04-sell-payment` | Payment dialog open after "Đặt vé" |
| `05-tickets` | Quản lý vé with a few rows, one selected |
| `06-passengers` | Hành khách with a passenger selected (history visible) |
| `07-routes` | Tuyến xe with the edit form open |
| `08-vehicles` | Phương tiện |
| `09-drivers` | Tài xế with the yellow warning banner |
| `10-schedules` | Lịch chạy with the add form open (route chosen, price/arrival filled) |
| `11-statistics` | Thống kê for the current month |
| `12-staff` | Nhân viên with the add form open |
| `13-backup` | Sao lưu after pressing "Sao lưu ngay" (success bar visible) |
| `14-english` | Any page after switching to English |
| `15-excel` | An exported `.xlsx` opened in Excel – run with `-Process EXCEL` |

- [ ] **Step 4: Check every image**

Run: `ls docs/images` → 15 files. Open each with the Read tool and confirm it shows the described state (no other windows covering it).

- [ ] **Step 5: Commit**

```bash
git add scripts/capture-window.ps1 docs/images
git commit -m "docs: add screenshot helper and application screenshots"
```

---

### Task 2: User guide and README

**Files:**
- Create: `docs/user-guide.md`
- Create: `README.md`

- [ ] **Step 1: Write `docs/user-guide.md`**

````markdown
# Hướng dẫn sử dụng – Hệ thống quản lý vận tải và đặt vé xe

Đề tài 11 – Xây dựng cơ sở dữ liệu quản lý vận tải và đặt vé xe (HaUI).
Ứng dụng desktop C# WPF (.NET 10) kết nối Oracle Database 21c XE.

## Mục lục

1. [Yêu cầu hệ thống](#1-yêu-cầu-hệ-thống)
2. [Cài Oracle 21c XE](#2-cài-oracle-21c-xe)
3. [Cài cơ sở dữ liệu](#3-cài-cơ-sở-dữ-liệu)
4. [Cấu hình và chạy ứng dụng](#4-cấu-hình-và-chạy-ứng-dụng)
5. [Tài khoản demo và phân quyền](#5-tài-khoản-demo-và-phân-quyền)
6. [Hướng dẫn từng màn hình](#6-hướng-dẫn-từng-màn-hình)
7. [Quy trình bán vé đầy đủ](#7-quy-trình-bán-vé-đầy-đủ)
8. [Quản lý nhân viên](#8-quản-lý-nhân-viên)
9. [Sao lưu và khôi phục](#9-sao-lưu-và-khôi-phục)
10. [Xử lý lỗi thường gặp](#10-xử-lý-lỗi-thường-gặp)

## 1. Yêu cầu hệ thống

| Thành phần | Yêu cầu |
|---|---|
| Hệ điều hành | Windows 10/11 64-bit |
| CSDL | Oracle Database 21c Express Edition (XE), PDB `XEPDB1` |
| .NET | .NET 10 SDK (để build) hoặc .NET 10 Desktop Runtime (để chạy bản đã publish) |
| Màn hình | Tối thiểu 1366×768 |
| Excel (tuỳ chọn) | Để mở file xuất Excel |

## 2. Cài Oracle 21c XE

1. Tải Oracle Database 21c XE cho Windows từ trang Oracle và cài đặt; ghi nhớ mật khẩu **SYS** đặt lúc cài.
2. Kiểm tra dịch vụ: mở *Services* → `OracleServiceXE` và `OracleOraDB21Home1TNSListener` phải ở trạng thái *Running*.
3. Kiểm tra PDB: mở Command Prompt và chạy
   ```
   lsnrctl status
   ```
   phải thấy dòng `Service "xepdb1" has 1 instance(s).`

## 3. Cài cơ sở dữ liệu

1. Mở thư mục `database` của dự án.
2. Chạy `install_all.bat` (nhấp đúp hoặc chạy trong Command Prompt).
3. Gõ `Y` để xác nhận (script **xóa và tạo lại** schema `TRANSPORT`), rồi nhập mật khẩu SYS.
4. Script tự tạo thư mục `D:\OracleBackup`, chạy lần lượt các file `01` → `08`. Khi thấy
   `INSTALL OK. Demo accounts: ...` là xong.

Các file SQL chính:

| File | Nội dung |
|---|---|
| `01_sys_setup.sql` | User `TRANSPORT`, 2 role `ROLE_OPERATOR`/`ROLE_TICKET_SELLER`, thư mục `BACKUP_DIR` |
| `02_tables.sql` | Bảng tuyến, xe, tài xế, lịch chạy, hành khách, vé, thanh toán, nhân viên |
| `03_triggers.sql` | `TRG_TICKETS_CHECK_SEAT` (kiểm tra ghế đã đặt), `TRG_SCHEDULES_CHECK_CANCEL` |
| `04_packages.sql` | `PKG_TICKET` (đặt/thanh toán/hủy vé), `PKG_SCHEDULE` (hủy chuyến), `PKG_STAFF` (nhân viên) |
| `05_views.sql` | `V_TRIP_STATISTICS` (thống kê chuyến xe) |
| `06_backup.sql` | Bảng sao lưu, `BACKUP_OPERATIONAL_DATA`, job `JOB_NIGHTLY_BACKUP` |
| `07_grants.sql` | Phân quyền cho 2 role |
| `08_seed_data.sql` | Dữ liệu mẫu và 3 tài khoản demo |
| `09_test_scenarios.sql` | Kịch bản kiểm thử (in PASS/FAIL) |

Kiểm thử CSDL (tuỳ chọn): đăng nhập SQL*Plus bằng `TRANSPORT` và chạy `@09_test_scenarios.sql`; dòng cuối phải là `ALL TESTS PASSED`.

## 4. Cấu hình và chạy ứng dụng

1. Mở `src/Transport.Presentation.Wpf/appsettings.json`:
   ```json
   {
     "Database": { "DataSource": "localhost:1521/XEPDB1", "Schema": "TRANSPORT" },
     "Language": "vi"
   }
   ```
   Đổi `DataSource` nếu Oracle nằm ở máy khác; `Language` là ngôn ngữ mặc định (`vi` hoặc `en`).
2. Chạy từ mã nguồn:
   ```
   dotnet run --project src/Transport.Presentation.Wpf
   ```
   hoặc mở `TransportManagement.slnx` bằng Visual Studio và nhấn F5.
3. Tạo bản chạy độc lập (tuỳ chọn):
   ```
   dotnet publish src/Transport.Presentation.Wpf -c Release -o publish
   ```
   rồi chạy `publish\Transport.Presentation.Wpf.exe`.

![Màn hình đăng nhập](images/01-login.png)

Đăng nhập bằng tài khoản Oracle của nhân viên; chọn ngôn ngữ ngay tại màn hình đăng nhập.

## 5. Tài khoản demo và phân quyền

| Tài khoản | Mật khẩu | Vai trò |
|---|---|---|
| `operator01` | `Operator#01` | Điều hành (`ROLE_OPERATOR`) |
| `seller01` | `Seller#001` | Nhân viên bán vé (`ROLE_TICKET_SELLER`) |
| `seller02` | `Seller#002` | Nhân viên bán vé (`ROLE_TICKET_SELLER`) |

| Chức năng | Điều hành | Bán vé |
|---|---|---|
| Tổng quan, Bán vé, Quản lý vé, Thống kê | ✔ | ✔ |
| Hành khách | Thêm, sửa, xóa | Thêm, sửa |
| Tuyến xe, Lịch chạy | Thêm, sửa, xóa, hủy chuyến | Chỉ xem |
| Phương tiện, Tài xế, Nhân viên, Sao lưu | ✔ | Không hiện |

Phân quyền được Oracle kiểm soát: mỗi nhân viên là một user Oracle được gán role. Nhân viên bán vé không có quyền ghi trực tiếp vào bảng vé – mọi thao tác bán/hủy vé đều đi qua `PKG_TICKET`.

## 6. Hướng dẫn từng màn hình

Thao tác chung trên các màn hình danh sách:

| Thao tác | Cách làm |
|---|---|
| Tìm kiếm | Gõ vào ô tìm kiếm (Ctrl+F), danh sách lọc ngay |
| Thêm mới | Nút **Thêm mới** (Ctrl+N), nhập form bên phải, **Lưu** (Ctrl+S) |
| Sửa | Chọn dòng rồi nhấn **Sửa** hoặc nhấp đúp vào dòng |
| Xóa | Chọn dòng → **Xóa** → xác nhận |
| Làm mới | F5 |
| Xuất Excel | **Xuất Excel** (Ctrl+E) → chọn nơi lưu |

Ô nhập sai được viền đỏ; rê chuột vào ô để xem lý do. Thông báo thành công/lỗi hiện ở thanh màu dưới tiêu đề trang.

### 6.1 Tổng quan

![Tổng quan](images/02-dashboard.png)

Bốn thẻ số liệu của hôm nay (số chuyến, vé đã bán, doanh thu thực, tỷ lệ lấp đầy trung bình) và biểu đồ doanh thu thực 7 ngày gần nhất (theo ngày khởi hành). Doanh thu thực = tiền thu – tiền hoàn.

### 6.2 Bán vé

![Bán vé – sơ đồ ghế](images/03-sell-seatmap.png)

- Ô lọc: điểm đi, điểm đến, ngày đi, **giá từ / giá đến** → **Tìm** (F5). Chỉ hiện chuyến còn mở bán.
- Chọn một chuyến → sơ đồ ghế hiện ra: **xanh lá** = trống, **đỏ** = đã đặt, **xanh dương** = đang chọn.
- Xem quy trình chi tiết ở [mục 7](#7-quy-trình-bán-vé-đầy-đủ).

### 6.3 Quản lý vé

![Quản lý vé](images/05-tickets.png)

- Tìm theo mã vé, số điện thoại hoặc tên khách; lọc theo trạng thái và khoảng ngày khởi hành.
- **Thanh toán**: chỉ với vé "Chờ thanh toán".
- **Hủy vé**: nhập lý do; vé đã thanh toán được hoàn 100% (ghi một dòng hoàn tiền).
- **In vé** (Ctrl+P), **Xuất Excel** (Ctrl+E).

### 6.4 Hành khách

![Hành khách](images/06-passengers.png)

Danh sách khách (tìm theo tên, SĐT, CCCD). Chọn một khách để xem **lịch sử vé** ở khung dưới. SĐT gồm 10 số bắt đầu bằng 0; CCCD (nếu nhập) gồm 12 số.

### 6.5 Tuyến xe

![Tuyến xe](images/07-routes.png)

Tên tuyến, điểm đi, điểm đến (phải khác nhau), quãng đường, thời gian dự kiến (phút) và giá cơ bản. Thời gian và giá cơ bản được dùng để tự điền khi tạo lịch chạy.

### 6.6 Phương tiện (điều hành)

![Phương tiện](images/08-vehicles.png)

Biển số (dạng `29B-196.88`), loại xe, số ghế (1–60) và trạng thái (Đang hoạt động / Bảo trì / Ngừng sử dụng). Chỉ xe *Đang hoạt động* được chọn khi tạo lịch chạy.

### 6.7 Tài xế (điều hành)

![Tài xế](images/09-drivers.png)

Thông tin tài xế, hạng bằng (D1, D2, D, E) và hạn bằng lái. Dòng **vàng** = bằng còn dưới 30 ngày, dòng **đỏ** = đã hết hạn; dải cảnh báo phía trên cho biết số tài xế cần chú ý. Tài xế hết hạn bằng không xuất hiện khi phân công chuyến.

### 6.8 Lịch chạy

![Lịch chạy](images/10-schedules.png)

- Lọc theo khoảng ngày → **Tìm**. Cột "Đã bán / số ghế" cho biết tình trạng từng chuyến.
- **Thêm mới**: chọn tuyến (giá vé và giờ đến tự điền theo tuyến), xe, ngày/giờ khởi hành (giờ dạng `HH:mm`), tài xế (chỉ hiện tài xế còn hạn bằng tại ngày chạy).
- **Bắt đầu chạy** / **Hoàn thành**: đổi trạng thái chuyến.
- **Hủy chuyến**: nhập lý do → toàn bộ vé của chuyến bị hủy và vé đã thanh toán được hoàn tiền (thủ tục `PKG_SCHEDULE.CANCEL_SCHEDULE`).
- Chỉ sửa được chuyến "Chờ chạy"; chỉ xóa được chuyến chưa có vé.

### 6.9 Thống kê

![Thống kê](images/11-statistics.png)

Dữ liệu lấy từ view `V_TRIP_STATISTICS`. Chọn khoảng ngày và tuyến → **Tìm**. Năm ô tổng hợp (số chuyến, vé đã bán, doanh thu thực, tỷ lệ lấp đầy TB, vé hủy), biểu đồ doanh thu theo tuyến và bảng chi tiết từng chuyến; có thể xuất Excel.

### 6.10 Đổi ngôn ngữ

![Giao diện tiếng Anh](images/14-english.png)

Nhấn nút ngôn ngữ trên thanh tiêu đề để chuyển Việt ↔ Anh; mọi nhãn, menu và thông báo đổi ngay. Số tiền và ngày luôn hiển thị theo định dạng Việt Nam.

### 6.11 File Excel xuất ra

![File Excel](images/15-excel.png)

Dòng 1 là tiêu đề, dòng 2 là thời điểm xuất và người xuất, từ dòng 4 là bảng dữ liệu (có bộ lọc, định dạng tiền và ngày).

## 7. Quy trình bán vé đầy đủ

1. Vào **Bán vé**, chọn ngày đi (và điểm đi/đến, khoảng giá nếu cần) → **Tìm**.
2. Chọn chuyến trong danh sách → sơ đồ ghế hiện ra.
3. Bấm một ghế **xanh lá** → ghế chuyển **xanh dương**.
4. Nhập **số điện thoại** → **Tìm khách**: khách quen sẽ tự điền họ tên; khách mới thì nhập họ tên (CCCD không bắt buộc).
5. Nhấn **Đặt vé** (Ctrl+S). Hệ thống gọi `PKG_TICKET.BOOK_TICKET`; trigger `TRG_TICKETS_CHECK_SEAT` kiểm tra ghế. Nếu ghế vừa bị quầy khác bán, thông báo "Ghế … đã có người đặt." hiện ra.
6. Hộp thoại **Thanh toán** hiện số tiền → chọn Tiền mặt / Chuyển khoản / Thẻ → **Đồng ý** (hoặc **Hủy bỏ** để khách thanh toán sau; vé ở trạng thái "Chờ thanh toán").

   ![Thanh toán](images/04-sell-payment.png)

7. Chọn **Có** khi được hỏi in vé → chọn máy in (hoặc "Microsoft Print to PDF" để lưu file PDF khổ A6).
8. Muốn hủy vé: vào **Quản lý vé** → chọn vé → **Hủy vé** → nhập lý do. Vé đã thanh toán được hoàn tiền tự động. Không hủy được vé của chuyến đã khởi hành.

## 8. Quản lý nhân viên

![Nhân viên](images/12-staff.png)

Chỉ tài khoản điều hành thấy màn hình này. Mỗi nhân viên là một **user Oracle** (tạo bằng `PKG_STAFF`).

- **Thêm mới**: tên đăng nhập (3–30 ký tự chữ/số/_ , bắt đầu bằng chữ), mật khẩu ban đầu (≥ 8 ký tự, không chứa dấu `"`), họ tên, SĐT, vai trò.
- **Sửa**: đổi họ tên, SĐT, vai trò (tên đăng nhập không đổi được).
- **Đặt lại mật khẩu**: nhập mật khẩu mới cho nhân viên đang chọn.
- **Khóa / mở khóa**: nhân viên bị khóa không đăng nhập được. Không có chức năng xóa (để giữ lịch sử người bán vé). Không thể tự khóa hoặc tự đổi quyền tài khoản của chính mình.

## 9. Sao lưu và khôi phục

![Sao lưu](images/13-backup.png)

Có hai cách sao lưu bổ trợ cho nhau:

**a) Sao lưu dữ liệu vận hành (trong CSDL)**
- Nút **Sao lưu ngay** gọi thủ tục `BACKUP_OPERATIONAL_DATA`: chép toàn bộ lịch chạy, vé, thanh toán sang các bảng `SCHEDULES_BK`, `TICKETS_BK`, `PAYMENTS_BK` (giữ 7 bản gần nhất) và ghi nhật ký vào `BACKUP_LOGS`.
- Job `JOB_NIGHTLY_BACKUP` tự chạy lúc 02:00 hằng ngày; màn hình hiện trạng thái, lần chạy gần nhất/tiếp theo và số lần lỗi.
- Dùng để đối chiếu, xem lại dữ liệu theo thời điểm.

**b) Sao lưu toàn bộ bằng Data Pump (file .dmp)**
- Chạy `database\backup.bat` → tạo `D:\OracleBackup\transport_YYYYMMDD_HHMM.dmp` và file `.log`.
- Khôi phục: `database\restore.bat transport_YYYYMMDD_HHMM.dmp` (ghi đè các bảng hiện có). Các thông báo "object already exists" đối với package/view/job khi khôi phục là bình thường.

## 10. Xử lý lỗi thường gặp

| Thông báo | Nguyên nhân | Cách xử lý |
|---|---|---|
| Không kết nối được Oracle… | Dịch vụ Oracle/Listener chưa chạy hoặc sai `DataSource` | Bật `OracleServiceXE`, `OracleOraDB21Home1TNSListener`; kiểm tra `appsettings.json` |
| Sai tên đăng nhập hoặc mật khẩu | Nhập sai | Nhập lại; mật khẩu phân biệt hoa thường |
| Tài khoản đã bị khóa | Bị điều hành khóa hoặc nhập sai quá nhiều lần | Điều hành mở khóa ở màn Nhân viên |
| Bạn không có quyền thực hiện thao tác này | Tài khoản không có role phù hợp | Đăng nhập bằng tài khoản điều hành |
| Ghế … đã có người đặt. | Quầy khác vừa bán ghế đó | Chọn ghế khác (sơ đồ đã được làm mới) |
| Chuyến đã khởi hành, không thể đặt/hủy vé | Quá giờ khởi hành | Chọn chuyến khác |
| Chuyến còn vé hiệu lực, hãy dùng chức năng "Hủy chuyến" | Đổi trạng thái chuyến sang hủy trực tiếp | Dùng nút **Hủy chuyến** để hệ thống hoàn tiền |
| Không xóa được vì đang có dữ liệu liên quan | Xóa tuyến/xe/tài xế/khách đã có chuyến hoặc vé | Đổi trạng thái thay vì xóa |
| Dữ liệu bị trùng | Trùng biển số, SĐT, CCCD, số bằng lái, tên tuyến | Kiểm tra lại dữ liệu nhập |
| Dữ liệu không hợp lệ | Vi phạm ràng buộc (VD điểm đi trùng điểm đến) | Sửa lại dữ liệu nhập |
| Chữ tiếng Việt bị lỗi trong CSDL | Chạy script SQL không đặt `NLS_LANG` | Dùng `install_all.bat` (đã đặt `NLS_LANG=AMERICAN_AMERICA.AL32UTF8`) |
````

- [ ] **Step 2: Write `README.md`**

````markdown
# Quản lý vận tải và đặt vé xe (Đề tài 11 – HaUI)

Ứng dụng desktop quản lý tuyến xe, phương tiện, tài xế, lịch chạy, hành khách, đặt vé và thanh toán.
Trọng tâm Oracle: trigger kiểm tra ghế đã đặt, package đặt/hủy vé, view thống kê chuyến xe,
phân quyền điều hành / nhân viên bán vé, sao lưu dữ liệu lịch chạy và đặt vé.

## Công nghệ

- **CSDL:** Oracle Database 21c XE – schema `TRANSPORT`, PL/SQL package, trigger, view, role, DBMS_SCHEDULER, Data Pump
- **Ứng dụng:** C# WPF .NET 10, Clean Architecture (Domain / Application / Infrastructure / Presentation),
  MVVM (CommunityToolkit.Mvvm), Generic Host DI, Fluent theme, ODP.NET + Dapper, ClosedXML, đa ngôn ngữ VI/EN
- **Kiểm thử:** xUnit v3 (unit + integration với Oracle), kịch bản PL/SQL `09_test_scenarios.sql`

## Chạy nhanh

```bat
cd database
install_all.bat
cd ..
dotnet run --project src/Transport.Presentation.Wpf
```

Tài khoản demo: `operator01` / `Operator#01` (điều hành), `seller01` / `Seller#001` (bán vé).

## Cấu trúc

```
database/   Script SQL (cài đặt, dữ liệu mẫu, kiểm thử) và backup/restore Data Pump
src/        Transport.Domain, Transport.Application, Transport.Infrastructure, Transport.Presentation.Wpf
tests/      Transport.UnitTests
docs/       Hướng dẫn sử dụng, ảnh màn hình, thiết kế và kế hoạch triển khai
```

## Tài liệu

- [Hướng dẫn sử dụng](docs/user-guide.md)
- [Thiết kế hệ thống](docs/superpowers/specs/2026-10-17-transport-management-design.md)

## Kiểm thử

```bat
dotnet test --project tests/Transport.UnitTests
set TRANSPORT_DB_TESTS=1 && dotnet test --project tests/Transport.UnitTests
```
````

- [ ] **Step 3: Verify links and images**

Run:
```bash
grep -o 'images/[0-9a-z-]*\.png' docs/user-guide.md | sort -u | while read f; do test -f "docs/$f" && echo "OK $f" || echo "MISSING $f"; done
```
Expected: 15 lines (`01-login` … `15-excel`), every line `OK`.

- [ ] **Step 4: Commit**

```bash
git add docs/user-guide.md README.md
git commit -m "docs: add Vietnamese user guide and README"
```
