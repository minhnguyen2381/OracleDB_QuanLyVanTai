# Task 5 Brief: Staff Management Package and Role Grants (TDD)

**Plan Source:** `docs/superpowers/plans/2026-10-17-plan-1-database.md` Task 5 (lines 447-522)
**Spec Source:** `docs/superpowers/specs/2026-10-17-transport-management-design.md` §5.3, §7.2, §7.3, §9

## Goal
Implement:
- `database/04_packages.sql`: append `pkg_staff` (package spec and body from spec §5.3) right after `pkg_schedule` body's `/` and before the compile-check block.
- `database/07_grants.sql`: object grants for `ROLE_OPERATOR` and `ROLE_TICKET_SELLER` according to spec §7.3.
- `database/09_test_scenarios.sql`: replace `-- @@STAFF_TESTS@@` with tests 10 and 11, plus add cleanup lines in `cleanup` procedure.

## Strict TDD Requirement
1. **RED**:
   - In `database/09_test_scenarios.sql`:
     Add these two lines at the START of `cleanup` procedure:
     ```sql
         DELETE FROM staff WHERE username = 'TEST_STAFF_01';
         BEGIN EXECUTE IMMEDIATE 'DROP USER test_staff_01'; EXCEPTION WHEN OTHERS THEN NULL; END;
     ```
     Replace `-- @@STAFF_TESTS@@` with tests 10 & 11 (scenarios 10a-10d, 11a-11d):
     ```sql
       -- 10. PKG_STAFF chặn dữ liệu sai
       expect_error('10a. invalid username is rejected', -20040,
         'BEGIN pkg_staff.create_staff(''x; drop'', ''Password1'', ''X'', NULL, ''ROLE_OPERATOR''); END;');
       expect_error('10b. invalid role is rejected', -20041,
         'BEGIN pkg_staff.create_staff(''test_staff_01'', ''Password1'', ''X'', NULL, ''ROLE_ADMIN''); END;');
       expect_error('10c. short password is rejected', -20044,
         'BEGIN pkg_staff.create_staff(''test_staff_01'', ''short'', ''X'', NULL, ''ROLE_OPERATOR''); END;');
       expect_error('10d. unknown staff is rejected', -20042,
         'BEGIN pkg_staff.set_locked(''nobody_xyz'', 1); END;');

       -- 11. Vòng đời nhân viên: tạo, đổi role, khóa
       pkg_staff.create_staff('test_staff_01', 'TestPass#1', 'Test Staff', NULL, 'ROLE_TICKET_SELLER');
       SELECT COUNT(*) INTO v_n FROM all_users WHERE username = 'TEST_STAFF_01';
       ok('11a. Oracle user created', v_n = 1);
       pkg_staff.change_role('test_staff_01', 'ROLE_OPERATOR');
       SELECT COUNT(*) INTO v_n FROM staff WHERE username = 'TEST_STAFF_01' AND role_name = 'ROLE_OPERATOR';
       ok('11b. role changed', v_n = 1);
       pkg_staff.set_locked('test_staff_01', 1);
       SELECT COUNT(*) INTO v_n FROM staff WHERE username = 'TEST_STAFF_01' AND status = 'LOCKED';
       ok('11c. account locked', v_n = 1);
       expect_error('11d. duplicate username is rejected', -1920,
         'BEGIN pkg_staff.create_staff(''test_staff_01'', ''TestPass#1'', ''X'', NULL, ''ROLE_OPERATOR''); END;');
       -- Dọn user test
       DELETE FROM staff WHERE username = 'TEST_STAFF_01';
       COMMIT;
       EXECUTE IMMEDIATE 'DROP USER test_staff_01';
     ```
   - Run `database/09_test_scenarios.sql` against schema TRANSPORT and verify it fails with:
     `PLS-00201: identifier 'PKG_STAFF.CREATE_STAFF' must be declared`. Capture this failure output for TDD Evidence.

2. **GREEN**:
   - Add `pkg_staff` spec and body (spec §5.3) into `database/04_packages.sql`.
   - Create `database/07_grants.sql` with standard header + spec §7.3 grants + EXIT.
   - Run `04_packages.sql` on schema TRANSPORT. Verify 0 compile errors.
   - Run `07_grants.sql` on schema TRANSPORT.
   - Run `09_test_scenarios.sql` on schema TRANSPORT. Verify all 27 assertions PASS, ending with `ALL TESTS PASSED`.

3. Self-review and commit:
   `git add database/04_packages.sql database/07_grants.sql database/09_test_scenarios.sql`
   `git commit -m "feat(db): add staff management package and role grants"`

4. Report to `d:\DuAn\OracleDB_QuanLyVanTai\.superpowers\sdd\task-5-report.md`.
