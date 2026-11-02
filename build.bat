@echo off
setlocal
cd /d "%~dp0"

echo ============================================
echo  Building FlashPad++.exe
echo ============================================
echo.

set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" (
    echo [ERROR] csc.exe not found.
    echo         .NET Framework 4.x is required. It ships with Windows 10/11.
    echo.
    pause
    exit /b 1
)

echo Compiler: %CSC%
echo.

if not exist "flashpadpp.ico" (
    echo [WARN] flashpadpp.ico missing - building without icon.
    set "ICONOPT="
) else (
    set "ICONOPT=/win32icon:flashpadpp.ico"
)

"%CSC%" /nologo /target:winexe /platform:anycpu /optimize+ /codepage:65001 ^
    /out:"FlashPad++.exe" %ICONOPT% ^
    /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll ^
    "FlashPadPP.cs" > build.log 2>&1

set "RC=%ERRORLEVEL%"

type build.log

if not "%RC%"=="0" (
    echo.
    echo ============================================
    echo [FAILED] compile error, exit code %RC%
    echo Full log saved to: %CD%\build.log
    echo Please send me that file.
    echo ============================================
    pause
    exit /b 1
)

echo.
echo ============================================
echo [OK] FlashPad++.exe built successfully.
echo ============================================
if exist "FlashPad++.exe" (
    for %%A in ("FlashPad++.exe") do echo Size: %%~zA bytes
)
echo.
echo Double-click FlashPad++.exe to run.
echo Icon embedded: flashpadpp.ico
echo Source: FlashPadPP.cs  (edit and re-run this file to rebuild)
echo.
pause
