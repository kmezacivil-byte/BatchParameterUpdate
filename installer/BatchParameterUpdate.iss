; Batch Parameter Update - Inno Setup installer script
;
; Design decisions (see README > Design decisions for the full rationale):
;   - Per-user install (%AppData%), no admin elevation required. Matches the
;     folder the DeployToRevit dev-time MSBuild target already uses, and
;     avoids a UAC prompt for a single-user evaluation install.
;   - Revit detection checks for the default install folder rather than a
;     registry key. Simpler to verify, at the cost of missing a non-default
;     install path - acceptable for a single declared, tested version.
;   - Only Revit 2025 is declared compatible. See RevitVersion below.

#define AppVersion "1.0.0"
#define RevitVersion "2025"
#define RevitProductFolder "Revit " + RevitVersion
#define RevitDisplayName "Autodesk Revit " + RevitVersion
#define SourceDir "..\src\BatchParameterUpdate.Revit\bin\x64\Release\net8.0-windows"

[Setup]
AppId={{02D116BE-0431-4D9A-83BC-6CF8CBAAC016}
AppName=Batch Parameter Update
AppVersion={#AppVersion}
AppPublisher=Kevin Meza
DefaultDirName={userappdata}\Autodesk\Revit\Addins\{#RevitVersion}\BatchParameterUpdate
DisableDirPage=yes
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=Output
OutputBaseFilename=BatchParameterUpdate-Setup-{#AppVersion}
Compression=lzma
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64

; No code-signing certificate is used for this evaluation build. Windows
; SmartScreen and Revit's own unsigned-add-in prompt will still appear on
; first run - this is expected and documented in the README, not a defect.

[Files]
Source: "{#SourceDir}\BatchParameterUpdate.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\BatchParameterUpdate.Core.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\BatchParameterUpdate.deps.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\BatchParameterUpdate.addin"; DestDir: "{userappdata}\Autodesk\Revit\Addins\{#RevitVersion}"; Flags: ignoreversion

[UninstallDelete]
; Removes the BatchParameterUpdate subfolder itself on uninstall; Inno
; already removes the individual files listed above automatically.
Type: filesandordirs; Name: "{app}"

[Code]
function IsRevitInstalled(): Boolean;
begin
  Result := DirExists(ExpandConstant('{commonpf64}\Autodesk\{#RevitProductFolder}'))
    or DirExists(ExpandConstant('{commonpf}\Autodesk\{#RevitProductFolder}'));
end;

function IsProcessRunning(const ExeName: string): Boolean;
var
  ResultCode: Integer;
begin
  // Uses tasklist.exe rather than a Windows API call: no extra DLL imports
  // needed in the installer, and the exit code alone (0 = found, 1 = not
  // found) is enough - the command's text output is discarded.
  Result := Exec('cmd.exe', '/C tasklist /FI "IMAGENAME eq ' + ExeName + '" | find /I "' + ExeName + '" > nul',
    '', SW_HIDE, ewWaitUntilTerminated, ResultCode) and (ResultCode = 0);
end;

function InitializeSetup(): Boolean;
begin
  Result := True;

  if IsProcessRunning('Revit.exe') then
  begin
    MsgBox('Revit is currently running. Close Revit before installing, ' +
      'so the add-in files are not locked during copy.', mbError, MB_OK);
    Result := False;
    exit;
  end;

  if not IsRevitInstalled() then
  begin
    if MsgBox('{#RevitDisplayName} was not found at its default install path.' + #13#10 +
      'This add-in was only built and tested against {#RevitDisplayName}.' + #13#10#13#10 +
      'Continue anyway?', mbConfirmation, MB_YESNO) = IDNO then
    begin
      Result := False;
    end;
  end;
end;
