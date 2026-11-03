-- Video demo: worksheet kết nối SYS AS SYSDBA, service XEPDB1.

-- ===== DEMO 2b: ai đang chờ ai =====
-- [2b-S1] Chạy khi phiên seller01 đang treo: BLOCKING_SESSION là SID của phiên A
SELECT sid, username, blocking_session, event, seconds_in_wait
  FROM v$session
 WHERE blocking_session IS NOT NULL;

-- Khoá đang giữ trên bảng SCHEDULES (TM mode 3 = row exclusive) và khoá giao dịch TX
SELECT l.sid, s.username, l.type, l.lmode, l.request, l.block
  FROM v$lock l JOIN v$session s ON s.sid = l.sid
 WHERE s.username IN ('TRANSPORT', 'SELLER01') AND l.type IN ('TM', 'TX')
 ORDER BY l.sid, l.type;


-- ===== DEMO 3: profile mật khẩu áp cho nhân viên =====
-- [3-S1]
SELECT resource_name, limit
  FROM dba_profiles
 WHERE profile = 'PROFILE_STAFF' AND resource_type = 'PASSWORD'
 ORDER BY resource_name;

-- [3-S2] Hai policy Unified Auditing đang bật
SELECT policy_name, enabled_option, success, failure
  FROM audit_unified_enabled_policies
 WHERE policy_name LIKE 'POL_TRANSPORT%';
