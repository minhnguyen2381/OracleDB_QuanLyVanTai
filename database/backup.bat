@echo off
:: Xuất schema TRANSPORT bằng Data Pump; hỏi mật khẩu TRANSPORT (không ghi trong file)
setlocal
set NLS_LANG=AMERICAN_AMERICA.AL32UTF8
set /p TR_PWD=Enter TRANSPORT password: 
for /f %%i in ('powershell -NoProfile -Command "Get-Date -Format yyyyMMdd_HHmm"') do set TS=%%i
expdp transport/"%TR_PWD%"@localhost:1521/XEPDB1 schemas=TRANSPORT directory=BACKUP_DIR dumpfile=transport_%TS%.dmp logfile=export_%TS%.log
