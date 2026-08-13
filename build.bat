@echo off
setlocal
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
if not exist "%CSC%" (
  echo Compilateur C# introuvable ^(.NET Framework 4 requis^).
  exit /b 1
)

if not exist "%~dp0assets\app.ico" (
  echo assets\app.ico manquant : regenerez-le avec tools\MakeIcon.cs.
  exit /b 1
)

rem /codepage:65001 : les sources sont en UTF-8 sans BOM (chaines accentuees).
rem /win32icon    : icone de l'executable dans l'Explorateur et la barre des taches.
rem /resource     : logo embarque, l'exe reste utilisable sans le dossier assets.
"%CSC%" /nologo /target:winexe /platform:anycpu /optimize+ /codepage:65001 ^
  /out:"%~dp0KeepScreen.exe" ^
  /win32icon:"%~dp0assets\app.ico" ^
  /resource:"%~dp0assets\app.ico",KeepScreen.app.ico ^
  /resource:"%~dp0assets\icon.png",KeepScreen.logo.png ^
  /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll ^
  "%~dp0Program.cs" "%~dp0Native.cs" "%~dp0VirtualDesktop.cs" ^
  "%~dp0WindowPicker.cs" "%~dp0WindowMarker.cs" "%~dp0Assets.cs"

if errorlevel 1 (
  echo.
  echo Echec de la compilation.
  exit /b 1
)

echo.
echo OK : %~dp0KeepScreen.exe
endlocal
