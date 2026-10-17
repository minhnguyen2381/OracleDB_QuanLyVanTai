@echo off
:: Cài lại toàn bộ CSDL vận tải (XÓA dữ liệu cũ). Cần mật khẩu SYS của Oracle XE.
setlocal
cd /d "%~dp0"
set NLS_LANG=AMERICAN_AMERICA.AL32UTF8
set DB=//localhost:1521/XEPDB1

echo This will DROP and recreate schema TRANSPORT, its roles and staff users.
set /p CONFIRM=Continue? (Y/N): 
if /i not "%CONFIRM%"=="Y" exit /b 1
set /p SYS_PWD=Enter SYS password: 

if not exist D:\OracleBackup mkdir D:\OracleBackup

sqlplus -S -L "sys/%SYS_PWD%@%DB% as sysdba" @00_uninstall.sql
sqlplus -S -L "sys/%SYS_PWD%@%DB% as sysdba" @01_sys_setup.sql || goto :fail

for %%f in (02_tables 03_triggers 04_packages 05_views 06_backup 07_grants 08_seed_data) do (
  echo === %%f ===
  sqlplus -S -L transport/"Transport#2026"@%DB% @%%f.sql || goto :fail
)

echo.
echo INSTALL OK. Demo accounts: operator01/Operator#01, seller01/Seller#001, seller02/Seller#002
exit /b 0

:fail
echo.
echo INSTALL FAILED - see the error above.
exit /b 1
