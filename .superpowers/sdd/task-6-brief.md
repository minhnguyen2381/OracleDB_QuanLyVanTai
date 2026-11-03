# Task 6 Brief: Seed Data, Install/Backup Scripts, Full Reinstall and Permission Checks

**Plan Source:** `docs/superpowers/plans/2026-10-17-plan-1-database.md` Task 6 (lines 524-800)
**Spec Source:** `docs/superpowers/specs/2026-10-17-transport-management-design.md` §8.2, §13, §14

## Goal
Finalize the database schema with seed data, automated batch scripts, complete reinstall test, permission verification, and Data Pump export test.

## Requirements
1. **`database/01_sys_setup.sql` Update**:
   Add `GRANT SELECT ON sys.user_scheduler_jobs TO transport WITH GRANT OPTION;` so that fresh installs grant permissions on `v_backup_job_status` without ORA-01720.

2. **`database/08_seed_data.sql`**:
   Write seed data script (Plan 1 lines 540-690):
   - Standard header (WHENEVER SQLERROR EXIT FAILURE ROLLBACK...)
   - 5 routes (Hà Nội - Lào Cai, Hải Phòng, Ninh Bình, Sa Pa, Thanh Hóa)
   - 6 vehicles (incl. 29B-196.88; last one 35B-888.99 in MAINTENANCE)
   - 6 drivers (incl. Phạm Văn Dũng with license expiring in 20 days)
   - 30 passengers (Vietnamese names)
   - 35 schedules (14 past with 50-80% seats sold and COMPLETED; 21 future with some booked/paid seats)
   - 3 demo staff created via `pkg_staff.create_staff`:
     - `operator01` / `Operator#01` (`ROLE_OPERATOR`)
     - `seller01` / `Seller#001` (`ROLE_TICKET_SELLER`)
     - `seller02` / `Seller#002` (`ROLE_TICKET_SELLER`)
   - Ends with `EXIT`.

3. **`database/install_all.bat`**:
   Interactive batch script (Plan 1 lines 694-725) running:
   - Sets `NLS_LANG=AMERICAN_AMERICA.AL32UTF8`
   - Runs `00_uninstall.sql` and `01_sys_setup.sql` as sysdba
   - Sequentially runs `02_tables`, `03_triggers`, `04_packages`, `05_views`, `06_backup`, `07_grants`, `08_seed_data` as transport/"Transport#2026"@%DB%.

4. **`database/backup.bat` and `database/restore.bat`**:
   - `backup.bat`: uses `expdp transport/"Transport#2026"@localhost:1521/XEPDB1` schemas=TRANSPORT directory=BACKUP_DIR dumpfile=transport_%TS%.dmp logfile=export_%TS%.log. Includes `set NLS_LANG=AMERICAN_AMERICA.AL32UTF8`.
   - `restore.bat`: uses `impdp` with table_exists_action=REPLACE. Includes `set NLS_LANG=AMERICAN_AMERICA.AL32UTF8`.

5. **`database/09_test_scenarios.sql`**:
   Append manual permission check comments before final `EXIT`.

6. **Full Validation**:
   - Run clean install using `install_all.bat` (or via piped input / powershell helper).
   - Check seed counts: routes=5, vehicles=6, drivers=6, passengers=30, schedules=35, staff=3.
   - Check route name diacritics: `Hà Nội - Lào Cai`.
   - Check revenue trips count: 17.
   - Run `09_test_scenarios.sql`: All tests PASS.
   - Run permission checks as `seller01` and `operator01` (confirm seller cannot insert into tickets, cannot call pkg_staff, cannot select drivers; operator has ROLE_OPERATOR).
   - Test `backup.bat`: verify `.dmp` and `.log` are generated in `D:\OracleBackup`.

7. **Commit**:
   `git add database/01_sys_setup.sql database/08_seed_data.sql database/install_all.bat database/backup.bat database/restore.bat database/09_test_scenarios.sql`
   `git commit -m "feat(db): add seed data, install and Data Pump scripts"`

8. Report to `d:\DuAn\OracleDB_QuanLyVanTai\.superpowers\sdd\task-6-report.md`.
