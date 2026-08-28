# Build script: compiles Angular + .NET, then packages into a Windows installer via electron-builder.
param(
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"
$Root = $PSScriptRoot

# ---------- 1. Build Angular ----------
Write-Host "Building Angular frontend..." -ForegroundColor Cyan
Push-Location "$Root\frontend"
npm install
npx ng build --configuration production --base-href ./
Pop-Location

# ---------- 2. Publish .NET API ----------
Write-Host "Publishing .NET API ($Runtime)..." -ForegroundColor Cyan
Push-Location "$Root\backend\PocketPirate\PocketPirate"
dotnet publish -r $Runtime --self-contained -c Release -o "$Root\electron-shell\resources\api"
Pop-Location

# ---------- 3. Copy Angular dist into electron resources ----------
Write-Host "Copying Angular build to electron resources..." -ForegroundColor Cyan
$angularDist = "$Root\frontend\dist\frontend"
$electronApp  = "$Root\electron-shell\resources\app"
if (Test-Path $electronApp) { Remove-Item $electronApp -Recurse -Force }
Copy-Item $angularDist $electronApp -Recurse

# ---------- 4. Ensure tools are present ----------
$toolsSrc  = "$Root\backend\PocketPirate\PocketPirate\bin\Debug\net10.0\tools"
$toolsDest = "$Root\electron-shell\resources\api\tools"
if (Test-Path $toolsSrc) {
    Write-Host "Copying yt-dlp / ffmpeg tools..." -ForegroundColor Cyan
    New-Item -ItemType Directory -Force -Path $toolsDest | Out-Null
    Copy-Item "$toolsSrc\*" $toolsDest -Force
} else {
    Write-Warning "tools/ not found at $toolsSrc - place yt-dlp.exe and ffmpeg.exe in $toolsDest manually."
}

# ---------- 5. Install electron-builder deps & package ----------
Write-Host "Installing Electron dependencies..." -ForegroundColor Cyan
Push-Location "$Root\electron-shell"
npm install
Write-Host "Packaging app (installer)..." -ForegroundColor Cyan
$env:CSC_IDENTITY_AUTO_DISCOVERY = "false"
npx electron-builder
Pop-Location

Write-Host ""
Write-Host "Done! Installer is in dist-electron\ - run 'Pocket Pirate Setup *.exe' to install." -ForegroundColor Green
