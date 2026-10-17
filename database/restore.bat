@echo off
set NLS_LANG=AMERICAN_AMERICA.AL32UTF8
if "%~1"=="" (echo Usage: restore.bat transport_YYYYMMDD_HHMM.dmp & exit /b 1)
impdp transport/"Transport#2026"@localhost:1521/XEPDB1 schemas=TRANSPORT directory=BACKUP_DIR dumpfile=%1 table_exists_action=REPLACE
