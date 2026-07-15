; SceneManager 설치 스크립트 (Inno Setup 6+)
; 컴파일: iscc installer\SceneManager.iss /DMyAppVersion=1.0.0
; 전제: 게시 산출물이 artifacts\publish\ 에 있어야 한다
;   dotnet publish src/SceneManager -c Release -r win-x64 --self-contained true -o artifacts/publish
;   dotnet publish src/SceneRunner  -c Release -r win-x64 --self-contained true -o artifacts/publish

#ifndef MyAppVersion
  #define MyAppVersion "0.0.0"
#endif

#define MyAppName "SceneManager"
#define MyAppPublisher "ChangDaeJun"
#define MyAppURL "https://github.com/ChangDaeJun/SceneManager"
#define MyAppExeName "SceneManager.exe"

[Setup]
AppId={{9F6E1A2B-3C4D-4E5F-8A9B-0C1D2E3F4A5B}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/issues
AppUpdatesURL={#MyAppURL}/releases
; 관리자 권한 없이 사용자별 설치 → UAC 없음, 고정 경로(바로가기 안정)
PrivilegesRequired=lowest
DefaultDirName={localappdata}\Programs\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\{#MyAppExeName}
OutputDir=..\artifacts
OutputBaseFilename=SceneManager-setup-{#MyAppVersion}
SetupIconFile=..\assets\app.ico
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; 게시 폴더 전체(편집기 + 실행기 + 런타임)를 설치 폴더에 복사
Source: "..\artifacts\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent

; 참고: 사용자 데이터(%LOCALAPPDATA%\SceneManager\)는 설치 폴더 밖이라 제거 시 지워지지 않는다.
; 사용자가 만든 바탕화면 씬 바로가기는 제거 후 실행기가 사라져 동작하지 않게 된다.
