@echo off
set NLS_LANG=AMERICAN_AMERICA.AL32UTF8
for /f %%i in ('powershell -NoProfile -Command "Get-Date -Format yyyyMMdd_HHmm"') do set TS=%%i
expdp transport/"Transport#2026"@localhost:1521/XEPDB1 schemas=TRANSPORT directory=BACKUP_DIR dumpfile=transport_%TS%.dmp logfile=export_%TS%.log
