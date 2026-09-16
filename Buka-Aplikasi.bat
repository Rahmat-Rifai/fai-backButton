@echo off
title Back Button Customizer
cd /d "%~dp0"

if exist "dist\portable\GbfRightClickBack.exe" (
    start "" "%~dp0dist\portable\GbfRightClickBack.exe"
    exit
)

if exist "dist\framework-dependent\GbfRightClickBack.exe" (
    start "" "%~dp0dist\framework-dependent\GbfRightClickBack.exe"
    exit
)

echo [ERROR] File executable tidak ditemukan di folder dist.
echo Silakan jalankan build.bat terlebih dahulu.
pause
