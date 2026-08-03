# Windows Auto Power Manager

A Windows desktop application that schedules system actions like shutdown, restart, sleep, lock, log off, and monitor off based on configurable triggers.

Built with **.NET 8 (WinForms)** and **WebView2** for a modern HTML/CSS/JS-based UI.

[![Build and Release](https://github.com/enginyilmaaz/windows-auto-power-manager/actions/workflows/build-and-release.yml/badge.svg)](https://github.com/enginyilmaaz/windows-auto-power-manager/actions/workflows/build-and-release.yml)

## Download

Get the latest installer from the [Releases](https://github.com/enginyilmaaz/windows-auto-power-manager/releases) page.

## Features

### System Actions
- **Shutdown** - Shuts down the computer
- **Restart** - Restarts the computer
- **Sleep** - Puts the computer to sleep
- **Lock** - Locks the workstation
- **Log Off** - Logs off the current Windows session
- **Turn Off Monitor** - Turns off the display

### Trigger Types
- **System Idle** - Triggers after the system has been idle for a specified duration
- **Countdown (From Now)** - Triggers after a specified amount of time from creation
- **Every Day by Hour (Certain Time)** - Triggers daily at a specific time

### Updates
- Checks GitHub Releases for a newer version; the repository is public, so no sign-in or token is involved
- Runs once per launch and then on a configurable interval (hourly / daily / weekly), or on demand from the About page
- Offers the update in a dialog showing the version change and download progress, which can be sent to a background indicator
- Installs silently and relaunches the app when accepted

### General
- Up to 5 concurrent actions
- Countdown notifier popup before action execution with options to ignore, delete, or skip
- Pause/resume all actions with preset durations (30m, 1h, 2h, 4h, end of day) or custom duration
- Action conflict validation (prevents duplicate or conflicting actions)
- Search and filter actions by type
- System tray integration with context menu
- Built-in **Help** page with trigger usage guidance (available from hamburger menu, tray menu, and action-list right-click menu)
- Start with Windows (startup shortcut, re-asserted on every launch so a stale entry repairs itself)
- Run in background when window is closed
- Action logging with log viewer (filtering, sorting; the viewer shows 250 entries, 1000 are retained)
- Optional debug trace for diagnosing stalls and power events, capped at 5 MB and rolled over
- Dark/Light/System theme support
- Multi-language support: English, Turkish, German, French, Russian, Italian, Spanish, Portuguese, Japanese, Korean, Chinese, Hindi, Arabic, Indonesian
- Automatic language detection based on system locale
- Single instance enforcement (named mutex)

## Usage Quick Guide

1. Click **New Action**.
2. Select an action type (Shutdown, Restart, Sleep, Lock, Log Off, Monitor Off).
3. Select a trigger type.
4. Enter trigger value and save.
5. Optionally open **Help** from:
   - Hamburger menu (`Help`)
   - Tray context menu (`Help`)
   - Action table right-click context menu (`Trigger usage help`)

## Tech Stack

| Component | Technology |
|-----------|------------|
| Runtime | .NET 8.0 (Windows 10+) |
| UI Framework | WinForms + WebView2 |
| Frontend | HTML / CSS / JavaScript (SPA) |
| UI Font | Material Icons (Round) |
| Data Storage | JSON files (Settings.json, ActionList.json, Logs.json) |
| Tests | xUnit |
| Installer | Inno Setup 6 |
| CI/CD | GitHub Actions |

## Project Structure

```
WindowsAutoPowerManager/
├── src/
│   ├── Program.cs                    # Entry point, single instance mutex
│   ├── MainForm.cs                   # Main form, WebView2 host, timer, update coordination
│   ├── SubWindow.cs                  # Settings/Logs/Help/About sub-windows
│   ├── ActionCountdownNotifier.cs    # Countdown popup before action execution
│   ├── Settings.cs                   # Settings model
│   ├── BuildInfo.cs                  # Build commit ID (injected by CI)
│   ├── Config/
│   │   ├── ActionTypes.cs            # Action type constants
│   │   ├── TriggerTypes.cs           # Trigger type constants
│   │   ├── UpdateCheckIntervals.cs   # Update interval constants
│   │   ├── Constants.cs              # App name, startup switch, update repository
│   │   ├── SettingsINI.cs            # Default settings
│   │   ├── LanguageINI.cs            # Language model
│   │   └── Lang/                     # 14 language files (English.cs, Turkish.cs, ...)
│   ├── Functions/
│   │   ├── ActionScheduler.cs        # Decides which actions are due (no side effects)
│   │   ├── Actions.cs                # System action execution (Win32 API calls)
│   │   ├── ActionValidation.cs       # Action conflict validation
│   │   ├── SystemIdleDetector.cs     # User idle time detection (Win32 API)
│   │   ├── DetectScreen.cs           # Session lock/unlock detection
│   │   ├── NotifySystem.cs           # Countdown notification logic
│   │   ├── UpdatePolicy.cs           # Version comparison, asset choice, interval rules
│   │   ├── UpdateChecker.cs          # GitHub release check, download, installer launch
│   │   ├── UpdateInfo.cs             # Update check result model
│   │   ├── Logger.cs                 # JSON-based action logging
│   │   ├── DebugLog.cs               # Opt-in rolling diagnostic trace
│   │   ├── JsonWriter.cs             # JSON file writer
│   │   ├── JsonPayload.cs            # Tolerant readers for web view payloads
│   │   ├── SettingsStorage.cs        # Settings persistence
│   │   ├── AppDataTransfer.cs        # Config import/export
│   │   ├── StartWithWindows.cs       # Startup entry management
│   │   ├── LanguageSelector.cs       # Language detection and loading
│   │   ├── LanguagePayloadCache.cs   # Cached language payload for the web view
│   │   ├── BuildMetadata.cs          # Version and commit id resolution
│   │   ├── ModernMenuRenderer.cs     # Custom tray menu renderer
│   │   └── WebViewEnvironmentProvider.cs # WebView2 environment singleton
│   ├── Enums/                        # UI-related enumerations
│   └── WebView/                      # Frontend assets
│       ├── Index.html                # Main SPA page
│       ├── SubWindow.html            # Sub-window SPA page
│       ├── Countdown.html            # Countdown notifier page
│       ├── Css/                      # Style.css / Countdown.css (+ minified output)
│       ├── Fonts/
│       │   └── MaterialIconsRound.woff2
│       └── Js/
│           ├── App.js                # SPA router
│           ├── Bridge.js             # C# ↔ JS bridge
│           ├── SubWindow.js          # Sub-window router
│           ├── Countdown.js          # Countdown UI logic
│           ├── Components/
│           │   ├── Toast.js          # Toast notification component
│           │   └── UpdateDialog.js   # Update prompt, progress and background indicator
│           └── Pages/
│               ├── Main.js           # Action list page
│               ├── Settings.js       # Settings page
│               ├── Logs.js           # Log viewer page
│               ├── Help.js           # Help and trigger usage page
│               └── About.js          # About page and manual update check
├── tests/
│   └── WindowsAutoPowerManager.Tests/ # xUnit tests for scheduling, validation and updates
├── tools/
│   ├── create-build.ps1              # PowerShell build script
│   ├── create-build.sh               # Bash build script
│   └── minify-css.js                 # CSS minifier, run by the build
├── installer.iss                     # Inno Setup installer script
├── Windows Auto Power Manager.csproj    # .NET project file
├── Windows Auto Power Manager.sln       # Solution file
└── .github/
    └── workflows/
        └── build-and-release.yml     # CI/CD pipeline
```

## Building

### Prerequisites
- .NET 8.0 SDK
- **Node.js** - the build runs `tools/minify-css.js` through a `MinifyCss` target, so a build fails without it
- Windows 10 or later
- WebView2 Runtime (included in modern Windows)

### Build
```bash
dotnet restore "Windows Auto Power Manager.sln"
dotnet build "Windows Auto Power Manager.sln" -c Release
```

### Test
```bash
dotnet test "tests/WindowsAutoPowerManager.Tests/WindowsAutoPowerManager.Tests.csproj" -c Release
```

### Publish (framework-dependent x64)
```bash
dotnet publish "Windows Auto Power Manager.csproj" -c Release -r win-x64 --self-contained false -p:PublishReadyToRun=true
```

### Create Installer
Requires [Inno Setup 6](https://jrsoftware.org/isinfo.php):
```bash
iscc installer.iss
```

## CI/CD

The GitHub Actions workflow (`.github/workflows/build-and-release.yml`) runs on pushes to `master` and `dev`:

- Tests run before the version is determined, so a failure stops the run without consuming a release number
- **dev branch**: Builds the project, creates the installer, and uploads artifacts
- **master branch**: Builds, creates installer, auto-increments the patch version from the latest git tag, and publishes a GitHub Release

## Architecture

The application uses a **hybrid architecture**:
- **Backend (C#)**: WinForms host with WebView2 controls. Handles system actions via Win32 API calls (`user32.dll`, `PowrProf.dll`), timer-based action scheduling, idle detection, session and display-power monitoring, settings/action/log persistence in JSON files, and startup entry management.
- **Scheduling**: `ActionScheduler` decides which actions are due and reports them; it performs no action itself, which keeps the rules testable and independent of the form.
- **Frontend (HTML/JS)**: Single Page Application rendered inside WebView2. Pages are lazy-loaded as separate JS modules. Communication between C# and JS happens via `PostWebMessageAsJson` / `WebMessageReceived` message passing through a Bridge layer.
- **Sub-windows** (Settings, Logs, Help, About) run in separate WebView2 forms, prewarmed in background for faster opening and suspended while hidden to release renderer memory.

## License

Copyright 2020 enginyilmaaz
