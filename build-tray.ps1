Write-Host "Building ShotPath..." -ForegroundColor Yellow

$sourceFile = Get-Content -Path "shotpath.cs" -Raw

# Kill any running instances
$process = Get-Process shotpath -ErrorAction SilentlyContinue
if ($process) {
    Write-Host "Stopping running instance..." -ForegroundColor Yellow
    Stop-Process -Name shotpath -Force
    Start-Sleep -Seconds 1
}

# Delete old executable if exists
if (Test-Path "shotpath.exe") {
    Remove-Item "shotpath.exe" -Force
}

# Convert PNG to ICO if needed
if (!(Test-Path "icon.ico") -and (Test-Path "app.png")) {
    Write-Host "Converting app.png to icon.ico..." -ForegroundColor Yellow
    & powershell -File convert-icon.ps1
}

# Use C# compiler directly for icon embedding
$cscPath = ""
$dotnetPath = (Get-Command dotnet -ErrorAction SilentlyContinue).Source
if ($dotnetPath) {
    $sdkPath = Split-Path (Split-Path $dotnetPath)
    $cscSearch = Get-ChildItem -Path $sdkPath -Recurse -Filter "csc.exe" -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($cscSearch) {
        $cscPath = $cscSearch.FullName
    }
}

if (!$cscPath) {
    # Try .NET Framework paths
    $frameworkPaths = @(
        "C:\Program Files\Microsoft Visual Studio\2022\*\MSBuild\Current\Bin\Roslyn\csc.exe",
        "C:\Program Files (x86)\Microsoft Visual Studio\2019\*\MSBuild\Current\Bin\Roslyn\csc.exe",
        "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
        "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
    )
    
    foreach ($path in $frameworkPaths) {
        $found = Get-Item $path -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($found) {
            $cscPath = $found.FullName
            break
        }
    }
}

if ($cscPath -and (Test-Path "icon.ico")) {
    Write-Host "Using C# compiler with icon..." -ForegroundColor Yellow
    & $cscPath /target:winexe /win32icon:icon.ico /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /out:shotpath.exe shotpath.cs
} else {
    Write-Host "Using Add-Type (no icon embedding)..." -ForegroundColor Yellow
    Add-Type -TypeDefinition $sourceFile -ReferencedAssemblies System.Drawing, System.Windows.Forms -OutputAssembly "shotpath.exe" -OutputType WindowsApplication
}

if (Test-Path "shotpath.exe") {
    Write-Host "Build successful! Executable created: shotpath.exe" -ForegroundColor Green
    Write-Host ""
    Write-Host "Features:" -ForegroundColor Cyan
    Write-Host "- Runs in system tray (background)" -ForegroundColor White
    Write-Host "- Hotkeys:" -ForegroundColor White
    Write-Host "  - PrintScreen: Take screenshot and copy path" -ForegroundColor Gray
    Write-Host "  - Ctrl+PrintScreen: Take screenshot and copy image" -ForegroundColor Gray
    Write-Host "  - Alt+PrintScreen: Take screenshot and upload to Imgur" -ForegroundColor Gray
    Write-Host "- Screenshots saved to temp\shotpath folder" -ForegroundColor White
    Write-Host "- Draw selection box to capture specific area" -ForegroundColor White
    Write-Host "- Right-click tray icon for menu options:" -ForegroundColor White
    Write-Host "  - Copy as Path (PrintScreen)" -ForegroundColor Gray
    Write-Host "  - Copy as Image (Ctrl+PrintScreen)" -ForegroundColor Gray
    Write-Host "  - Copy as Imgur URL (Alt+PrintScreen)" -ForegroundColor Gray
    Write-Host "  - Open Folder: Open screenshots folder" -ForegroundColor Gray
    Write-Host "  - Clear Folder: Delete all screenshots" -ForegroundColor Gray
    Write-Host "  - Run at Startup: Enable/disable auto-start (ON by default)" -ForegroundColor Gray
    Write-Host "  - Exit: Close the application" -ForegroundColor Gray
    Write-Host "- Press ESC to cancel screenshot selection" -ForegroundColor White
    Write-Host "- Uses tray.png for system tray icon if present" -ForegroundColor White
    Write-Host "- Uses app.png for executable icon if present" -ForegroundColor White
} else {
    Write-Host "Build failed!" -ForegroundColor Red
}