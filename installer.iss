; Windows Shutdown Helper - Inno Setup Script
; Build: Once projeyi Visual Studio'da Release olarak build edin, sonra bu .iss dosyasini Inno Setup ile derleyin.

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
SetupIconFile=setup.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x86 x64
ArchitecturesInstallIn64BitMode=x64
MinVersion=10.0

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startupentry"; Description: "Windows ile birlikte baslat / Start with Windows"; GroupDescription: "Diger secenekler / Other options:"; Flags: unchecked

[Files]
Source: "bin\Release\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\wwwroot\*"; DestDir: "{app}\wwwroot"; Flags: ignoreversion recursesubdirs createallsubdirs

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
Type: filesandordirs; Name: "{app}\wwwroot"
