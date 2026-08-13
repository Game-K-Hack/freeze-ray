@echo off
setlocal
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
if not exist "%CSC%" (
  echo Compilateur C# introuvable ^(.NET Framework 4 requis^).
  exit /b 1
)

rem /codepage:65001 : les sources sont en UTF-8 sans BOM (chaines accentuees).
"%CSC%" /nologo /target:winexe /platform:anycpu /optimize+ /codepage:65001 ^
  /out:"%~dp0KeepScreen.exe" ^
  /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll ^
  "%~dp0Program.cs" "%~dp0Native.cs" "%~dp0VirtualDesktop.cs" "%~dp0WindowPicker.cs"

if errorlevel 1 (
  echo.
  echo Echec de la compilation.
  exit /b 1
)

echo.
echo OK : %~dp0KeepScreen.exe
endlocal
