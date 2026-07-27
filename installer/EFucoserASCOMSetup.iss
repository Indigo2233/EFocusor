#define ProductName "EFucoser ASCOM Focuser Driver"
#define DriverFileName "ASCOM.EFucoser.Focuser.dll"
#define DriverSource "..\driver\EFucoserFocuserDriver\bin\Release\" + DriverFileName
#define ProductVersion GetVersionNumbersString(DriverSource)

[Setup]
AppId={{3F4866EA-609C-4B33-82B7-BEF61C258122}
AppName={#ProductName}
AppVersion={#ProductVersion}
AppPublisher=EFucoser
DefaultDirName={autopf}\ASCOM\EFucoser Focuser
DefaultGroupName=EFucoser
DisableProgramGroupPage=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x86compatible x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\dist
OutputBaseFilename=EFucoserASCOMSetup
SetupIconFile=..\driver\EFucoserFocuserDriver\ASCOM.ico
UninstallDisplayIcon={app}\ASCOM.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
VersionInfoVersion={#ProductVersion}
VersionInfoDescription={#ProductName} Setup
VersionInfoProductName={#ProductName}
VersionInfoProductVersion={#ProductVersion}

[Files]
Source: "{#DriverSource}"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\driver\EFucoserFocuserDriver\ASCOM.ico"; DestDir: "{app}"; Flags: ignoreversion

[Run]
Filename: "{dotnet40}\RegAsm.exe"; Parameters: """{app}\{#DriverFileName}"" /codebase"; StatusMsg: "Registering the 32-bit ASCOM driver..."; Flags: runhidden waituntilterminated
Filename: "{dotnet4064}\RegAsm.exe"; Parameters: """{app}\{#DriverFileName}"" /codebase"; StatusMsg: "Registering the 64-bit ASCOM driver..."; Flags: runhidden waituntilterminated; Check: IsWin64

[UninstallRun]
Filename: "{dotnet4064}\RegAsm.exe"; Parameters: """{app}\{#DriverFileName}"" /unregister"; RunOnceId: "Unregister64"; Flags: runhidden waituntilterminated; Check: IsWin64
Filename: "{dotnet40}\RegAsm.exe"; Parameters: """{app}\{#DriverFileName}"" /unregister"; RunOnceId: "Unregister32"; Flags: runhidden waituntilterminated

[Code]
function InitializeSetup(): Boolean;
var
  PlatformVersion: String;
begin
  Result := RegQueryStringValue(
    HKLM32, 'SOFTWARE\ASCOM', 'PlatformVersion', PlatformVersion);

  if not Result then
    MsgBox(
      'ASCOM Platform was not detected. Install ASCOM Platform 6 or later, then run this installer again.',
      mbError, MB_OK);
end;
