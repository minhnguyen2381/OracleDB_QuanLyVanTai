# Sửa lỗi font tiếng Việt trong dữ liệu – Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Đưa 170 giá trị tiếng Việt bị lỗi mã hóa kép trong schema TRANSPORT về đúng chữ (vd `HÃ  Ná»™i` → `Hà Nội`), và chặn không cho `08_seed_data.sql` ghi dữ liệu lỗi lần nữa.

**Architecture:** Lỗi sinh ra khi sqlplus chạy script UTF-8 với `NLS_LANG` mặc định trong registry (`AMERICAN_AMERICA.WE8MSWIN1252`). Khi đó từng byte UTF-8 bị hiểu là một ký tự Windows-1252 và được lưu lại thành UTF-8 thêm một lần nữa. Phép đảo ngược là `CONVERT(col, 'WE8MSWIN1252', 'AL32UTF8')`. Script này chạy một lần, không lưu vào repo, và chỉ cập nhật những dòng có dấu hiệu lỗi. Chốt chặn đặt ở đầu `08_seed_data.sql`: nếu `LENGTH('Đ') <> 1` (tức client đọc sai bảng mã) thì dừng ngay, trước câu INSERT đầu tiên.

**Tech Stack:** Oracle 21c XE (PDB `XEPDB1`, charset `AL32UTF8`), SQL*Plus, PowerShell (Windows).

**Spec:** Thiết kế đã duyệt trong chat ngày 2026-10-18 (tóm tắt ở mục "Quyết định").

## Quyết định (đã chốt với người dùng)

- **Sửa tại chỗ**, không cài lại bằng `install_all.bat`, để giữ các tài khoản tự tạo (`MINHNN2381`, `MINHNN2381A`) và dữ liệu nhập qua app.
- Dữ liệu nhập qua app (ODP.NET managed, luôn Unicode) vẫn đúng, vd `Khách Test` và tên nhân viên. Không được đụng vào.
- Script sửa dữ liệu chỉ chạy một lần và **không commit** vào repo.
- **Bỏ qua** comment tiếng Việt bị lỗi trong source PL/SQL trên DB (33 dòng `DBA_SOURCE`). Đây chỉ là comment, code chạy đúng, và không có string literal tiếng Việt nào trong 03–06/10. Lần cài lại sau sẽ tự sạch.
- `09_test_scenarios.sql` không có literal tiếng Việt ngoài comment, nên không cần chốt chặn.

## Số liệu đã khảo sát (trước khi sửa)

| Bảng.cột | Dòng lỗi | Đảo được |
|---|---|---|
| ROUTES.ROUTE_NAME | 5 | 5 |
| ROUTES.ORIGIN | 5 | 5 |
| ROUTES.DESTINATION | 4 | 4 |
| VEHICLES.VEHICLE_TYPE | 6 | 6 |
| DRIVERS.FULL_NAME | 6 | 6 |
| PASSENGERS.FULL_NAME | 30 | 30 |
| TICKETS.CANCEL_REASON | 57 | 57 |
| TICKETS_BK.CANCEL_REASON | 57 | 57 |

Các trigger trong `03_triggers.sql` chỉ bắt `UPDATE OF seat_no, schedule_id, status` (tickets) và `UPDATE OF status` (schedules). Các câu UPDATE dưới đây không đụng những cột đó nên không kích hoạt trigger.

## Global Constraints

- Định danh SQL và thông điệp `RAISE_APPLICATION_ERROR` bằng **tiếng Anh**; comment bằng **tiếng Việt**.
- Mọi lệnh chạy bằng **PowerShell**, không dùng Git Bash: Git Bash biến tham số `/ as sysdba` thành đường dẫn.
- Kết nối quản trị dùng xác thực OS: `sqlplus -S -L "/ as sysdba"`. Mật khẩu TRANSPORT không lưu ở đâu cả, nên không dùng tới.
- Trước mỗi lần chạy sqlplus đọc/ghi tiếng Việt phải đặt `$env:NLS_LANG='AMERICAN_AMERICA.AL32UTF8'`.
- Mã lỗi chốt chặn: `-20998`. `-20999` đã dùng cho kiểm tra biên dịch trong `03_triggers.sql`.
- Theo `CLAUDE.md`: chạy `node .gitnexus/run.cjs detect-changes --scope all --repo .` trước khi commit.

---

### Task 1: Sửa dữ liệu đang lỗi trong DB

**Files:**
- Create (ngoài repo): `$env:TEMP\survey_mojibake.sql`, `$env:TEMP\fix_mojibake.sql`
- Không sửa file nào trong repo.

**Interfaces:**
- Consumes: không có.
- Produces: DB không còn giá trị lỗi. Task 3 dùng lại `survey_mojibake.sql`.

- [ ] **Step 1: Viết script khảo sát (đóng vai "test")**

Script này quét mọi cột chữ của TRANSPORT và in ra những cột còn dòng lỗi. Khi DB sạch, nó không in dòng nào.

```powershell
$survey = @'
WHENEVER SQLERROR EXIT FAILURE
SET SERVEROUTPUT ON
ALTER SESSION SET CONTAINER = XEPDB1;
-- In các cột còn chứa chuỗi lỗi mã hóa kép (Ã, Ä, Å, Æ, á» , áº)
DECLARE
  n NUMBER;
BEGIN
  FOR c IN (SELECT c.table_name, c.column_name
              FROM dba_tab_columns c
              JOIN dba_tables t ON t.owner = c.owner AND t.table_name = c.table_name
             WHERE c.owner = 'TRANSPORT' AND c.data_type IN ('VARCHAR2', 'CHAR', 'CLOB')) LOOP
    EXECUTE IMMEDIATE 'SELECT COUNT(*) FROM transport.' || c.table_name ||
      ' WHERE REGEXP_LIKE(' || c.column_name || ', UNISTR(''[\00C3\00C4\00C5\00C6]|\00E1[\00BA\00BB]''))' INTO n;
    IF n > 0 THEN
      DBMS_OUTPUT.PUT_LINE('BAD ' || RPAD(c.table_name || '.' || c.column_name, 32) || n);
    END IF;
  END LOOP;
END;
/
EXIT
'@
[IO.File]::WriteAllText("$env:TEMP\survey_mojibake.sql", $survey)
```

- [ ] **Step 2: Chạy khảo sát, xác nhận đang "FAIL"**

```powershell
$env:NLS_LANG='AMERICAN_AMERICA.AL32UTF8'
sqlplus -S -L "/ as sysdba" "@$env:TEMP\survey_mojibake.sql"
```

Expected: 8 dòng `BAD ...`, khớp với bảng số liệu ở trên (ROUTES.ROUTE_NAME 5, …, TICKETS_BK.CANCEL_REASON 57). Nếu số khác bảng thì DỪNG và báo lại: có thể dữ liệu đã đổi kể từ lúc khảo sát.

- [ ] **Step 3: Viết script sửa**

Toàn bộ script là một transaction. Sau khi UPDATE từng cột, script đếm lại số dòng còn lỗi. Còn sót dòng nào thì raise lỗi, và `WHENEVER SQLERROR` sẽ ROLLBACK tất cả.

```powershell
$fix = @'
WHENEVER SQLERROR EXIT FAILURE ROLLBACK
SET SERVEROUTPUT ON
ALTER SESSION SET CONTAINER = XEPDB1;
-- Đảo ngược lỗi mã hóa kép (UTF-8 bị đọc thành Windows-1252), chỉ trên các dòng có dấu hiệu lỗi
DECLARE
  TYPE t_cols IS TABLE OF VARCHAR2(61);
  v_cols t_cols := t_cols('ROUTES.ROUTE_NAME', 'ROUTES.ORIGIN', 'ROUTES.DESTINATION', 'VEHICLES.VEHICLE_TYPE',
                          'DRIVERS.FULL_NAME', 'PASSENGERS.FULL_NAME', 'TICKETS.CANCEL_REASON', 'TICKETS_BK.CANCEL_REASON');
  c_bad CONSTANT VARCHAR2(100) := UNISTR('[\00C3\00C4\00C5\00C6]|\00E1[\00BA\00BB]');
  v_tab  VARCHAR2(30);
  v_col  VARCHAR2(30);
  v_left NUMBER;
BEGIN
  FOR i IN 1 .. v_cols.COUNT LOOP
    v_tab := REGEXP_SUBSTR(v_cols(i), '[^.]+', 1, 1);
    v_col := REGEXP_SUBSTR(v_cols(i), '[^.]+', 1, 2);
    EXECUTE IMMEDIATE 'UPDATE transport.' || v_tab || ' SET ' || v_col || ' = CONVERT(' || v_col ||
      ', ''WE8MSWIN1252'', ''AL32UTF8'') WHERE REGEXP_LIKE(' || v_col || ', :p)' USING c_bad;
    DBMS_OUTPUT.PUT_LINE(RPAD(v_cols(i), 30) || SQL%ROWCOUNT || ' rows fixed');
    EXECUTE IMMEDIATE 'SELECT COUNT(*) FROM transport.' || v_tab || ' WHERE REGEXP_LIKE(' || v_col || ', :p)'
      INTO v_left USING c_bad;
    IF v_left > 0 THEN
      RAISE_APPLICATION_ERROR(-20998, v_cols(i) || ' still has ' || v_left || ' broken rows.');
    END IF;
  END LOOP;
END;
/
COMMIT;
EXIT
'@
[IO.File]::WriteAllText("$env:TEMP\fix_mojibake.sql", $fix)
```

- [ ] **Step 4: Chạy script sửa**

```powershell
$env:NLS_LANG='AMERICAN_AMERICA.AL32UTF8'
sqlplus -S -L "/ as sysdba" "@$env:TEMP\fix_mojibake.sql"; "exit=$LASTEXITCODE"
```

Expected: 8 dòng `... rows fixed` với các số 5, 5, 4, 6, 6, 30, 57, 57; `PL/SQL procedure successfully completed.`; `Commit complete.`; `exit=0`. Nếu exit khác 0 thì đã ROLLBACK, DB vẫn như cũ. Khi đó DỪNG và báo lỗi in ra.

- [ ] **Step 5: Chạy lại khảo sát, xác nhận "PASS"**

```powershell
sqlplus -S -L "/ as sysdba" "@$env:TEMP\survey_mojibake.sql"
```

Expected: không có dòng `BAD` nào, chỉ có `PL/SQL procedure successfully completed.`

- [ ] **Step 6: Soi tận mắt vài giá trị**

```powershell
$check = @'
SET LINES 120 PAGES 100
ALTER SESSION SET CONTAINER = XEPDB1;
SELECT route_name FROM transport.routes ORDER BY route_id;
SELECT DISTINCT vehicle_type FROM transport.vehicles;
SELECT full_name FROM transport.drivers ORDER BY driver_id;
SELECT full_name FROM transport.passengers WHERE full_name LIKE UNISTR('%\0110%') OR full_name LIKE 'Kh%';
SELECT DISTINCT cancel_reason FROM transport.tickets WHERE cancel_reason IS NOT NULL;
EXIT
'@
[IO.File]::WriteAllText("$env:TEMP\check_text.sql", $check)
sqlplus -S -L "/ as sysdba" "@$env:TEMP\check_text.sql" | Out-File -Encoding utf8 "$env:TEMP\check_text.txt"
Get-Content -Encoding utf8 "$env:TEMP\check_text.txt"
```

Expected (đọc qua file UTF-8 để console không tự làm hỏng chữ):
- Tuyến: `Hà Nội - Lào Cai`, `Hà Nội - Hải Phòng`, `Hà Nội - Ninh Bình`, `Hà Nội - Sa Pa`, `Hà Nội - Thanh Hóa`
- Loại xe: `Giường nằm 40 chỗ`, `Ghế ngồi 45 chỗ`, `Limousine 16 chỗ`, `Ghế ngồi 29 chỗ`
- Tài xế: `Nguyễn Văn An`, `Trần Văn Bình`, `Lê Văn Cường`, `Phạm Văn Dũng`, `Hoàng Văn Em`, `Vũ Văn Phúc`
- Hành khách có `Đ`: `Vũ Đức Thắng`, `Đặng Thùy Linh`, `Đỗ Ngọc Ánh`, `Đinh Công Sơn`, … và `Khách Test` vẫn nguyên.
- Lý do hủy: `Khách đổi kế hoạch`

Không có commit ở task này (không đổi file nào trong repo).

---

### Task 2: Chốt chặn sai bảng mã trong `08_seed_data.sql`

**Files:**
- Modify: `database/08_seed_data.sql:2-5` (chèn khối kiểm tra ngay sau các dòng `SET`, trước `-- Tuyến xe`)
- Test (ngoài repo): `$env:TEMP\seed_guard_test.sql`

**Interfaces:**
- Consumes: không có.
- Produces: script dừng với `ORA-20998` khi client không phải UTF-8.

Cách test an toàn: chạy chính file `08_seed_data.sql` trong container `CDB$ROOT`. Ở đó không có schema TRANSPORT, nên mọi câu INSERT đều lỗi `ORA-00942` và không ghi được gì. Nhờ vậy:
- chưa có chốt chặn → dừng ở INSERT đầu tiên với `ORA-00942`;
- có chốt chặn + sai bảng mã → dừng sớm hơn với `ORA-20998`;
- có chốt chặn + đúng bảng mã → qua được chốt, rồi dừng ở `ORA-00942`.

- [ ] **Step 1: Viết test wrapper**

```powershell
$wrapper = @"
ALTER SESSION SET CONTAINER = CDB`$ROOT;
@"$PWD\database\08_seed_data.sql"
"@
[IO.File]::WriteAllText("$env:TEMP\seed_guard_test.sql", $wrapper)
```

(Chạy từ thư mục gốc repo `D:\DuAn\OracleDB_QuanLyVanTai` để `$PWD` đúng.)

- [ ] **Step 2: Chạy với bảng mã sai, xác nhận FAIL (chưa có chốt)**

```powershell
$env:NLS_LANG='AMERICAN_AMERICA.WE8MSWIN1252'
sqlplus -S -L "/ as sysdba" "@$env:TEMP\seed_guard_test.sql"
```

Expected: có `ORA-00942: table or view does not exist`, KHÔNG có `ORA-20998`. Tức là hiện tại script vẫn chạy tới INSERT dù bảng mã sai.

- [ ] **Step 3: Thêm chốt chặn**

Trong `database/08_seed_data.sql`, sau dòng `SET SERVEROUTPUT ON SIZE UNLIMITED` và trước dòng trống + `-- Tuyến xe`, chèn:

```sql

-- Chặn chạy sai bảng mã: nếu sqlplus đọc file UTF-8 này với NLS_LANG khác AL32UTF8 thì 'Đ' thành 2 ký tự
-- và mọi chữ tiếng Việt bị lưu sai (vd "HÃ  Ná»™i"). install_all.bat đã đặt NLS_LANG đúng.
BEGIN
  IF LENGTH('Đ') <> 1 THEN
    RAISE_APPLICATION_ERROR(-20998, 'Client character set is not UTF-8. Set NLS_LANG=AMERICAN_AMERICA.AL32UTF8 and rerun.');
  END IF;
END;
/
```

Đầu file sau khi sửa sẽ là:

```sql
-- Dữ liệu mẫu để demo (chạy bằng TRANSPORT, sau 07_grants.sql)
WHENEVER SQLERROR EXIT FAILURE ROLLBACK
SET DEFINE OFF
SET SQLBLANKLINES ON
SET SERVEROUTPUT ON SIZE UNLIMITED

-- Chặn chạy sai bảng mã: ...
-- và mọi chữ tiếng Việt ...
BEGIN
  IF LENGTH('Đ') <> 1 THEN
    RAISE_APPLICATION_ERROR(-20998, 'Client character set is not UTF-8. Set NLS_LANG=AMERICAN_AMERICA.AL32UTF8 and rerun.');
  END IF;
END;
/

-- Tuyến xe
INSERT INTO routes ...
```

File phải được lưu ở dạng **UTF-8 không BOM**, giống hiện tại. Kiểm tra bằng `file database/08_seed_data.sql` trong Git Bash, kết quả phải là `Unicode text, UTF-8 text`.

- [ ] **Step 4: Chạy lại với bảng mã sai, xác nhận PASS**

```powershell
$env:NLS_LANG='AMERICAN_AMERICA.WE8MSWIN1252'
sqlplus -S -L "/ as sysdba" "@$env:TEMP\seed_guard_test.sql"
```

Expected: có `ORA-20998: Client character set is not UTF-8. Set NLS_LANG=AMERICAN_AMERICA.AL32UTF8 and rerun.` và KHÔNG có `ORA-00942`.

- [ ] **Step 5: Chạy với bảng mã đúng, xác nhận chốt không chặn nhầm**

```powershell
$env:NLS_LANG='AMERICAN_AMERICA.AL32UTF8'
sqlplus -S -L "/ as sysdba" "@$env:TEMP\seed_guard_test.sql"
```

Expected: KHÔNG có `ORA-20998`; có `ORA-00942` (đã qua chốt và dừng ở INSERT đầu tiên trong `CDB$ROOT`).

- [ ] **Step 6: Kiểm tra thay đổi và commit**

```powershell
node .gitnexus/run.cjs detect-changes --scope all --repo .
git add database/08_seed_data.sql docs/superpowers/plans/2026-10-18-plan-7-fix-vietnamese-text.md
git commit -m "fix(db): stop seed script when client charset is not UTF-8"
```

Expected của detect-changes: chỉ `database/08_seed_data.sql` (cùng file plan), không có symbol C# nào bị ảnh hưởng. Nếu kết quả có `partial`/`truncated` thì chạy lại.

---

### Task 3: Kiểm tra trên app

**Files:** không có.

**Interfaces:**
- Consumes: DB đã sửa (Task 1).
- Produces: xác nhận bằng mắt.

- [ ] **Step 1: Chạy app**

```powershell
dotnet run --project src/Transport.Presentation.Wpf
```

- [ ] **Step 2: Soi các màn hình**

Đăng nhập bằng `operator01` hoặc tài khoản `MINHNN2381`, rồi mở lần lượt:
- **Tuyến xe**: tên tuyến, điểm đi, điểm đến có dấu đúng.
- **Phương tiện**: loại xe đúng chữ.
- **Tài xế**: 6 tên đúng chữ.
- **Hành khách**: tên có `Đ`, `ư`, `ơ` đúng chữ.
- **Vé**: lọc vé đã hủy, lý do hủy là `Khách đổi kế hoạch`.
- **Thống kê** và **Dashboard**: tên tuyến đúng chữ (lấy qua view `V_TRIP_STATISTICS`).

Expected: không còn chuỗi kiểu `Ã`, `á»`, `Æ°` ở bất kỳ màn hình nào.

- [ ] **Step 3: Chạy lại khảo sát lần cuối**

```powershell
$env:NLS_LANG='AMERICAN_AMERICA.AL32UTF8'
sqlplus -S -L "/ as sysdba" "@$env:TEMP\survey_mojibake.sql"
```

Expected: không có dòng `BAD`.
