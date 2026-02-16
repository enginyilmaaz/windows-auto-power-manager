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
english.PreserveDataTitle=Preserve Existing Data
english.PreserveDataDescription=Previous installation data was found in the selected folder.
english.PreserveDataOption=Keep old settings/tasks/logs (Recommended)
english.RemoveDataOnUninstall=Also delete user data (settings, tasks, logs)?
turkish.StartWithWindows=Windows ile birlikte başlat
turkish.OtherOptions=Diğer seçenekler:
turkish.PreserveDataTitle=Var olan verileri koru
turkish.PreserveDataDescription=Seçili klasörde önceki kurulum verileri bulundu.
turkish.PreserveDataOption=Eski ayar/görev/kayıtları koru (Önerilen)
turkish.RemoveDataOnUninstall=Kullanıcı verileri de silinsin mi (ayarlar, görevler, kayıtlar)?
german.StartWithWindows=Mit Windows starten
german.OtherOptions=Weitere Optionen:
german.PreserveDataTitle=Vorhandene Daten beibehalten
german.PreserveDataDescription=Im ausgewählten Ordner wurden Daten einer vorherigen Installation gefunden.
german.PreserveDataOption=Alte Einstellungen/Aufgaben/Protokolle behalten (Empfohlen)
german.RemoveDataOnUninstall=Benutzerdaten (Einstellungen, Aufgaben, Protokolle) ebenfalls löschen?
french.StartWithWindows=Démarrer avec Windows
french.OtherOptions=Autres options :
french.PreserveDataTitle=Conserver les données existantes
french.PreserveDataDescription=Des données d'une installation précédente ont été trouvées dans le dossier sélectionné.
french.PreserveDataOption=Conserver les anciens paramètres/tâches/journaux (Recommandé)
french.RemoveDataOnUninstall=Supprimer également les données utilisateur (paramètres, tâches, journaux) ?
russian.StartWithWindows=Запускать вместе с Windows
russian.OtherOptions=Другие параметры:
russian.PreserveDataTitle=Сохранить существующие данные
russian.PreserveDataDescription=В выбранной папке найдены данные предыдущей установки.
russian.PreserveDataOption=Сохранить старые настройки/задачи/журналы (Рекомендуется)
russian.RemoveDataOnUninstall=Удалить также пользовательские данные (настройки, задачи, журналы)?
italian.StartWithWindows=Avvia con Windows
italian.OtherOptions=Altre opzioni:
italian.PreserveDataTitle=Mantieni i dati esistenti
italian.PreserveDataDescription=Nella cartella selezionata sono stati trovati dati di una precedente installazione.
italian.PreserveDataOption=Mantieni vecchie impostazioni/attività/log (Consigliato)
italian.RemoveDataOnUninstall=Eliminare anche i dati utente (impostazioni, attività, log)?

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "startupentry"; Description: "{cm:StartWithWindows}"; GroupDescription: "{cm:OtherOptions}"; Flags: unchecked

[Files]
#ifexist "bin\Release\net8.0-windows\win-x64\publish\Windows Shutdown Helper.exe"
Source: "bin\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "Settings.json,settings.json,ActionList.json,actionList.json,actionlist.json,Logs.json,logs.json,lang\*"; Check: Is64BitInstallMode
Source: "bin\Release\net8.0-windows\win-x86\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs skipifsourcedoesntexist; Excludes: "Settings.json,settings.json,ActionList.json,actionList.json,actionlist.json,Logs.json,logs.json,lang\*"; Check: not Is64BitInstallMode
#else
#ifexist "bin\Release\net8.0-windows\win-x86\publish\Windows Shutdown Helper.exe"
Source: "bin\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs skipifsourcedoesntexist; Excludes: "Settings.json,settings.json,ActionList.json,actionList.json,actionlist.json,Logs.json,logs.json,lang\*"; Check: Is64BitInstallMode
Source: "bin\Release\net8.0-windows\win-x86\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "Settings.json,settings.json,ActionList.json,actionList.json,actionlist.json,Logs.json,logs.json,lang\*"; Check: not Is64BitInstallMode
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

[Code]
var
  PreserveDataPage: TWizardPage;
  PreserveDataCheckBox: TNewCheckBox;
  RemoveUserDataOnUninstall: Boolean;

function HasUserData(const RootDir: string): Boolean;
var
  BaseDir: string;
begin
  BaseDir := AddBackslash(RootDir);
  Result :=
    FileExists(BaseDir + 'Settings.json') or
    FileExists(BaseDir + 'settings.json') or
    FileExists(BaseDir + 'ActionList.json') or
    FileExists(BaseDir + 'actionList.json') or
    FileExists(BaseDir + 'actionlist.json') or
    FileExists(BaseDir + 'Logs.json') or
    FileExists(BaseDir + 'logs.json') or
    DirExists(BaseDir + 'lang');
end;

procedure DeleteUserData(const RootDir: string);
var
  BaseDir: string;
begin
  BaseDir := AddBackslash(RootDir);
  DeleteFile(BaseDir + 'Settings.json');
  DeleteFile(BaseDir + 'settings.json');
  DeleteFile(BaseDir + 'ActionList.json');
  DeleteFile(BaseDir + 'actionList.json');
  DeleteFile(BaseDir + 'actionlist.json');
  DeleteFile(BaseDir + 'Logs.json');
  DeleteFile(BaseDir + 'logs.json');
  DelTree(BaseDir + 'lang', True, True, True);
end;

procedure DeleteLegacyFiles(const RootDir: string);
var
  BaseDir: string;
begin
  BaseDir := AddBackslash(RootDir);
  DeleteFile(BaseDir + 'LastPage.txt');
end;

function ShouldPreserveUserData(): Boolean;
begin
  Result := True;
  if PreserveDataCheckBox <> nil then
  begin
    Result := PreserveDataCheckBox.Checked;
  end;
end;

procedure InitializeWizard();
begin
  PreserveDataPage := CreateCustomPage(
    wpSelectDir,
    ExpandConstant('{cm:PreserveDataTitle}'),
    ExpandConstant('{cm:PreserveDataDescription}')
  );

  PreserveDataCheckBox := TNewCheckBox.Create(PreserveDataPage);
  PreserveDataCheckBox.Parent := PreserveDataPage.Surface;
  PreserveDataCheckBox.Left := 0;
  PreserveDataCheckBox.Top := ScaleY(8);
  PreserveDataCheckBox.Width := PreserveDataPage.SurfaceWidth;
  PreserveDataCheckBox.Caption := ExpandConstant('{cm:PreserveDataOption}');
  PreserveDataCheckBox.Checked := True;
end;

function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := False;
  if (PreserveDataPage <> nil) and (PageID = PreserveDataPage.ID) then
  begin
    Result := not HasUserData(WizardDirValue);
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if (CurStep = ssInstall) and HasUserData(WizardDirValue) and (not ShouldPreserveUserData()) then
  begin
    DeleteUserData(WizardDirValue);
  end;

  if CurStep = ssPostInstall then
  begin
    DeleteLegacyFiles(WizardDirValue);
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  UninstallDir: string;
begin
  UninstallDir := ExpandConstant('{app}');

  if CurUninstallStep = usUninstall then
  begin
    RemoveUserDataOnUninstall := False;
    if HasUserData(UninstallDir) then
    begin
      RemoveUserDataOnUninstall :=
        MsgBox(
          ExpandConstant('{cm:RemoveDataOnUninstall}'),
          mbConfirmation,
          MB_YESNO or MB_DEFBUTTON2
        ) = IDYES;
    end;
  end;

  if (CurUninstallStep = usPostUninstall) and RemoveUserDataOnUninstall then
  begin
    DeleteUserData(UninstallDir);
  end;

  if CurUninstallStep = usPostUninstall then
  begin
    DeleteLegacyFiles(UninstallDir);
  end;
end;
