@echo off
REM GBF Right Click Back - Build script
REM Membuild single-file .exe ke dist\

setlocal

where dotnet >nul 2>nul
if %ERRORLEVEL% neq 0 (
  echo [ERROR] dotnet SDK tidak ditemukan.
  echo Install .NET 8 SDK dari https://dotnet.microsoft.com/download
  pause
  exit /b 1
)

set CONFIG=Release
set RID=win-x64

echo === Build (framework-dependent, ~180KB, butuh .NET Runtime) ===
dotnet publish src\GbfRightClickBack\GbfRightClickBack.csproj -c %CONFIG% -r %RID% --self-contained false -p:PublishSingleFile=true -o dist\framework-dependent
if %ERRORLEVEL% neq 0 exit /b 1

echo.
echo === Build (portable/self-contained, ~160MB, tanpa dependency) ===
dotnet publish src\GbfRightClickBack\GbfRightClickBack.csproj -c %CONFIG% -r %RID% --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o dist\portable
if %ERRORLEVEL% neq 0 exit /b 1

echo.
echo === Selesai ===
echo   dist\framework-dependent\GbfRightClickBack.exe
echo   dist\portable\GbfRightClickBack.exe
dir dist\framework-dependent\GbfRightClickBack.exe dist\portable\GbfRightClickBack.exe 2>nul | findstr GbfRightClickBack
pause
