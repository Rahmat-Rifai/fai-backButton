@echo off
REM GBF Right Click Back - Install autostart (registry HKCU\...\Run)
REM Tidak butuh admin privilege (HKCU, bukan HKLM).

setlocal

set APP_NAME=GBFRightClickBack

REM Path default: exe di sebelah script ini (framework-dependent) atau portable.
REM Prioritas: dist\framework-dependent, lalu dist\portable, lalu exe di folder yang sama.

set EXE_PATH=
if exist "%~dp0dist\framework-dependent\GbfRightClickBack.exe" set EXE_PATH=%~dp0dist\framework-dependent\GbfRightClickBack.exe
if exist "%~dp0dist\portable\GbfRightClickBack.exe" if not defined EXE_PATH set EXE_PATH=%~dp0dist\portable\GbfRightClickBack.exe
if exist "%~dp0GbfRightClickBack.exe" if not defined EXE_PATH set EXE_PATH=%~dp0GbfRightClickBack.exe

REM Jika dipanggil dari folder dist itu sendiri
if exist "%~dp0GbfRightClickBack.exe" set EXE_PATH=%~dp0GbfRightClickBack.exe

if not defined EXE_PATH (
  echo [ERROR] GbfRightClickBack.exe tidak ditemukan.
  echo Letakkan script ini di sebelah GbfRightClickBack.exe atau di root project.
  pause
  exit /b 1
)

echo Menginstall autostart untuk:
echo   %EXE_PATH%
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v "%APP_NAME%" /t REG_SZ /d "\"%EXE_PATH%\"" /f
if %ERRORLEVEL% equ 0 (
  echo [OK] Autostart terpasang. Aplikasi akan jalan otomatis saat login Windows.
) else (
  echo [ERROR] Gagal menulis registry.
)
pause
