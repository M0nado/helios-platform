; ============================================================================
; HELIOS Platform Professional Windows Installer
; ============================================================================
; NSIS Installer Script v2.0
; Supports Windows 11 Pro and Enterprise
; Production-Ready Installation Package
; ============================================================================

; Compression settings
SetCompress off

; Include Modern UI
!include "MUI2.nsh"
!include "x64.nsh"
!include "FileFunc.nsh"
!include "LogicLib.nsh"
!include "WinMessages.nsh"

; ============================================================================
; GENERAL SETTINGS
; ============================================================================

Name "HELIOS Platform"
OutFile "HELIOS-Platform-Setup.exe"
InstallDir "$PROGRAMFILES\HELIOS Platform"
InstallDirRegKey HKCU "Software\HELIOS Platform" "InstallPath"

; Request admin rights
RequestExecutionLevel admin

; Version information
VIProductVersion "1.0.0.0"
VIAddVersionKey /LANG=1033 "ProductName" "HELIOS Platform"
VIAddVersionKey /LANG=1033 "ProductVersion" "1.0.0.0"
VIAddVersionKey /LANG=1033 "CompanyName" "HELIOS Solutions"
VIAddVersionKey /LANG=1033 "FileVersion" "1.0.0.0"
VIAddVersionKey /LANG=1033 "FileDescription" "HELIOS Platform - Enterprise Deployment & Automation"
VIAddVersionKey /LANG=1033 "LegalCopyright" "© 2024 HELIOS Solutions. All rights reserved."

; ============================================================================
; INSTALLER VARIABLES
; ============================================================================

Var DeploymentTier
Var InstallDrive
Var EnableAutoStart
Var EnableTelemetry
Var ProgramFilesPath

; ============================================================================
; MUI CONFIGURATION
; ============================================================================

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "LICENSE.txt"
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_LANGUAGE "English"

; ============================================================================
; SECTION: CORE INSTALLATION
; ============================================================================

Section "HELIOS Platform Core" SecCore
  SectionIn RO
  SetOutPath "$INSTDIR"
  
  DetailPrint "Installing HELIOS Platform Core files..."
  
  ; Copy core files (placeholder - actual build files will be added)
  File /oname=HELIOS.Platform.exe "HELIOS.Platform.exe"
  
  ; Create directories
  CreateDirectory "$INSTDIR\config"
  CreateDirectory "$INSTDIR\logs"
  CreateDirectory "$INSTDIR\data"
  CreateDirectory "$INSTDIR\plugins"
  
  ; Create default config
  FileOpen $0 "$INSTDIR\config\helios.config" w
  FileWrite $0 "[HELIOS Platform Configuration]$\r$\n"
  FileWrite $0 "Version=1.0.0.0$\r$\n"
  FileWrite $0 "InstallDate=2024$\r$\n"
  FileClose $0
  
  ; Store installation info
  WriteRegStr HKCU "Software\HELIOS Platform" "InstallPath" "$INSTDIR"
  WriteRegStr HKCU "Software\HELIOS Platform" "Version" "1.0.0.0"
  
  ; Uninstaller registry
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\HELIOS Platform" "DisplayName" "HELIOS Platform"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\HELIOS Platform" "DisplayVersion" "1.0.0.0"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\HELIOS Platform" "UninstallString" "$\"$INSTDIR\Uninstall.exe$\""
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\HELIOS Platform" "InstallLocation" "$INSTDIR"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\HELIOS Platform" "Publisher" "HELIOS Solutions"
  
  ; Create uninstaller
  WriteUninstaller "$INSTDIR\Uninstall.exe"
  
  DetailPrint "HELIOS Platform Core installed successfully"
SectionEnd

; ============================================================================
; SECTION: START MENU SHORTCUTS
; ============================================================================

Section "Start Menu Shortcuts" SecStartMenu
  SectionIn 1 2 3
  
  DetailPrint "Creating Start Menu shortcuts..."
  
  CreateDirectory "$SMPROGRAMS\HELIOS Platform"
  CreateShortCut "$SMPROGRAMS\HELIOS Platform\HELIOS Platform.lnk" "$INSTDIR\HELIOS.Platform.exe"
  CreateShortCut "$SMPROGRAMS\HELIOS Platform\Uninstall.lnk" "$INSTDIR\Uninstall.exe"
  
  DetailPrint "Start Menu shortcuts created"
SectionEnd

; ============================================================================
; SECTION: DESKTOP SHORTCUTS
; ============================================================================

Section "Desktop Shortcut" SecDesktop
  SectionIn 1 2 3
  
  DetailPrint "Creating Desktop shortcuts..."
  
  CreateShortCut "$DESKTOP\HELIOS Platform.lnk" "$INSTDIR\HELIOS.Platform.exe"
  
  DetailPrint "Desktop shortcuts created"
SectionEnd

; ============================================================================
; SECTION: ADD TO PATH
; ============================================================================

Section "Add to PATH" SecPath
  SectionIn 1 2 3
  
  DetailPrint "Registering application in system PATH..."
  
  ; Read current PATH
  ReadRegStr $0 HKLM "SYSTEM\CurrentControlSet\Control\Session Manager\Environment" "Path"
  
  ; Check if path exists
  ${StrStr} $1 $0 "$INSTDIR"
  ${If} $1 == ""
    WriteRegExpandStr HKLM "SYSTEM\CurrentControlSet\Control\Session Manager\Environment" "Path" "$0;$INSTDIR"
  ${EndIf}
  
  DetailPrint "System PATH updated successfully"
SectionEnd

; ============================================================================
; SECTION DESCRIPTIONS
; ============================================================================

LangString DESC_SecCore ${LANG_ENGLISH} "HELIOS Platform core application and libraries (required)"
LangString DESC_SecStartMenu ${LANG_ENGLISH} "Create shortcuts in Windows Start Menu"
LangString DESC_SecDesktop ${LANG_ENGLISH} "Create shortcut on Desktop"
LangString DESC_SecPath ${LANG_ENGLISH} "Add HELIOS to system PATH environment variable"

!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
  !insertmacro MUI_DESCRIPTION_TEXT ${SecCore} $(DESC_SecCore)
  !insertmacro MUI_DESCRIPTION_TEXT ${SecStartMenu} $(DESC_SecStartMenu)
  !insertmacro MUI_DESCRIPTION_TEXT ${SecDesktop} $(DESC_SecDesktop)
  !insertmacro MUI_DESCRIPTION_TEXT ${SecPath} $(DESC_SecPath)
!insertmacro MUI_FUNCTION_DESCRIPTION_END

; ============================================================================
; UNINSTALLER
; ============================================================================

Section "Uninstall"
  DetailPrint "Uninstalling HELIOS Platform..."
  
  ; Remove registry entries
  DeleteRegKey HKCU "Software\HELIOS Platform"
  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\HELIOS Platform"
  
  ; Remove shortcuts
  RMDir /r "$SMPROGRAMS\HELIOS Platform"
  Delete "$DESKTOP\HELIOS Platform.lnk"
  
  ; Remove files
  RMDir /r "$INSTDIR"
  
  DetailPrint "HELIOS Platform uninstalled successfully"
SectionEnd

; ============================================================================
; INSTALLER FINALIZATION
; ============================================================================

Function .onInstSuccess
  MessageBox MB_ICONINFORMATION "HELIOS Platform has been installed successfully!$\n$\nInstallation Path: $INSTDIR$\n$\nYou can now run HELIOS Platform from your Start Menu or Desktop."
FunctionEnd

Function un.onUninstSuccess
  MessageBox MB_ICONINFORMATION "HELIOS Platform has been successfully removed from your computer."
FunctionEnd

; ============================================================================
; END OF INSTALLER SCRIPT
; ============================================================================
