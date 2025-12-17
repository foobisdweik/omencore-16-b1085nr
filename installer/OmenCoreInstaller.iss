#define MyAppName "OmenCore"
#ifndef MyAppVersion
  #define MyAppVersion "1.5.0"
#endif
#define MyAppPublisher "OmenCore Project"
#define MyAppExeName "OmenCore.exe"
#define PawnIOInstallerUrl "https://pawnio.eu/PawnIO.exe"

[Setup]
AppId={{6F5B6F3F-8FAF-4FC8-A5E0-4E2C0E8F2E2B}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL="https://github.com/theantipopau/OmenCore"
AppSupportURL="https://github.com/theantipopau/OmenCore/issues"
AppUpdatesURL="https://github.com/theantipopau/OmenCore/releases"
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
SetupIconFile=..\src\OmenCoreApp\Assets\OmenCore.ico
; Branding images
WizardImageFile=wizard-large.bmp
WizardSmallImageFile=wizard-small.bmp
Compression=lzma2/ultra64
SolidCompression=yes
OutputDir=..\\artifacts
OutputBaseFilename=OmenCoreSetup-{#MyAppVersion}
PrivilegesRequired=admin
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible
; Modern look
WizardResizable=no
DisableWelcomePage=no
LicenseFile=
InfoBeforeFile=
InfoAfterFile=

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startmenu"; Description: "Create Start Menu shortcut"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checked
Name: "installpawnio"; Description: "Install PawnIO driver (Secure Boot compatible, recommended for advanced features)"; GroupDescription: "🔧 Hardware Drivers:"; Flags: checkedonce
Name: "autostart"; Description: "Start OmenCore with Windows"; GroupDescription: "⚡ Startup Options:"; Flags: unchecked

[Files]
; Self-contained app with embedded .NET runtime - no separate .NET installation needed
Source: "..\\publish\\win-x64\\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; PawnIO installer (optional)
Source: "PawnIO_setup.exe"; DestDir: "{tmp}"; Flags: ignoreversion deleteafterinstall; Tasks: installpawnio; Check: PawnIOInstallerExists
; Default config
Source: "..\\config\\default_config.json"; DestDir: "{app}\\config"; Flags: ignoreversion onlyifdoesntexist

[Dirs]
Name: "{app}\\logs"; Permissions: users-modify
Name: "{app}\\config"; Permissions: users-modify

[Icons]
Name: "{autoprograms}\\{#MyAppName}"; Filename: "{app}\\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{autodesktop}\\{#MyAppName}"; Filename: "{app}\\{#MyAppExeName}"; Tasks: desktopicon; WorkingDir: "{app}"
Name: "{userstartup}\\{#MyAppName}"; Filename: "{app}\\{#MyAppExeName}"; Tasks: autostart; WorkingDir: "{app}"; Parameters: "--minimized"

[Run]
; Install PawnIO driver if bundled
Filename: "{tmp}\\PawnIO_setup.exe"; Parameters: "/SILENT"; StatusMsg: "Installing PawnIO driver (Secure Boot compatible)..."; Flags: waituntilterminated; Tasks: installpawnio; Check: PawnIOInstallerExists
; Launch OmenCore with elevation (shellexec verb=runas)
Filename: "{app}\\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent shellexec runascurrentuser; Verb: runas

[UninstallRun]
; Stop OmenCore if running
Filename: "taskkill"; Parameters: "/F /IM OmenCore.exe"; Flags: runhidden; RunOnceId: "StopOmenCore"

[UninstallDelete]
Type: filesandordirs; Name: "{app}\\logs"
Type: dirifempty; Name: "{app}"

[Code]
function PawnIOInstallerExists: Boolean;
begin
  Result := FileExists(ExpandConstant('{src}\\PawnIO_setup.exe'));
end;

function IsPawnIOInstalled: Boolean;
var
  InstallPath: String;
begin
  Result := RegQueryStringValue(HKLM, 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\PawnIO', 'InstallLocation', InstallPath);
  if not Result then
    Result := DirExists(ExpandConstant('{pf}\\PawnIO'));
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  if CurPageID = wpSelectTasks then
  begin
    if IsPawnIOInstalled then
    begin
      // PawnIO already installed, uncheck the task
      WizardForm.TasksList.Checked[2] := False;
    end;
  end;
end;

function InitializeSetup(): Boolean;
begin
  Result := True;
end;

function InitializeUninstall(): Boolean;
begin
  Result := True;
end;

[Messages]
WelcomeLabel2=This will install [name/ver] on your computer.%n%n%n╔═══════════════════════════════════════╗%n║    🎮  OmenCore v1.5.0  🎮    ║%n║   Complete OMEN Control Suite   ║%n╚═══════════════════════════════════════╝%n%n%n🌀  FAN CONTROL%n   • Custom fan curves with visual editor%n   • Per-model calibration & verification%n   • Thermal protection & hysteresis%n%n📊  MONITORING%n   • Real-time CPU/GPU temps & loads%n   • Fan RPM and power consumption%n   • Performance throttling detection%n%n⚡  POWER & PERFORMANCE%n   • Intel/AMD CPU undervolting%n   • GPU Dynamic Boost (+25W on RTX 5080)%n   • Performance mode switching%n%n💡  RGB LIGHTING%n   • 4-zone keyboard control%n   • Multi-backend support%n   • Game profile auto-switching%n%n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━%n%nReplaces HP OMEN Gaming Hub with more%nfeatures and better performance.%n%nClick Next to continue.
