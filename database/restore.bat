@echo off
:: Nhập lại schema TRANSPORT từ file dump; hỏi mật khẩu TRANSPORT (không ghi trong file)
setlocal
set NLS_LANG=AMERICAN_AMERICA.AL32UTF8
if "%~1"=="" (echo Usage: restore.bat transport_YYYYMMDD_HHMM.dmp & exit /b 1)
set /p TR_PWD=Enter TRANSPORT password: 
impdp transport/"%TR_PWD%"@localhost:1521/XEPDB1 schemas=TRANSPORT directory=BACKUP_DIR dumpfile=%1 table_exists_action=REPLACE
