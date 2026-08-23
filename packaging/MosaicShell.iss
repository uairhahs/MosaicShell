; MosaicShell Inno Setup script (UniGetUI-style).
; Compile with: ISCC.exe packaging\MosaicShell.iss
; Expects packaging\stage\ with Host\, Mosaicist\, Tiles\, VERSION.txt
;
; Versioning is date-build (yyyy.M.d-bN), not semver.
; AppVersion / filenames use the full tag; VersionInfoVersion is numeric PE metadata only.

#ifndef MyAppVersion
  #define MyAppVersion "0.0.0-dev"
#endif
#ifndef MyVersionInfoVersion
  #define MyVersionInfoVersion "0.0.0.0"
#endif

#define MyAppName "MosaicShell"
#define MyAppPublisher "MosaicShell"
#define MyAppURL "https://github.com/uairhahs/MosaicShell"
#define MyAppExeName "MosaicShell.Host.exe"
#define StageDir "stage"

[Setup]
AppId={{A7C3E9F1-4B2D-4E8A-9C1F-6D5E0B8A2C34}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
VersionInfoVersion={#MyVersionInfoVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={localappdata}\Programs\MosaicShell
DefaultGroupName=MosaicShell
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
OutputDir=output
OutputBaseFilename=MosaicShell-Setup-{#MyAppVersion}
SetupIconFile=..\host\MosaicShell.Host\Assets\mosaicshell.ico
UninstallDisplayIcon={app}\Host\{#MyAppExeName}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
MinVersion=10.0
CloseApplications=yes
RestartIfNeededByRun=no
UsePreviousAppDir=yes
DisableWelcomePage=no
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked
Name: "startupicon"; Description: "Launch MosaicShell at logon (tray-only)"; GroupDescription: "Startup:"

[Files]
; Application payload
Source: "{#StageDir}\Host\*"; DestDir: "{app}\Host"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#StageDir}\Mosaicist\*"; DestDir: "{app}\Mosaicist"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#StageDir}\Tiles\*"; DestDir: "{app}\Tiles"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#StageDir}\VERSION.txt"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
; Deploy this Setup into the app folder for repair / reinstall (UniGetUI pattern)
Source: "{srcexe}"; DestDir: "{app}"; DestName: "MosaicShell-Setup.exe"; Flags: external ignoreversion

[Icons]
Name: "{group}\MosaicShell"; Filename: "{app}\Host\{#MyAppExeName}"; WorkingDir: "{app}\Host"
Name: "{autodesktop}\MosaicShell"; Filename: "{app}\Host\{#MyAppExeName}"; WorkingDir: "{app}\Host"; Tasks: desktopicon
Name: "{userstartup}\MosaicShell"; Filename: "{app}\Host\{#MyAppExeName}"; Parameters: "--tray-only"; WorkingDir: "{app}\Host"; Tasks: startupicon

[Run]
; Install default modules from bundled Tiles/ (release layout)
Filename: "{app}\Mosaicist\Mosaicist.exe"; Parameters: "install-module Tessera"; StatusMsg: "Installing Tessera..."; Flags: runhidden waituntilterminated
Filename: "{app}\Mosaicist\Mosaicist.exe"; Parameters: "install-module Mixdeck"; StatusMsg: "Installing Mixdeck..."; Flags: runhidden waituntilterminated
Filename: "{app}\Host\{#MyAppExeName}"; Description: "Launch MosaicShell"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: files; Name: "{app}\MosaicShell-Setup.exe"
Type: files; Name: "{userstartup}\MosaicShell.lnk"
Type: files; Name: "{userstartup}\MosaicShell.url"
