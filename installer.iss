; Windows Shutdown Helper - Inno Setup Script
; Build: once tools\create-build.ps1 ile publish alin, sonra bu .iss dosyasini Inno Setup ile derleyin.

#define MyAppName "Windows Shutdown Helper"
#ifndef MyAppVersion
  #define MyAppVersion "1.0.0"
#endif
#ifndef CommitHash
  #define CommitHash "000000"
#endif
#ifndef OutputName
  #define OutputName "WindowsShutdownHelper_Setup_" + MyAppVersion + "_" + CommitHash
#endif
#define MyAppPublisher "enginyilmaaz"
#define MyAppExeName "Windows Shutdown Helper.exe"

[Setup]
AppId={{9E741018-0E77-4458-BCDE-803A66EEF48C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=installer_output
OutputBaseFilename={#OutputName}
SetupIconFile=src\setup.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x86 x64
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"
Name: "german"; MessagesFile: "compiler:Languages\German.isl"
Name: "french"; MessagesFile: "compiler:Languages\French.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "italian"; MessagesFile: "compiler:Languages\Italian.isl"

[CustomMessages]
english.StartWithWindows=Start with Windows
english.OtherOptions=Other options:
turkish.StartWithWindows=Windows ile birlikte başlat
turkish.OtherOptions=Diğer seçenekler:
german.StartWithWindows=Mit Windows starten
german.OtherOptions=Weitere Optionen:
french.StartWithWindows=Démarrer avec Windows
french.OtherOptions=Autres options :
russian.StartWithWindows=Запускать вместе с Windows
russian.OtherOptions=Другие параметры:
italian.StartWithWindows=Avvia con Windows
italian.OtherOptions=Altre opzioni:

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startupentry"; Description: "{cm:StartWithWindows}"; GroupDescription: "{cm:OtherOptions}"; Flags: unchecked

[Files]
#ifexist "bin\Release\net8.0-windows\win-x64\publish\Windows Shutdown Helper.exe"
Source: "bin\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: Is64BitInstallMode
Source: "bin\Release\net8.0-windows\win-x86\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs skipifsourcedoesntexist; Check: not Is64BitInstallMode
#else
#ifexist "bin\Release\net8.0-windows\win-x86\publish\Windows Shutdown Helper.exe"
Source: "bin\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs skipifsourcedoesntexist; Check: Is64BitInstallMode
Source: "bin\Release\net8.0-windows\win-x86\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: not Is64BitInstallMode
#else
Source: "bin\Release\net8.0-windows\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net8.0-windows\WebView\*"; DestDir: "{app}\WebView"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "bin\Release\net8.0-windows\runtimes\*"; DestDir: "{app}\runtimes"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "bin\Release\net8.0-windows\*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net8.0-windows\Windows Shutdown Helper.runtimeconfig.json"; DestDir: "{app}"; Flags: ignoreversion
#endif
#endif

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#MyAppName}"; ValueData: """{app}\{#MyAppExeName}"" -runInTaskBar"; Flags: uninsdeletevalue; Tasks: startupentry

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: files; Name: "{app}\settings.json"
Type: files; Name: "{app}\actionList.json"
Type: files; Name: "{app}\logs.json"
Type: filesandordirs; Name: "{app}\lang"
Type: filesandordirs; Name: "{app}\WebView"
Type: filesandordirs; Name: "{app}\runtimes"
