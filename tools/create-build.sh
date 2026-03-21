#!/bin/bash
# =============================================
#  Windows Auto Power Manager - Build Script
#  Uses local .NET 8 SDK from tools/dotnet/
# =============================================

set -euo pipefail

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
SOLUTION_FILE="$PROJECT_ROOT/Windows Auto Power Manager.sln"
PROJECT_FILE="$PROJECT_ROOT/Windows Auto Power Manager.csproj"
LOCAL_DOTNET="$TOOLS_DIR/dotnet/dotnet"

# --- Defaults ---
CONFIGURATION="Release"
SKIP_RESTORE=false
CLEAN=false
INSTALL_SDK=false

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
        --install-sdk)
            INSTALL_SDK=true
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
            echo "  --install-sdk    Download .NET 8 SDK into tools/dotnet/"
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
echo -e "${WHITE}  Windows Auto Power Manager - Build Script${NC}"
echo -e "${WHITE}============================================${NC}"
echo "  Configuration : $CONFIGURATION"
echo "  Project Root  : $PROJECT_ROOT"

# --- Step 0: Install SDK if requested ---
if [ "$INSTALL_SDK" = true ]; then
    step "Installing .NET 8 SDK into tools/dotnet/"
    curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
    chmod +x /tmp/dotnet-install.sh
    /tmp/dotnet-install.sh --channel 8.0 --install-dir "$TOOLS_DIR/dotnet"
    rm -f /tmp/dotnet-install.sh
    echo -e "  ${GREEN}[OK] .NET 8 SDK installed.${NC}"
fi

# --- Step 1: Check local dotnet SDK ---
step "Checking .NET SDK"
DOTNET="$LOCAL_DOTNET"
export DOTNET_ROOT="$TOOLS_DIR/dotnet"

if [ ! -x "$DOTNET" ]; then
    echo -e "  ${RED}[ERROR] Local .NET SDK not found at: $TOOLS_DIR/dotnet/${NC}"
    echo ""
    echo -e "  ${YELLOW}Run the following command to install it:${NC}"
    echo -e "  ${YELLOW}  ./create-build.sh --install-sdk${NC}"
    echo ""
    exit 1
fi

DOTNET_VER=$("$DOTNET" --version 2>&1)
echo -e "  ${GREEN}[OK]${NC} Local SDK: $DOTNET_VER ($TOOLS_DIR/dotnet/)"

# --- Step 2: Clean (optional) ---
if [ "$CLEAN" = true ]; then
    step "Cleaning Build Output"
    "$DOTNET" clean "$SOLUTION_FILE" -c "$CONFIGURATION" --nologo -v q
    echo -e "  ${GREEN}[OK] Clean complete.${NC}"
fi

# --- Step 3: Restore ---
if [ "$SKIP_RESTORE" = false ]; then
    step "Restoring NuGet Packages"
    "$DOTNET" restore "$SOLUTION_FILE"
    echo -e "  ${GREEN}[OK] Packages restored.${NC}"
fi

# --- Step 4: Build metadata (without mutating tracked files) ---
step "Preparing Build Metadata"
BUILD_EPOCH="$(date +%s)"
BUILD_NUMBER="$(git -C "$PROJECT_ROOT" rev-list --count HEAD 2>/dev/null || echo "")"
[[ -z "$BUILD_NUMBER" ]] && BUILD_NUMBER="$((BUILD_EPOCH % 100000))"
DISPLAY_VERSION="1.0.$BUILD_NUMBER"
ASSEMBLY_VERSION="1.0.$((BUILD_EPOCH / 65536)).$((BUILD_EPOCH % 65536))"
COMMIT_HASH="$(git -C "$PROJECT_ROOT" rev-parse --short=6 HEAD 2>/dev/null || echo "")"
[[ -z "$COMMIT_HASH" ]] && COMMIT_HASH="local"
INFO_VERSION="$DISPLAY_VERSION+$COMMIT_HASH"
echo -e "  ${GREEN}[OK]${NC} Display version : $DISPLAY_VERSION"
echo -e "  ${GREEN}[OK]${NC} Assembly version: $ASSEMBLY_VERSION"
echo -e "  ${GREEN}[OK]${NC} Commit hash     : $COMMIT_HASH"
echo -e "  ${GREEN}[OK]${NC} Info version    : $INFO_VERSION"

# --- Step 5: Build ---
step "Building Solution ($CONFIGURATION)"
"$DOTNET" build "$SOLUTION_FILE" -c "$CONFIGURATION" --no-restore \
    -p:AssemblyVersion="$ASSEMBLY_VERSION" \
    -p:FileVersion="$ASSEMBLY_VERSION" \
    -p:Version="$DISPLAY_VERSION" \
    -p:InformationalVersion="$INFO_VERSION" \
    -p:IncludeSourceRevisionInInformationalVersion=false

# --- Step 6: Publish (Installer Output) ---
step "Publishing win-x64 (ReadyToRun)"
"$DOTNET" publish "$PROJECT_FILE" -c "$CONFIGURATION" -r win-x64 --self-contained false --no-restore \
    -p:AssemblyVersion="$ASSEMBLY_VERSION" \
    -p:FileVersion="$ASSEMBLY_VERSION" \
    -p:Version="$DISPLAY_VERSION" \
    -p:InformationalVersion="$INFO_VERSION" \
    -p:IncludeSourceRevisionInInformationalVersion=false \
    -p:PublishReadyToRun=true \
    -p:PublishSingleFile=false

# --- Step 7: Publish x86 (Win10 legacy compatibility) ---
step "Publishing win-x86 (ReadyToRun)"
"$DOTNET" publish "$PROJECT_FILE" -c "$CONFIGURATION" -r win-x86 --self-contained false --no-restore \
    -p:AssemblyVersion="$ASSEMBLY_VERSION" \
    -p:FileVersion="$ASSEMBLY_VERSION" \
    -p:Version="$DISPLAY_VERSION" \
    -p:InformationalVersion="$INFO_VERSION" \
    -p:IncludeSourceRevisionInInformationalVersion=false \
    -p:PublishReadyToRun=true \
    -p:PublishSingleFile=false

# --- Done ---
echo ""
echo -e "${GREEN}============================================${NC}"
echo -e "${GREEN}  BUILD SUCCEEDED${NC}"
echo -e "${GREEN}============================================${NC}"
echo -e "${WHITE}  Display Version: $DISPLAY_VERSION${NC}"
echo -e "${WHITE}  Commit Hash    : $COMMIT_HASH${NC}"
echo -e "${WHITE}  Build Output   : bin/$CONFIGURATION/net8.0-windows10.0.19041.0/${NC}"
echo -e "${WHITE}  Publish Output : bin/$CONFIGURATION/net8.0-windows10.0.19041.0/win-x64/publish/${NC}"
echo -e "${WHITE}  Publish Output : bin/$CONFIGURATION/net8.0-windows10.0.19041.0/win-x86/publish/${NC}"
echo ""
