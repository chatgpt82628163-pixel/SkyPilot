; SkyPilot installer (Inno Setup 6).
; Built by .github/workflows/release.yml:
;   iscc /DAppVersion=1.0.0 /DSourceDir=..\publish\SkyPilot /DOutputDir=..\dist installer\SkyPilot.iss
; The program updates itself by running a newer installer silently (see its UpdateChecker):
;   SkyPilot-Setup-x.y.z.exe /SILENT /SP- /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS /LAUNCH=1

#ifndef AppVersion
  #define AppVersion "0.3.0"
#endif
#ifndef SourceDir
  #define SourceDir "..\publish\SkyPilot"
#endif
#ifndef OutputDir
  #define OutputDir "..\dist"
#endif

#define AppName "SkyPilot"
#define AppExe "SkyPilot.exe"

[Setup]
AppId={{AC69B878-0A5B-4B59-83B3-0AD1ECF25925}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher=SkyNetwork
AppPublisherURL=https://sky.network.npzy2.us/docs/software
AppSupportURL=https://sky.network.npzy2.us/support
VersionInfoVersion={#AppVersion}
DefaultDirName={autopf}\SkyNetwork\{#AppName}
DefaultGroupName=SkyNetwork
DisableProgramGroupPage=yes
; Installs for the current user (no administrator rights, so updates install without questions) or, if chosen,
; for all users. An update keeps the choice made at the first install (UsePreviousPrivileges).
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog commandline
UsePreviousPrivileges=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
OutputDir={#OutputDir}
OutputBaseFilename={#AppName}-Setup-{#AppVersion}
UninstallDisplayIcon={app}\{#AppExe}
SetupIconFile=..\src\SkyPilot.App\Assets\SkyPilot.ico
UninstallDisplayName={#AppName}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
; The program is started by the [Run] entry below after an update, not a second time by the restart manager.
RestartApplications=no

[Languages]
Name: "ru"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "en"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent
; After a silent self-update (/LAUNCH=1) the program comes back by itself, as the user, not as administrator.
Filename: "{app}\{#AppExe}"; Flags: nowait runasoriginaluser; Check: LaunchAfterSilentUpdate

; Settings in %APPDATA%\SkyPilot are kept on uninstall.

[Code]
function LaunchAfterSilentUpdate(): Boolean;
begin
  Result := WizardSilent and (ExpandConstant('{param:LAUNCH|0}') = '1');
end;
