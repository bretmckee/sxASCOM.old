;
; Script generated by the ASCOM Driver Installer Script Generator 1.3.0.0
; Generated by Bret McKee on 5/7/2010 (UTC)
;

#define BUILD_TYPE "Debug"
#define APP_VERSION "1.3.3"
#define ASCOM_VERSION_REQUIRED  "5.5"

[Setup]
AppName=ASCOM SX Camera Driver
AppVerName=ASCOM SX Camera Driver {#APP_VERSION}
AppPublisher=Bret McKee <bretm@daddog.com>
AppPublisherURL=http://www.daddog.com/ascom/sx/index.html
AppVersion={#APP_VERSION}
AppSupportURL=http://tech.groups.yahoo.com/group/ASCOM-Talk/
AppUpdatesURL=http://ascom-standards.org/
VersionInfoVersion="{#APP_VERSION}"
MinVersion=0,5.0.2195sp4
DefaultDirName="{cf}\ASCOM\Camera"
DisableDirPage=yes
DisableProgramGroupPage=yes
OutputDir="."
OutputBaseFilename="SXAscomInstaller-v{#APP_VERSION}"
Compression=lzma
SolidCompression=yes
; Put there by Platform if Driver Installer Support selected
WizardImageFile="C:\Program Files (x86)\ASCOM\InstallGen\Resources\WizardImage.bmp"
LicenseFile="C:\Users\bretm\Astronomy\src\sxASCOM\CreativeCommons.txt"

; {cf}\ASCOM\Uninstall\Camera folder created by Platform, always
UninstallFilesDir="{cf}\ASCOM\Uninstall\Camera\SX"

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Dirs]
Name: "{cf}\ASCOM\Uninstall\Camera\SX"
Name: "{app}\SXCamera"
Name: "{app}\SXCamera\SXCameraServedClasses"

[Files]
Source: "SXCamera\bin\{#BUILD_TYPE}\ASCOM.SXCamera.exe"; DestDir: "{app}"
Source: "ASCOM.SXCamera.exe.config"; DestDir: "{app}"
; TODO: Add driver assemblies into the ServedClasses folder
Source: "Main\bin\{#BUILD_TYPE}\ASCOM.SXMain.Camera.dll"; DestDir: "{app}\SXCamera\SXCameraServedClasses"
Source: "Guide\bin\{#BUILD_TYPE}\ASCOM.SXGuide.Camera.dll"; DestDir: "{app}\SXCamera\SXCameraServedClasses"
Source: "Generic\bin\{#BUILD_TYPE}\ASCOM.SXGeneric.Camera.dll"; DestDir: "{app}\SXCamera\SXCameraServedClasses"
Source: "CreativeCommons.html"; DestDir: "{app}\SXCamera" ; Flags: isreadme
Source: "CreativeCommons.html"; DestDir: "{app}\SXCamera\SXCameraServedClasses"
; Require a read-me HTML to appear after installation, maybe driver's Help doc
Source: "SXCamera.Readme.txt"; DestDir: "{app}\SXCamera"; Flags: isreadme
Source: "SXCamera.Readme.txt"; DestDir: "{app}\SXCamera\SXCameraServedClasses"
; TODO: Add other files needed by your driver here (add subfolders above)
Source: "UsbRoutines\bin\{#BUILD_TYPE}\ASCOM.SXCamera.UsbRoutines.dll"; DestDir: "{app}\SXCamera"
Source: "Logging\bin\{#BUILD_TYPE}\ASCOM.SXLogging.dll"; DestDir: "{app}\SXCamera"


; Only if driver is .NET
[Run]
; Only for .NET local-server drivers
Filename: "{app}\ASCOM.SXCamera.exe"; Parameters: "/register"

; Only if driver is .NET
[UninstallRun]
; Only for .NET local-server drivers
Filename: "{app}\ASCOM.SXCamera.exe"; Parameters: "/unregister"



[CODE]
//
// Before the installer UI appears, verify that the (prerequisite)
// ASCOM Platform 5 or greater is installed, including both Helper
// components. Helper is required for all types (COM and .NET)!
//
function InitializeSetup(): Boolean;
var
   H : Variant;
   H2 : Variant;
begin
   Result := FALSE;  // Assume failure
   try               // Will catch all errors including missing reg data
      H := CreateOLEObject('DriverHelper.Util');  // Assure both are available
      H2 := CreateOleObject('DriverHelper2.Util');
      if (H2.PlatformVersion >= {#ASCOM_VERSION_REQUIRED}) then
         Result := TRUE;
   except
   end;
   if(not Result) then
      MsgBox('The ASCOM Platform {#ASCOM_VERSION_REQUIRED} or greater is required for this driver.', mbInformation, MB_OK);
end;

