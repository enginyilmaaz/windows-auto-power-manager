<#
.SYNOPSIS
    Windows Shutdown Helper - Local Build Script
.DESCRIPTION
    Builds the project locally using the bundled NuGet and vswhere tools
    from the tools/ folder. No internet connection or extra setup needed.
.PARAMETER Configuration
    Build configuration: Debug or Release (default: Release)
.PARAMETER SkipRestore
    Skip NuGet package restore
.PARAMETER Clean
    Clean build output before building
.EXAMPLE
    .\create-build.ps1
    .\create-build.ps1 -Configuration Debug
    .\create-build.ps1 -Clean
#>

param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [switch]$SkipRestore,
    [switch]$Clean
)

$ErrorActionPreference = "Stop"

# --- Paths ---
$toolsDir     = $PSScriptRoot
$projectRoot  = Split-Path $toolsDir -Parent
$solutionFile = Join-Path $projectRoot "Windows Shutdown Helper.sln"
$nugetExe     = Join-Path $toolsDir "nuget.exe"
$vswhereExe   = Join-Path $toolsDir "vswhere.exe"

# --- Helper Functions ---
function Write-Step {
    param([string]$Message)
    Write-Host ""
    Write-Host "=== $Message ===" -ForegroundColor Cyan
}

function Find-MSBuild {
    Write-Step "Locating MSBuild"

    # 1) Try vswhere first
    if (Test-Path $vswhereExe) {
        $installPath = & $vswhereExe -latest -requires Microsoft.Component.MSBuild -property installationPath 2>$null
        if ($installPath) {
            $msbuild = Join-Path $installPath "MSBuild\Current\Bin\MSBuild.exe"
            if (Test-Path $msbuild) {
                Write-Host "  [OK] Found via vswhere: $msbuild" -ForegroundColor Green
                return $msbuild
            }
        }
    }

    # 2) Try Build Tools installation
    $buildToolsPaths = @(
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
    )

    foreach ($path in $buildToolsPaths) {
        if (Test-Path $path) {
            Write-Host "  [OK] Found at known path: $path" -ForegroundColor Green
            return $path
        }
    }

    # 3) Try PATH
    $inPath = Get-Command msbuild -ErrorAction SilentlyContinue
    if ($inPath) {
        Write-Host "  [OK] Found in PATH: $($inPath.Source)" -ForegroundColor Green
        return $inPath.Source
    }

    return $null
}

# ============================================================
#  MAIN
# ============================================================

Write-Host ""
Write-Host "============================================" -ForegroundColor White
Write-Host "  Windows Shutdown Helper - Build Script" -ForegroundColor White
Write-Host "============================================" -ForegroundColor White
Write-Host "  Configuration : $Configuration"
Write-Host "  Project Root  : $projectRoot"

# --- Step 1: Verify Tools ---
Write-Step "Verifying Build Tools"
if (-not (Test-Path $nugetExe)) {
    Write-Host "  [ERROR] nuget.exe not found at: $nugetExe" -ForegroundColor Red
    exit 1
}
Write-Host "  [OK] nuget.exe" -ForegroundColor Green

if (-not (Test-Path $vswhereExe)) {
    Write-Host "  [WARN] vswhere.exe not found, will search known paths." -ForegroundColor Yellow
} else {
    Write-Host "  [OK] vswhere.exe" -ForegroundColor Green
}

# --- Step 2: Find MSBuild ---
$msbuildPath = Find-MSBuild

if (-not $msbuildPath) {
    Write-Host ""
    Write-Host "  [ERROR] MSBuild not found!" -ForegroundColor Red
    Write-Host ""
    Write-Host "  MSBuild is required to build .NET Framework 4.8 projects." -ForegroundColor Yellow
    Write-Host "  Install one of the following:" -ForegroundColor Yellow
    Write-Host "    - Visual Studio 2019/2022 (any edition)" -ForegroundColor Yellow
    Write-Host "    - Visual Studio Build Tools:" -ForegroundColor Yellow
    Write-Host "      https://visualstudio.microsoft.com/visual-cpp-build-tools/" -ForegroundColor Yellow
    Write-Host "      (Select '.NET desktop build tools' workload)" -ForegroundColor Yellow
    Write-Host ""
    exit 1
}

# --- Step 3: Clean (optional) ---
if ($Clean) {
    Write-Step "Cleaning Build Output"
    $binDir = Join-Path $projectRoot "bin"
    $objDir = Join-Path $projectRoot "obj"
    if (Test-Path $binDir) { Remove-Item $binDir -Recurse -Force; Write-Host "  Removed bin/" }
    if (Test-Path $objDir) { Remove-Item $objDir -Recurse -Force; Write-Host "  Removed obj/" }
    Write-Host "  [OK] Clean complete." -ForegroundColor Green
}

# --- Step 4: Restore NuGet Packages ---
if (-not $SkipRestore) {
    Write-Step "Restoring NuGet Packages"
    & $nugetExe restore "$solutionFile"
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  [FAIL] NuGet restore failed." -ForegroundColor Red
        exit 1
    }
    Write-Host "  [OK] Packages restored." -ForegroundColor Green
}

# --- Step 5: Inject Build Info ---
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

# --- Step 6: Build ---
Write-Step "Building Solution ($Configuration)"
& $msbuildPath "$solutionFile" /p:Configuration=$Configuration /p:Platform="Any CPU" /p:SignManifests=false /m
if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "  [FAIL] Build failed!" -ForegroundColor Red
    exit 1
}

# --- Done ---
Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Host "  BUILD SUCCEEDED" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host "  Output: bin\$Configuration\" -ForegroundColor White
Write-Host ""
