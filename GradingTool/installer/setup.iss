[Setup]
AppId={{8B7E5F76-2C4D-4CC8-8F0A-5F0F4B8D5A21}
AppName=Rubrica
AppVersion=0.0.0-dev
AppPublisher=Olivier Tremblay
DefaultDirName={autopf}\Rubrica
DefaultGroupName=Rubrica
UninstallDisplayIcon={app}\Rubrica.exe
SetupIconFile=..\app.ico
OutputBaseFilename=Rubrica-Setup
OutputDir=.
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "french"; MessagesFile: "compiler:Languages\French.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "..\..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Rubrica"; Filename: "{app}\Rubrica.exe"; IconFilename: "{app}\app.ico"
Name: "{group}\Désinstaller Rubrica"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Rubrica"; Filename: "{app}\Rubrica.exe"; Tasks: desktopicon; IconFilename: "{app}\app.ico"

[Run]
Filename: "{app}\Rubrica.exe"; Description: "Lancer Rubrica"; Flags: nowait postinstall skipifsilent
