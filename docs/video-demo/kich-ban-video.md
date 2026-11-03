# Kịch bản video demo, 15 phút

Học phần: Hệ quản trị CSDL nâng cao. Video tập trung vào phần Oracle. App WPF chỉ dùng để tạo thao tác thật, sau đó mọi kết quả được chứng minh bằng SQL trong SQL Developer.

File đi kèm (cùng thư mục):

| File | Mở trong SQL Developer với kết nối |
|---|---|
| `demo_transport.sql` | TRANSPORT (worksheet chính, gọi là phiên A) |
| `demo_seller01.sql` | seller01 |
| `demo_sys.sql` | SYS AS SYSDBA, service XEPDB1 |

Mỗi câu lệnh có mã khối, ví dụ `[1b]`, `[2b-A1]`. Kịch bản dưới đây gọi theo mã đó.

## 1. Phân bổ thời gian

| Thời gian | Đoạn | Màn hình |
|---|---|---|
| 0:00–1:00 | Mở đầu | Slide 1–3 |
| 1:00–2:30 | Kiến trúc, mô hình dữ liệu | Slide 4–6 |
| 2:30–5:00 | Demo 1: bán vé, trigger kiểm tra ghế | App → SQL (TRANSPORT) |
| 5:00–8:00 | Demo 2: hai quầy tranh một ghế | Slide 8 → SQL (3 kết nối) |
| 8:00–10:00 | Demo 3: phân quyền, profile, kiểm toán | Slide 9–10 → App → SQL |
| 10:00–11:30 | Demo 4: view thống kê, kế hoạch thực thi | App → SQL |
| 11:30–13:00 | Demo 5: sao lưu | Slide 11 → SQL |
| 13:00–14:15 | Kiểm thử, đối chiếu mục tiêu, hạn chế | Terminal → Slide 12–14 |
| 14:15–15:00 | Kết | Slide 15 |

## 2. Chuẩn bị trước khi quay

**Cơ sở dữ liệu**

- [ ] Cài lại DB **trong ngày quay** bằng `database\install_all.bat`. Dữ liệu mẫu tạo lịch chạy 7 ngày tính từ lúc cài. Bản đang có trên máy cài từ 18/10 nên hiện không còn chuyến nào mở bán, và `book_ticket` sẽ báo ORA-20010.
- [ ] Đăng nhập app lần lượt bằng `operator01`, `seller01`, `seller02` để đổi mật khẩu tạm (tài khoản mới tạo đều ở trạng thái PASSWORD EXPIRE). Ghi lại mật khẩu mới.

**SQL Developer**

- [ ] Cài SQL Developer, hoặc VS Code kèm extension Oracle SQL Developer.
- [ ] Tools → Preferences → Database → Advanced: **bỏ chọn Autocommit**. Demo 2 sẽ hỏng nếu để autocommit.
- [ ] Tools → Preferences → Code Editor → Fonts: cỡ 18. Đặt lưới kết quả cũng cỡ chữ lớn.
- [ ] Tạo 3 kết nối tới `localhost:1521`, service name `XEPDB1`: TRANSPORT, seller01, SYS (Role: SYSDBA).
- [ ] Mở 3 file `.sql` trên đúng kết nối. Chạy khối `[0]`, chọn một chuyến **khởi hành ngày mai**, ghế 10–12 phải còn trống (dữ liệu mẫu chỉ bán từ ghế 1 đến ghế 9). Sửa `DEFINE sid = …` trong `demo_transport.sql` và `demo_seller01.sql`, rồi chạy dòng DEFINE ở từng worksheet.
- [ ] Mở sẵn worksheet phụ cho phiên B của Demo 2a: trên kết nối TRANSPORT bấm Ctrl+Shift+N (unshared worksheet, tức là một phiên riêng). Chạy `DEFINE sid = …` trong worksheet này.
- [ ] Mở sẵn code để giải thích: `database\04_packages.sql` (dòng 19–48, `book_ticket`) và `database\03_triggers.sql` (dòng 6–58, trigger kiểm tra ghế). Có thể mở bằng SQL Developer hoặc VS Code.

**Cách chạy câu lệnh trong SQL Developer**

- Câu SQL đơn: đặt con trỏ vào câu rồi bấm Ctrl+Enter.
- Khối PL/SQL `DECLARE … END; /` và `EXEC`: bôi đen cả khối rồi bấm F5 (Run Script).

**App và slide**

- [ ] `dotnet run --project src/Transport.Presentation.Wpf`, đăng nhập sẵn bằng `seller02`, mở màn Bán vé.
- [ ] Mở `bao-cao-quan-ly-van-tai\output\slide.pptx`.

**Máy quay**

- [ ] Màn hình 1920×1080. Bật Focus Assist để tắt thông báo, ẩn icon desktop.
- [ ] OBS 1080p 30fps, thu thử micro. Nên **quay từng đoạn thành file riêng** rồi ghép lại: lỗi ở đoạn nào thì chỉ quay lại đoạn đó.
- [ ] Tập dượt một lượt, sau đó chạy khối `[9]` để dọn dữ liệu demo rồi mới quay thật.

## 3. Kịch bản chi tiết

### 0:00–1:00 · Mở đầu (slide 1–3)

**Thao tác:** chiếu slide 1, chuyển sang 2, rồi 3.

**Lời thoại:**
> Em xin trình bày đề tài cơ sở dữ liệu quản lý vận tải và đặt vé xe, cài đặt trên Oracle 21c XE. Hệ thống quản lý tuyến, xe, tài xế, lịch chạy, hành khách, vé và thanh toán. Với học phần này, em tập trung vào phần hệ quản trị: các quy tắc nghiệp vụ được đặt trong trigger, package và view, việc phân quyền dùng chính user và role của Oracle, sao lưu dùng job của DBMS_SCHEDULER và Data Pump. Trong video, mỗi tính năng em sẽ thao tác trên ứng dụng trước, sau đó kiểm tra lại bằng câu lệnh SQL.

### 1:00–2:30 · Kiến trúc và mô hình dữ liệu (slide 4–6)

**Thao tác:** slide 4 (kiến trúc), slide 5 (ERD), slide 6 (bảng đối tượng Oracle). Ở slide 6 dừng lâu hơn.

**Lời thoại:**
> Ứng dụng desktop viết bằng WPF, kết nối bằng ODP.NET. Mỗi nhân viên đăng nhập bằng một user Oracle riêng, nên Oracle biết ai đang thao tác và áp quyền theo user đó. Ứng dụng không tự viết logic bán vé: nó gọi thủ tục trong package và đọc dữ liệu qua view.
>
> Mô hình dữ liệu xoay quanh bảng TICKETS. Mỗi vé thuộc một lịch chạy và một hành khách. Bảng PAYMENTS lưu cả dòng thanh toán lẫn dòng hoàn tiền, nên doanh thu thực bằng tổng thanh toán trừ đi hoàn tiền.
>
> Bảng này liệt kê các đối tượng trong schema TRANSPORT. Phần demo tiếp theo sẽ đi qua những đối tượng quan trọng nhất: trigger kiểm tra ghế, package bán vé, hai role, profile mật khẩu, view thống kê và job sao lưu.

### 2:30–5:00 · Demo 1: bán vé và trigger kiểm tra ghế

**Thao tác 1 (app, khoảng 45 giây):** màn Bán vé → lọc ngày mai → chọn chuyến có mã `sid` → bấm ghế **10** → nhập SĐT `0988000001`, họ tên `Nguyễn Văn Demo` → Đặt vé → thanh toán tiền mặt.

> Em đang đăng nhập bằng seller02, là nhân viên bán vé. Em chọn chuyến ngày mai, chọn ghế số 10, nhập khách mới rồi đặt vé và thu tiền mặt.

**Thao tác 2 (SQL, worksheet TRANSPORT):** chạy `[1a]`.

> Trong cơ sở dữ liệu đã có vé ghế 10, cột SOLD_BY ghi SELLER02, tức là chính user Oracle vừa bán vé. Giá trị này được gán bằng DEFAULT USER, ứng dụng không cần gửi lên. Bảng thanh toán cũng có một dòng PAYMENT.

**Thao tác 3:** mở `04_packages.sql`, cuộn tới `book_ticket`, bôi đen lần lượt từng đoạn.

> Khi bấm Đặt vé, ứng dụng chỉ gọi thủ tục book_ticket. Câu lệnh đầu tiên khoá dòng lịch chạy bằng SELECT FOR UPDATE, phần này em sẽ demo kỹ ở đoạn sau. Tiếp theo là kiểm tra chuyến đã khởi hành chưa. MERGE tìm hành khách theo số điện thoại, nếu chưa có thì thêm mới. Cuối cùng là INSERT vé, lấy mã vé bằng RETURNING rồi COMMIT. Ở đây không có câu nào kiểm tra ghế, việc đó do trigger đảm nhận.

**Thao tác 4:** chạy `[1b]` → ORA-20003, chạy `[1c]` → ORA-20001.

> Bây giờ em đăng nhập bằng chủ schema và bỏ qua package, INSERT thẳng vào bảng một vé ghế 10. Oracle báo ORA-20003, ghế đã có người đặt. Thử ghế 99 thì báo ORA-20001, vì xe chỉ có 40 ghế. Kể cả chủ schema cũng không thể ghi một vé sai.

**Thao tác 5:** mở `03_triggers.sql`, chỉ vào `COMPOUND TRIGGER`, `AFTER EACH ROW`, `AFTER STATEMENT`.

> Đây là một compound trigger. Muốn biết ghế đã bán hay chưa thì phải đếm lại bảng TICKETS, nhưng trigger mức dòng không được đọc bảng đang bị sửa, nếu không sẽ gặp lỗi mutating table ORA-04091. Vì thế phần AFTER EACH ROW chỉ gom mã vé vào một collection. Sang phần AFTER STATEMENT, câu lệnh INSERT đã chạy xong, trigger mới đếm và kiểm tra sức chứa xe, trạng thái chuyến và ghế trùng.

### 5:00–8:00 · Demo 2: hai quầy tranh một ghế

**Thao tác 1:** chiếu slide 8 khoảng 10 giây.

> Trigger kiểm tra ghế, nhưng khi hai quầy bấm bán cùng một lúc thì trigger có đủ không? Em thử ngay trên hai phiên.

**Demo 2a: không khoá chuyến (khoảng 1 phút 15 giây).** Đặt hai worksheet TRANSPORT cạnh nhau (A bên trái, B bên phải).

1. A chạy `[2a-A1]`, chèn ghế 12 nhưng chưa commit.
2. B dán câu INSERT của `[2a-B1]` và chạy, kết quả "1 row inserted". Sau đó B chạy `COMMIT`.
3. A chạy `COMMIT`, rồi chạy truy vấn đếm ghế: ghế 12 có **2 vé hiệu lực**.

> Phiên A chèn ghế 12 nhưng chưa commit. Phiên B cũng chèn ghế 12 và vẫn thành công. Lý do là Oracle mặc định chạy ở mức READ COMMITTED: khi trigger của phiên B đếm, nó không thấy dòng chưa commit của phiên A, nên đếm ra chỉ có một vé. Sau khi cả hai commit, ghế 12 có hai vé. Như vậy chỉ dựa vào trigger là không đủ.

4. A chạy `[2a-A3]` để xoá hai vé trùng.

**Demo 2b: qua book_ticket (khoảng 1 phút 30 giây).**

1. A (TRANSPORT) chạy `[2b-A1]`, giữ khoá dòng chuyến.
2. Worksheet seller01: bôi đen `[2b-B1]` rồi bấm F5. Câu lệnh **treo**.
3. Worksheet SYS chạy `[2b-S1]`: BLOCKING_SESSION là SID của phiên A, sự kiện chờ là `enq: TX - row lock contention`. Truy vấn `v$lock` cho thấy A giữ TX ở mode 6, seller01 đang xin TX mode 6.
4. Quay về A, chạy `[2b-A2]` (bán ghế 11 rồi COMMIT). Ngay lúc đó worksheet seller01 chạy xong và báo **ORA-20003**.
5. A chạy `[2b-A3]`: ghế 11 chỉ có một vé.

> Lần này em làm đúng như thủ tục book_ticket. Phiên A khoá dòng lịch chạy bằng SELECT FOR UPDATE. Quầy seller01 gọi book_ticket cho ghế 11 và bị treo. Xem từ phía SYS: v$session cho biết phiên này đang bị phiên A chặn, sự kiện chờ là row lock contention. Khi phiên A bán ghế 11 và commit, khoá được nhả, seller01 chạy tiếp. Lúc này trigger đã thấy vé của A nên báo ORA-20003. Khoá dòng chuyến buộc các quầy bán cùng một chuyến phải xếp hàng. Vì chỉ khoá một dòng, các chuyến khác vẫn bán song song bình thường.

### 8:00–10:00 · Demo 3: phân quyền, profile, kiểm toán

**Thao tác 1:** slide 9 khoảng 15 giây.

> Hai role chính là ROLE_OPERATOR và ROLE_TICKET_SELLER. Người bán vé chỉ ghi được bảng hành khách, mọi thao tác với vé đều phải đi qua package pkg_ticket.

**Thao tác 2 (app):** đăng xuất → đăng nhập `seller02` với **mật khẩu sai** → app báo lỗi → đăng nhập lại đúng mật khẩu → chỉ ra menu không có Phương tiện, Tài xế, Nhân viên, Nhật ký đăng nhập, Sao lưu.

**Thao tác 3 (SQL, worksheet seller01):** chạy `[3a]` đến `[3e]`.

> Trong worksheet seller01, role đang bật là ROLE_TICKET_SELLER. Đọc bảng tài xế thì báo ORA-00942, vì user này không được cấp quyền gì trên bảng đó. Bảng vé thì được SELECT nhưng INSERT bị ORA-01031. Gọi package quản trị nhân viên thì báo PLS-00201. Cửa được mở là view thống kê, đọc bình thường. Như vậy giao diện ẩn menu chỉ là lớp ngoài, người dùng có tự kết nối bằng công cụ khác thì Oracle vẫn chặn.

**Thao tác 4:** slide 10 khoảng 10 giây. Worksheet SYS chạy `[3-S1]`, `[3-S2]`. Worksheet TRANSPORT chạy `[3a]`.

> Nhân viên dùng PROFILE_STAFF: sai năm lần thì khoá tài khoản, mật khẩu hết hạn sau 90 ngày, không được dùng lại năm mật khẩu gần nhất, và có hàm kiểm tra độ phức tạp. Hai policy Unified Auditing đang bật. Lần đăng nhập sai vừa rồi trên ứng dụng đã được ghi lại với mã 1017. Người điều hành xem được nhật ký này qua view v_login_audit.

### 10:00–11:30 · Demo 4: view thống kê và kế hoạch thực thi

**Thao tác 1 (app):** mở màn Thống kê.

**Thao tác 2 (SQL, TRANSPORT):** chạy `[4a]`, sau đó `[4b]` (EXPLAIN PLAN rồi DBMS_XPLAN).

> Màn hình thống kê đọc từ view v_trip_statistics: số ghế đã bán, ghế trống, tỷ lệ lấp đầy, số vé huỷ và doanh thu thực. Bảng thanh toán được gộp theo vé trước khi join, để một vé có cả dòng thanh toán lẫn dòng hoàn tiền không làm doanh thu bị nhân đôi.
>
> Câu truy vấn được gọi nhiều nhất là truy vấn sơ đồ ghế, chạy mỗi lần nhân viên chọn chuyến. Kế hoạch thực thi cho thấy bộ tối ưu chọn INDEX RANGE SCAN trên chỉ mục IDX_TICKETS_SCHEDULE thay vì quét toàn bảng. Trong báo cáo em có đo thêm trên bảng 450 nghìn vé: khi không có chỉ mục, số lượt đọc logic mỗi lần truy vấn lớn hơn nhiều.

### 11:30–13:00 · Demo 5: sao lưu

**Thao tác:** slide 11 khoảng 20 giây, sau đó SQL (TRANSPORT) chạy `[5a]` → `[5b]` → `[5c]` → `[5d]`.

> Sao lưu có hai lớp. Lớp trong cơ sở dữ liệu là thủ tục backup_operational_data, chụp lịch chạy, vé và thanh toán sang các bảng sao lưu. Job JOB_NIGHTLY_BACKUP gọi thủ tục này lúc 2 giờ sáng mỗi ngày, và job đang ở trạng thái SCHEDULED. Em chạy tay một lần: BACKUP_LOGS có thêm một dòng ghi số dòng đã chụp, và số vé trong bản chụp khớp với bảng thật. Lớp thứ hai là Data Pump, dùng backup.bat và restore.bat để xuất và nhập cả schema ra tệp. Lớp này dùng khi mất cả cơ sở dữ liệu, vì bản chụp nằm cùng cơ sở dữ liệu thì cũng mất theo.

### 13:00–14:15 · Kiểm thử, đối chiếu, hạn chế

**Thao tác 1 (terminal):** cuộn qua kết quả của `09_test_scenarios.sql`, chỉ vào các dòng PASS.

```powershell
$env:NLS_LANG = 'AMERICAN_AMERICA.AL32UTF8'
sqlplus transport/<mật khẩu>@//localhost:1521/XEPDB1 @database/09_test_scenarios.sql
```

> Ngoài phần demo, các kịch bản nghiệp vụ được viết thành script tự kiểm tra. Script tự tạo dữ liệu thử, in PASS hoặc FAIL cho từng kịch bản, rồi tự dọn.

**Thao tác 2:** slide 12, 13, 14, mỗi slide khoảng 10 giây.

> Đây là bảng tổng hợp kết quả kiểm thử và đối chiếu với các yêu cầu của đề tài. Hệ thống vẫn còn hạn chế: chưa đo tải khi nhiều quầy cùng bán một chuyến, hoàn tiền đang cố định 100% bằng tiền mặt, và Data Pump vẫn chạy thủ công.

### 14:15–15:00 · Kết (slide 15)

> Tóm lại, các ràng buộc quan trọng nhất của bài toán bán vé được đặt trong Oracle: trigger chặn vé sai, khoá dòng xử lý tranh chấp giữa các quầy, role và profile kiểm soát quyền truy cập, còn job và Data Pump lo phần sao lưu. Em xin cảm ơn thầy cô đã theo dõi.

## 4. Dự phòng khi demo lỗi

| Sự cố | Cách xử lý |
|---|---|
| `[0]` không trả về chuyến nào, hoặc book_ticket báo ORA-20010 | Lịch chạy đã qua ngày. Cài lại bằng `install_all.bat` |
| Ghế 10/11/12 đã có người đặt | Chạy khối `[9]` để dọn, hoặc chọn chuyến khác rồi sửa DEFINE |
| Demo 2a: phiên B bị treo thay vì chèn được | Phiên A vừa chạy câu FOR UPDATE của 2b. Bấm COMMIT ở A rồi làm lại |
| Demo 2a: không thấy ghế trùng | Autocommit đang bật nên phiên A đã commit trước. Tắt Autocommit, chạy `[2a-A3]` rồi làm lại |
| Demo 2b: seller01 không treo | Phiên A chưa chạy `[2b-A1]`, hoặc A đã commit. Chạy lại `[2b-A1]` |
| SQL Developer hỏi giá trị `sid` | Chưa chạy dòng `DEFINE sid = …` trong worksheet đó |
| Kết nối seller01 báo mật khẩu hết hạn | Đổi mật khẩu trong hộp thoại, hoặc đăng nhập app một lần để đổi |
| Quá giờ | Rút ngắn Demo 4 (bỏ màn app) và phần kiểm thử (chỉ chiếu slide 12) |

## 5. Sau khi quay

- Chạy khối `[9]` trong `demo_transport.sql` để xoá vé, thanh toán và hành khách demo (các dòng gắn với SĐT `098800000x`), hoặc cài lại DB.
