; ============================================================================
; Environment Builder Installer Script
; Unified Application - All Functionality in One Tabbed Interface
; Evolved from TreeBuilder 3.4 by Robert Foster
; Test Brutally - Build Your Level of Complexity
; ============================================================================

#define MyAppName "Environment Builder"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Test Warrior - Robert Foster"
#define MyAppURL "https://github.com/devildog5x5/TestEnvironmentBuilder"
#define MyAppExeName "EnvironmentBuilderApp.exe"

[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=Output
OutputBaseFilename=EnvironmentBuilderSetup
Compression=lzma
SolidCompression=yes
PrivilegesRequired=admin
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "..\publish\EnvironmentBuilderApp.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
procedure CurStepChanged(CurStep: TSetupStep);
var
  BasePath: String;
begin
  if CurStep = ssPostInstall then
  begin
    BasePath := ExpandConstant('{userappdata}\EnvironmentBuilder');
    CreateDir(BasePath);
    CreateDir(BasePath + '\LDIF');
    CreateDir(BasePath + '\Logs');
    CreateDir(BasePath + '\Reports');
    CreateDir(BasePath + '\Configs');
  end;
end;
