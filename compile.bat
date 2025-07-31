@echo off
echo Compiling ScreenshotApp...
powershell -ExecutionPolicy Bypass -File build-tray.ps1
if %ERRORLEVEL% EQU 0 (
    echo Compilation successful!
    echo Executable created: ScreenshotApp.exe
) else (
    echo Compilation failed!
)
pause