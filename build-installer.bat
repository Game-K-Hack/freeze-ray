@echo off
setlocal
rem Compile l'application, puis l'installeur qui l'embarque.

call "%~dp0build.bat"
if errorlevel 1 exit /b 1

set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe

echo.
echo Compilation de l'installeur...

rem L'application et la licence sont embarquees comme ressources : l'installeur
rem est un fichier unique, rien d'autre a distribuer.
"%CSC%" /nologo /target:winexe /platform:anycpu /optimize+ /codepage:65001 ^
  /out:"%~dp0Freeze Ray Setup.exe" ^
  /win32icon:"%~dp0assets\app.ico" ^
  /resource:"%~dp0Freeze Ray.exe",FreezeRay.app.exe ^
  /resource:"%~dp0LICENSE.md",FreezeRay.license.md ^
  /resource:"%~dp0assets\app.ico",FreezeRay.app.ico ^
  /resource:"%~dp0assets\icon.png",FreezeRay.logo.png ^
  /resource:"%~dp0assets\Freeze Ray.png",FreezeRay.banner.png ^
  /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll ^
  "%~dp0installer\Setup.cs" "%~dp0installer\Shortcuts.cs" ^
  "%~dp0InstallPaths.cs" "%~dp0Strings.cs" "%~dp0Assets.cs" "%~dp0Updater.cs" ^
  "%~dp0AssemblyInfo.cs"

if errorlevel 1 (
  echo.
  echo Echec de la compilation de l'installeur.
  exit /b 1
)

echo.
echo OK : %~dp0Freeze Ray Setup.exe
endlocal
