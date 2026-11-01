#ifndef AppVersion
  #error AppVersion must be passed by Package-Release.ps1
#endif
#ifndef SourceDir
  #error SourceDir must be passed by Package-Release.ps1
#endif
#ifndef OutputDir
  #error OutputDir must be passed by Package-Release.ps1
#endif

[Setup]
AppId={{87C06DE4-75B8-4471-BBCB-6AC9CB282F63}
AppName=PetDesk
AppVersion={#AppVersion}
AppVerName=PetDesk {#AppVersion}
AppPublisher=PetDesk contributors
DefaultDirName={localappdata}\Programs\PetDesk
DefaultGroupName=PetDesk
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.22000
UninstallDisplayName=PetDesk
UninstallDisplayIcon={app}\PetDesk.exe
OutputDir={#OutputDir}
OutputBaseFilename=PetDesk-Setup-{#AppVersion}-win-x64
SetupIconFile=..\src\PetDesk.App\Resources\PetDesk.ico
LicenseFile=..\LICENSE
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
DisableProgramGroupPage=yes
UsePreviousAppDir=yes
CloseApplications=yes
RestartApplications=no
SetupLogging=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\PetDesk"; Filename: "{app}\PetDesk.exe"; WorkingDir: "{app}"; AppUserModelID: "PetDesk.Desktop"
Name: "{autodesktop}\PetDesk"; Filename: "{app}\PetDesk.exe"; WorkingDir: "{app}"; Tasks: desktopicon; AppUserModelID: "PetDesk.Desktop"

[Run]
Filename: "{app}\PetDesk.exe"; Description: "{cm:LaunchProgram,PetDesk}"; Flags: nowait postinstall skipifsilent

[CustomMessages]
english.ClosePetDesk=Please close PetDesk before continuing. Your settings and pets will be kept.
russian.ClosePetDesk=Закройте PetDesk перед продолжением. Настройки и питомцы будут сохранены.
english.StartupCleanupFailed=Could not remove the PetDesk startup registration. Close PetDesk and try again.
russian.StartupCleanupFailed=Не удалось убрать запись автозапуска PetDesk. Закройте приложение и повторите попытку.

[Code]
function RunPetDeskCommand(const Arguments: String): Boolean;
var Exe: String; Code: Integer;
begin
  Exe := ExpandConstant('{app}\PetDesk.exe');
  if not FileExists(Exe) then begin Result := True; exit; end;
  Result := Exec(Exe, Arguments, ExpandConstant('{app}'), SW_HIDE, ewWaitUntilTerminated, Code);
  if Result then Result := Code = 0;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  Result := '';
  if not RunPetDeskCommand('--exit') then Result := CustomMessage('ClosePetDesk');
end;

function InitializeUninstall(): Boolean;
begin
  Result := RunPetDeskCommand('--exit');
  if not Result then begin SuppressibleMsgBox(CustomMessage('ClosePetDesk'), mbError, MB_OK, IDOK); exit; end;
  Result := RunPetDeskCommand('--unregister-startup');
  if not Result then SuppressibleMsgBox(CustomMessage('StartupCleanupFailed'), mbError, MB_OK, IDOK);
  { User data in LocalAppData\PetDesk is intentionally never deleted. }
end;
