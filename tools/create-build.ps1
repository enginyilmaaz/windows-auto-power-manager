<#
.SYNOPSIS
    Windows Auto Power Manager - Local Build Script
.DESCRIPTION
    Builds the project using the local .NET 8 SDK from tools/dotnet/.
.PARAMETER Configuration
    Build configuration: Debug or Release (default: Release)
.PARAMETER SkipRestore
    Skip NuGet package restore
.PARAMETER Clean
    Clean build output before building
.PARAMETER InstallSdk
    Download .NET 8 SDK into tools/dotnet/
.EXAMPLE
    .\create-build.ps1
    .\create-build.ps1 -Configuration Debug
    .\create-build.ps1 -Clean
    .\create-build.ps1 -InstallSdk
#>

param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [switch]$SkipRestore,
    [switch]$Clean,
    [switch]$InstallSdk
)

$ErrorActionPreference = "Stop"

# --- Paths ---
$toolsDir     = $PSScriptRoot
$projectRoot  = Split-Path $toolsDir -Parent
$solutionFile = Join-Path $projectRoot "Windows Auto Power Manager.sln"
$projectFile  = Join-Path $projectRoot "Windows Auto Power Manager.csproj"
$localDotnet  = Join-Path $toolsDir "dotnet"

if ($IsWindows -or $env:OS -eq "Windows_NT") {
    $localDotnetExe = Join-Path $localDotnet "dotnet.exe"
} else {
    $localDotnetExe = Join-Path $localDotnet "dotnet"
}

# --- Helper ---
function Write-Step {
    param([string]$Message)
    Write-Host ""
    Write-Host "=== $Message ===" -ForegroundColor Cyan
}

# ============================================================
#  MAIN
# ============================================================

Write-Host ""
Write-Host "============================================" -ForegroundColor White
Write-Host "  Windows Auto Power Manager - Build Script" -ForegroundColor White
Write-Host "============================================" -ForegroundColor White
Write-Host "  Configuration : $Configuration"
Write-Host "  Project Root  : $projectRoot"

# --- Step 0: Install SDK if requested ---
if ($InstallSdk) {
    Write-Step "Installing .NET 8 SDK into tools/dotnet/"
    if ($IsWindows -or $env:OS -eq "Windows_NT") {
        $installScript = Join-Path $env:TEMP "dotnet-install.ps1"
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        Invoke-WebRequest -Uri "https://dot.net/v1/dotnet-install.ps1" -OutFile $installScript -UseBasicParsing
        & $installScript -Channel 8.0 -InstallDir $localDotnet
        Remove-Item $installScript -ErrorAction SilentlyContinue
    } else {
        $installScript = "/tmp/dotnet-install.sh"
        Invoke-WebRequest -Uri "https://dot.net/v1/dotnet-install.sh" -OutFile $installScript -UseBasicParsing
        chmod +x $installScript
        & bash $installScript --channel 8.0 --install-dir $localDotnet
        Remove-Item $installScript -ErrorAction SilentlyContinue
    }
    Write-Host "  [OK] .NET 8 SDK installed." -ForegroundColor Green
}

# --- Step 1: Check local dotnet SDK ---
Write-Step "Checking .NET SDK"
$dotnetCmd = $localDotnetExe
$env:DOTNET_ROOT = $localDotnet

if (-not (Test-Path $dotnetCmd)) {
    Write-Host "  [ERROR] Local .NET SDK not found at: $localDotnet" -ForegroundColor Red
    Write-Host ""
    Write-Host "  Run the following command to install it:" -ForegroundColor Yellow
    Write-Host "    .\create-build.ps1 -InstallSdk" -ForegroundColor Yellow
    Write-Host ""
    exit 1
}

$ver = & $dotnetCmd --version
Write-Host "  [OK] Local SDK: $ver ($localDotnet)" -ForegroundColor Green

# --- Step 2: Clean (optional) ---
if ($Clean) {
    Write-Step "Cleaning Build Output"
    & $dotnetCmd clean "$solutionFile" -c $Configuration --nologo -v q
    Write-Host "  [OK] Clean complete." -ForegroundColor Green
}

# --- Step 3: Restore ---
if (-not $SkipRestore) {
    Write-Step "Restoring NuGet Packages"
    & $dotnetCmd restore "$solutionFile"
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  [FAIL] Restore failed." -ForegroundColor Red
        exit 1
    }
    Write-Host "  [OK] Packages restored." -ForegroundColor Green
}

# --- Step 4: Inject Build Info ---
Write-Step "Injecting Build Info"
$buildInfoFile = Join-Path $projectRoot "src/BuildInfo.cs"
if (Test-Path $buildInfoFile) {
    $commitHash = git -C $projectRoot rev-parse --short=6 HEAD 2>$null
    if ($commitHash) {
        $content = Get-Content $buildInfoFile -Raw
        $updated = $content -replace 'public const string CommitId = "dev"', "public const string CommitId = ""$commitHash"""
        Set-Content $buildInfoFile $updated -NoNewline
        Write-Host "  [OK] CommitId set to: $commitHash" -ForegroundColor Green
    } else {
        Write-Host "  [SKIP] Git not available, keeping default CommitId." -ForegroundColor Yellow
    }
} else {
    Write-Host "  [SKIP] BuildInfo.cs not found." -ForegroundColor Yellow
}

# --- Step 5: Build ---
Write-Step "Building Solution ($Configuration)"
& $dotnetCmd build "$solutionFile" -c $Configuration --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "  [FAIL] Build failed!" -ForegroundColor Red
    exit 1
}

# --- Step 6: Publish (Installer Output) ---
Write-Step "Publishing win-x64 (ReadyToRun)"
& $dotnetCmd publish "$projectFile" -c $Configuration -r win-x64 --self-contained false --no-restore -p:PublishReadyToRun=true -p:PublishSingleFile=false
if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "  [FAIL] Publish (win-x64) failed!" -ForegroundColor Red
    exit 1
}

# --- Step 7: Publish x86 (Win10 legacy compatibility) ---
Write-Step "Publishing win-x86 (ReadyToRun)"
& $dotnetCmd publish "$projectFile" -c $Configuration -r win-x86 --self-contained false --no-restore -p:PublishReadyToRun=true -p:PublishSingleFile=false
if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "  [FAIL] Publish (win-x86) failed!" -ForegroundColor Red
    exit 1
}

# --- Done ---
Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Host "  BUILD SUCCEEDED" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host "  Build Output  : bin/$Configuration/net8.0-windows/" -ForegroundColor White
Write-Host "  Publish Output: bin/$Configuration/net8.0-windows/win-x64/publish/" -ForegroundColor White
Write-Host "  Publish Output: bin/$Configuration/net8.0-windows/win-x86/publish/" -ForegroundColor White
Write-Host ""
