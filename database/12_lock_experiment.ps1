# TN2: tranh chấp khoá dòng chuyến giữa hai phiên (mô phỏng hai quầy cùng đặt vé một chuyến).
# Chạy trên máy chủ:  powershell -ExecutionPolicy Bypass -File 12_lock_experiment.ps1 [-OutDir <thư mục log>]
# Dùng xác thực hệ điều hành "/ as sysdba". Không sửa dữ liệu: phiên A khoá một dòng SCHEDULES bằng đúng
# câu SELECT ... FOR UPDATE của pkg_ticket.book_ticket, giữ 10 giây rồi ROLLBACK.
param([string]$OutDir = '.')
$ErrorActionPreference = 'Stop'
$env:NLS_LANG = 'AMERICAN_AMERICA.AL32UTF8'
$OutDir = (New-Item -ItemType Directory -Force $OutDir).FullName
$utf8 = New-Object System.Text.UTF8Encoding($false)
function Write-Sql($name, $body) {
  $path = Join-Path $OutDir $name
  [IO.File]::WriteAllText($path, "ALTER SESSION SET CONTAINER = XEPDB1;`n" + $body + "`nEXIT`n", $utf8)
  return $path
}

# Hai chuyến đang mở bán
$idsFile = Write-Sql 'tn2_ids.sql' @"
SET HEADING OFF FEEDBACK OFF PAGESIZE 0
SELECT schedule_id FROM (SELECT schedule_id FROM transport.schedules WHERE status = 'SCHEDULED' ORDER BY schedule_id) WHERE ROWNUM <= 2;
"@
$ids = @(& sqlplus -S -L "/ as sysdba" "@$idsFile" | Where-Object { $_ -match '^\s*\d+\s*$' } | ForEach-Object { $_.Trim() })
$s1 = $ids[0]; $s2 = $ids[1]
$lock = "SELECT ticket_price, departure_time FROM transport.schedules WHERE schedule_id ="
$head = "SET FEEDBACK ON TIMING ON LINESIZE 200 PAGESIZE 100`nWHENEVER SQLERROR CONTINUE`n"
$now = "SELECT TO_CHAR(SYSTIMESTAMP, 'HH24:MI:SS.FF2') AS t FROM dual;"

$a = Write-Sql 'tn2_a.sql' @"
$head
PROMPT A: khoa chuyen $s1 (giong book_ticket), giu 10 giay
SELECT sys_context('USERENV', 'SID') AS sid_a FROM dual;
$now
$lock $s1 FOR UPDATE;
EXEC DBMS_SESSION.SLEEP(10)
$now
ROLLBACK;
"@
$b = Write-Sql 'tn2_b.sql' @"
$head
SELECT sys_context('USERENV', 'SID') AS sid_b FROM dual;
PROMPT B1: chuyen khac ($s2)
$lock $s2 FOR UPDATE;
ROLLBACK;
PROMPT B2: cung chuyen $s1, NOWAIT
$lock $s1 FOR UPDATE NOWAIT;
ROLLBACK;
PROMPT B3: cung chuyen $s1, WAIT 3
$lock $s1 FOR UPDATE WAIT 3;
ROLLBACK;
PROMPT B4: cung chuyen $s1, cho khong gioi han (giong book_ticket)
$now
$lock $s1 FOR UPDATE;
$now
ROLLBACK;
"@
$c = Write-Sql 'tn2_c.sql' @"
SET LINESIZE 200 PAGESIZE 100
COLUMN event FORMAT A35
COLUMN object_name FORMAT A15
PROMPT C: phien dang bi chan
SELECT sid, event, blocking_session, seconds_in_wait FROM v`$session WHERE blocking_session IS NOT NULL;
PROMPT C: khoa cua hai phien
SELECT l.sid, l.type, l.lmode, l.request, l.block,
       CASE l.type WHEN 'TM' THEN (SELECT object_name FROM dba_objects WHERE object_id = l.id1) END AS object_name
  FROM v`$lock l
 WHERE l.type IN ('TX', 'TM')
   AND l.sid IN (SELECT sid FROM v`$session WHERE blocking_session IS NOT NULL
                 UNION SELECT blocking_session FROM v`$session WHERE blocking_session IS NOT NULL)
 ORDER BY l.sid, l.type;
"@

$run = { param($file, $log) Start-Process sqlplus -ArgumentList '-S', '-L', '"/ as sysdba"', "@$file" -NoNewWindow -PassThru -RedirectStandardOutput $log }
$pa = & $run $a (Join-Path $OutDir 'tn2_a.log'); Start-Sleep -Seconds 2
$pb = & $run $b (Join-Path $OutDir 'tn2_b.log'); Start-Sleep -Seconds 5
$pc = & $run $c (Join-Path $OutDir 'tn2_c.log')
$pa, $pb, $pc | Wait-Process -Timeout 60
foreach ($s in 'a', 'b', 'c') { "===== Phien $($s.ToUpper()) ====="; Get-Content (Join-Path $OutDir "tn2_$s.log") }
