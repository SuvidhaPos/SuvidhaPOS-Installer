#define MyAppName "Suvidha POS Installer"
#define MyAppVersion "2.1.0"
#define MyAppPublisher "Suvidha POS"
#define MyAppExeName "SuvidhaPos-Installer.exe"

[Setup]
AppId={{D8B4B6D5-6C4E-4F0A-9F70-9B3B4F9C2D10}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\SuvidhaPOS Setup
DefaultGroupName=Suvidha POS
OutputDir=..\release
OutputBaseFilename=SuvidhaPOS-Installer-Setup
Compression=lzma2
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
SetupIconFile=..\Assets\SuvidhaPOS.ico
WizardStyle=modern
DisableProgramGroupPage=yes
Uninstallable=yes

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autodesktop}\Suvidha POS Installer"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Suvidha POS Installer"; Filename: "{app}\{#MyAppExeName}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch Suvidha POS Installer"; Flags: nowait postinstall skipifsilent runascurrentuser
