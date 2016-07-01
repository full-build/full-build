@echo off
setlocal

set VERSION=%1
if [%VERSION%] == [] set VERSION=0.0.0.0 
echo publishing version %VERSION%

set HERE=%~dp0

call :dopublish || goto :ko
goto :ok

:dopublish
%HERE%\src\fullbuild\bin\fullbuild publish * || goto :ko
robocopy %HERE%\apps\full-build %HERE%\refbin /MIR
nuget pack -NoDefaultExcludes -NoPackageAnalysis -NonInteractive -OutputDirectory apps -Version %VERSION% full-build.nuspec
verify >nul
goto :eof

:ok
exit /b 0

:ko
exit /b 5
