#define AppName "Suvidha POS Installer"
#define AppVersion "3.0.0"
#define AppPublisher "Suvidha POS"
#define AppExeName "SuvidhaPOS-Installer.exe"

[Setup]
AppId={{E8B7D4B5-4C75-4F61-AE2A-9F0B4D7A31C8}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\Suvidha POS Installer
DefaultGroupName=Suvidha POS
OutputDir=..\artifacts
OutputBaseFilename=SuvidhaPOS-Installer-Setup
Compression=lzma2
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
WizardStyle=modern
DisableProgramGroupPage=yes

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion

[Icons]
Name: "{group}\Suvidha POS Installer"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\Suvidha POS Installer"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional icons:"

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch Suvidha POS Installer"; Flags: nowait postinstall skipifsilent
