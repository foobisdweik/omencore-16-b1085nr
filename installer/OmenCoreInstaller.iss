#define MyAppName "OmenCore"
#ifndef MyAppVersion
  #define MyAppVersion "1.4.0"
#endif
#define MyAppPublisher "OmenCore Project"
#define MyAppExeName "OmenCore.exe"
#define PawnIOInstallerUrl "https://pawnio.eu/PawnIO.exe"

[Setup]
AppId={{6F5B6F3F-8FAF-4FC8-A5E0-4E2C0E8F2E2B}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL="https://github.com/theantipopau/omencore"
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

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "installpawnio"; Description: "Install PawnIO driver for Secure Boot compatible EC access (Recommended for 2023+ OMEN laptops)"; GroupDescription: "Hardware Control:"; Flags: checkedonce
Name: "installdriver"; Description: "Install WinRing0 driver (Alternative - requires Secure Boot disabled)"; GroupDescription: "Hardware Control:"; Flags: unchecked

[Files]
; Self-contained app with embedded .NET runtime - no separate .NET installation needed
Source: "..\\publish\\win-x64\\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; PawnIO installer (optional)
Source: "PawnIO_setup.exe"; DestDir: "{tmp}"; Flags: ignoreversion deleteafterinstall; Tasks: installpawnio; Check: PawnIOInstallerExists

[Icons]
Name: "{autoprograms}\\{#MyAppName}"; Filename: "{app}\\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{autodesktop}\\{#MyAppName}"; Filename: "{app}\\{#MyAppExeName}"; Tasks: desktopicon; WorkingDir: "{app}"

[Run]
; Install PawnIO driver if bundled
Filename: "{tmp}\\PawnIO_setup.exe"; Parameters: "/SILENT"; StatusMsg: "Installing PawnIO driver (Secure Boot compatible)..."; Flags: waituntilterminated; Tasks: installpawnio; Check: PawnIOInstallerExists
; Launch OmenCore with elevation (shellexec verb=runas)
Filename: "{app}\\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent shellexec runascurrentuser; Verb: runas

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
      WizardForm.TasksList.Checked[1] := False;
    end;
  end;
end;

function InitializeSetup(): Boolean;
begin
  Result := True;
end;

[Messages]
WelcomeLabel2=This will install [name/ver] on your computer.%n%n%n╔════════════════════════════════════╗%n║   🎮  HP OMEN Control Suite  🎮   ║%n╚════════════════════════════════════╝%n%n✨ FEATURES%n%n  🌀  Advanced Fan Control & Custom Curves%n  📊  Real-time Hardware Monitoring%n  ⚡  CPU Undervolting (Intel & AMD)%n  💡  4-Zone RGB Keyboard Control%n  🎯  Game Profile Auto-Switching%n  🚀  GPU Power Management%n  🖥️  Desktop & Laptop Support%n%n🔧 HARDWARE CONTROL%n%n  🛡️  Secure Boot Compatible (PawnIO driver)%n  🔌  WMI BIOS control for max compatibility%n  📈  Performance monitoring with OSD overlay%n  ⚙️  Direct EC register access option%n%n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━%n%nComplete replacement for HP OMEN Gaming Hub%nwith more features, better performance, and full control.%n%nClick Next to continue, or Cancel to exit Setup.
