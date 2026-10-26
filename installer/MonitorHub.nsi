; Instalador do Monitor Hub (NSIS) - instala só para o usuário atual, sem pedir administrador
Unicode true
!include "MUI2.nsh"

!define APP "Monitor Hub"
!define EXE "MonitorHub.exe"
!ifndef VERSION
  !define VERSION "1.0.0"
!endif
!define UNINST "Software\Microsoft\Windows\CurrentVersion\Uninstall\MonitorHub"

Name "${APP}"
OutFile "MonitorHub-Setup-${VERSION}.exe"
InstallDir "$LOCALAPPDATA\Programs\MonitorHub"
RequestExecutionLevel user
SetCompressor /SOLID lzma
BrandingText "${APP} ${VERSION}"

VIProductVersion "${VERSION}.0"
VIAddVersionKey "ProductName" "${APP}"
VIAddVersionKey "FileDescription" "${APP} Setup"
VIAddVersionKey "FileVersion" "${VERSION}"
VIAddVersionKey "ProductVersion" "${VERSION}"
VIAddVersionKey "LegalCopyright" "Open source"

!define MUI_ICON "..\icon.ico"
!define MUI_UNICON "..\icon.ico"
!define MUI_WELCOMEFINISHPAGE_BITMAP "sidebar.bmp"
!define MUI_UNWELCOMEFINISHPAGE_BITMAP "sidebar.bmp"
!define MUI_HEADERIMAGE
!define MUI_HEADERIMAGE_RIGHT
!define MUI_HEADERIMAGE_BITMAP "header.bmp"
!define MUI_ABORTWARNING
!define MUI_FINISHPAGE_RUN "$INSTDIR\${EXE}"

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "PortugueseBR"
!insertmacro MUI_LANGUAGE "English"
!insertmacro MUI_LANGUAGE "Spanish"
!insertmacro MUI_LANGUAGE "French"
!insertmacro MUI_LANGUAGE "German"

Function .onInit
  !insertmacro MUI_LANGDLL_DISPLAY
FunctionEnd

Section "Install"
  ; fecha a versão aberta, se houver, para poder substituir o exe
  nsExec::Exec 'taskkill /IM ${EXE} /F'
  Sleep 500
  SetOutPath "$INSTDIR"
  File "..\${EXE}"
  WriteUninstaller "$INSTDIR\Uninstall.exe"

  CreateShortcut "$SMPROGRAMS\${APP}.lnk" "$INSTDIR\${EXE}"
  CreateShortcut "$DESKTOP\${APP}.lnk" "$INSTDIR\${EXE}"

  WriteRegStr HKCU "${UNINST}" "DisplayName" "${APP}"
  WriteRegStr HKCU "${UNINST}" "DisplayVersion" "${VERSION}"
  WriteRegStr HKCU "${UNINST}" "DisplayIcon" "$INSTDIR\${EXE}"
  WriteRegStr HKCU "${UNINST}" "Publisher" "Monitor Hub"
  WriteRegStr HKCU "${UNINST}" "InstallLocation" "$INSTDIR"
  WriteRegStr HKCU "${UNINST}" "UninstallString" '"$INSTDIR\Uninstall.exe"'
  WriteRegDWORD HKCU "${UNINST}" "NoModify" 1
  WriteRegDWORD HKCU "${UNINST}" "NoRepair" 1
  WriteRegDWORD HKCU "${UNINST}" "EstimatedSize" 300
SectionEnd

Section "Uninstall"
  nsExec::Exec 'taskkill /IM ${EXE} /F'
  Sleep 500
  Delete "$INSTDIR\${EXE}"
  Delete "$INSTDIR\Uninstall.exe"
  RMDir "$INSTDIR"
  Delete "$SMPROGRAMS\${APP}.lnk"
  Delete "$DESKTOP\${APP}.lnk"
  DeleteRegValue HKCU "Software\Microsoft\Windows\CurrentVersion\Run" "MonitorHub"
  DeleteRegKey HKCU "${UNINST}"
SectionEnd
