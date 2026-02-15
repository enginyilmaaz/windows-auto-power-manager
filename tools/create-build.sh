#!/bin/bash
# =============================================
#  Windows Shutdown Helper - Linux Build Script
#  Uses Mono to build .NET Framework 4.8 project
# =============================================

set -e

# --- Colors ---
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
WHITE='\033[1;37m'
NC='\033[0m'

# --- Paths ---
TOOLS_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(dirname "$TOOLS_DIR")"
SOLUTION_FILE="$PROJECT_ROOT/Windows Shutdown Helper.sln"
NUGET_EXE="$TOOLS_DIR/nuget.exe"

# --- Defaults ---
CONFIGURATION="Release"
SKIP_RESTORE=false
CLEAN=false

# --- Parse Arguments ---
while [[ $# -gt 0 ]]; do
    case "$1" in
        --debug)
            CONFIGURATION="Debug"
            shift
            ;;
        --release)
            CONFIGURATION="Release"
            shift
            ;;
        --skip-restore)
            SKIP_RESTORE=true
            shift
            ;;
        --clean)
            CLEAN=true
            shift
            ;;
        --help|-h)
            echo "Usage: ./create-build.sh [OPTIONS]"
            echo ""
            echo "Options:"
            echo "  --debug          Build in Debug configuration"
            echo "  --release        Build in Release configuration (default)"
            echo "  --skip-restore   Skip NuGet package restore"
            echo "  --clean          Clean build output before building"
            echo "  --help, -h       Show this help message"
            exit 0
            ;;
        *)
            echo -e "${RED}Unknown option: $1${NC}"
            echo "Use --help to see available options."
            exit 1
            ;;
    esac
done

step() {
    echo ""
    echo -e "${CYAN}=== $1 ===${NC}"
}

# ============================================================
#  MAIN
# ============================================================

echo ""
echo -e "${WHITE}============================================${NC}"
echo -e "${WHITE}  Windows Shutdown Helper - Build Script${NC}"
echo -e "${WHITE}============================================${NC}"
echo "  Configuration : $CONFIGURATION"
echo "  Project Root  : $PROJECT_ROOT"
echo "  Platform      : Linux (Mono)"

# --- Step 1: Check Dependencies ---
step "Checking Dependencies"

# Check mono
if command -v mono &>/dev/null; then
    MONO_VER=$(mono --version 2>&1 | head -n1)
    echo -e "  ${GREEN}[OK]${NC} mono - $MONO_VER"
else
    echo -e "  ${RED}[ERROR] mono not found!${NC}"
    echo ""
    echo -e "  ${YELLOW}Install Mono:${NC}"
    echo -e "  ${YELLOW}  Ubuntu/Debian : sudo apt install mono-complete${NC}"
    echo -e "  ${YELLOW}  Fedora        : sudo dnf install mono-complete${NC}"
    echo -e "  ${YELLOW}  Arch          : sudo pacman -S mono${NC}"
    echo -e "  ${YELLOW}  Docs          : https://www.mono-project.com/download/stable/${NC}"
    echo ""
    exit 1
fi

# Check msbuild (comes with mono-complete)
if command -v msbuild &>/dev/null; then
    echo -e "  ${GREEN}[OK]${NC} msbuild"
    MSBUILD_CMD="msbuild"
elif command -v xbuild &>/dev/null; then
    echo -e "  ${YELLOW}[WARN]${NC} msbuild not found, falling back to xbuild (deprecated)"
    MSBUILD_CMD="xbuild"
else
    echo -e "  ${RED}[ERROR] msbuild not found!${NC}"
    echo ""
    echo -e "  ${YELLOW}Make sure you have mono-complete installed (not just mono-runtime).${NC}"
    echo -e "  ${YELLOW}  sudo apt install mono-complete${NC}"
    echo ""
    exit 1
fi

# Check nuget.exe
if [ -f "$NUGET_EXE" ]; then
    echo -e "  ${GREEN}[OK]${NC} nuget.exe"
else
    echo -e "  ${RED}[ERROR] nuget.exe not found at: $NUGET_EXE${NC}"
    exit 1
fi

# --- Step 2: Clean (optional) ---
if [ "$CLEAN" = true ]; then
    step "Cleaning Build Output"
    [ -d "$PROJECT_ROOT/bin" ] && rm -rf "$PROJECT_ROOT/bin" && echo "  Removed bin/"
    [ -d "$PROJECT_ROOT/obj" ] && rm -rf "$PROJECT_ROOT/obj" && echo "  Removed obj/"
    echo -e "  ${GREEN}[OK] Clean complete.${NC}"
fi

# --- Step 3: Restore NuGet Packages ---
if [ "$SKIP_RESTORE" = false ]; then
    step "Restoring NuGet Packages"
    mono "$NUGET_EXE" restore "$SOLUTION_FILE"
    echo -e "  ${GREEN}[OK] Packages restored.${NC}"
fi

# --- Step 4: Inject Build Info ---
step "Injecting Build Info"
BUILD_INFO_FILE="$PROJECT_ROOT/src/BuildInfo.cs"
if [ -f "$BUILD_INFO_FILE" ]; then
    COMMIT_HASH=$(git -C "$PROJECT_ROOT" rev-parse --short=6 HEAD 2>/dev/null || true)
    if [ -n "$COMMIT_HASH" ]; then
        sed -i "s/public const string CommitId = \"dev\"/public const string CommitId = \"$COMMIT_HASH\"/" "$BUILD_INFO_FILE"
        echo -e "  ${GREEN}[OK] CommitId set to: $COMMIT_HASH${NC}"
    else
        echo -e "  ${YELLOW}[SKIP] Git not available, keeping default CommitId.${NC}"
    fi
else
    echo -e "  ${YELLOW}[SKIP] BuildInfo.cs not found.${NC}"
fi

# --- Step 5: Build ---
step "Building Solution ($CONFIGURATION)"
$MSBUILD_CMD "$SOLUTION_FILE" /p:Configuration="$CONFIGURATION" /p:Platform="Any CPU" /p:SignManifests=false

# --- Done ---
echo ""
echo -e "${GREEN}============================================${NC}"
echo -e "${GREEN}  BUILD SUCCEEDED${NC}"
echo -e "${GREEN}============================================${NC}"
echo -e "${WHITE}  Output: bin/$CONFIGURATION/${NC}"
echo ""
