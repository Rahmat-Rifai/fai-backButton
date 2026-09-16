@echo off
REM GBF Right Click Back - Hapus autostart

setlocal
set APP_NAME=GBFRightClickBack

reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v "%APP_NAME%" /f 2>nul
if %ERRORLEVEL% equ 0 (
  echo [OK] Autostart dihapus.
) else (
  echo [INFO] Autostart tidak ditemukan atau sudah dihapus.
)
pause
