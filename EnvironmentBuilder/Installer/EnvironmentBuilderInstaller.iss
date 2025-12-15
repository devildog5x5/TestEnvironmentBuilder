; ============================================================================
; Environment Builder Installer Script
; Inno Setup Script for Environment Builder
; Evolved from TreeBuilder 3.4 by Robert Foster
; ============================================================================

#define MyAppName "Environment Builder"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Test Warrior"
#define MyAppURL "https://github.com/devildog5x5/TestEnvironmentBuilder"
#define MyAppExeName "EnvironmentBuilderApp.exe"

[Setup]
; Application information
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}

; Installation directories
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes

; Output settings
OutputDir=Output
OutputBaseFilename=EnvironmentBuilderSetup
SetupIconFile=..\EnvironmentBuilderApp\Resources\TestTree.ico
Compression=lzma
SolidCompression=yes

; Requirements
PrivilegesRequired=admin
WizardStyle=modern

; Visual settings
WizardImageFile=compiler:WizModernImage.bmp
WizardSmallImageFile=compiler:WizModernSmallImage.bmp

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Main application files from publish folder
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
; Start menu shortcut
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
; Desktop shortcut (optional)
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
; Option to launch after installation
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
// Display custom welcome message
function InitializeSetup: Boolean;
begin
  Result := True;
end;

