@echo off
setlocal

set VERSION=%1
set VERSTATUS=%2
if [%VERSION%] == [] set VERSION=0.0.0
echo publishing version '%VERSION%' with status '%VERSTATUS%'

set HERE=%~dp0

call :dopublish || goto :ko
goto :ok

:dopublish
%HERE%\src\fullbuild\bin\fullbuild publish * || goto :ko
robocopy %HERE%\apps\full-build %HERE%\refbin /MIR

set NUGETVERSION=%VERSION%%VERSTATUS%
nuget pack -NoDefaultExcludes -NoPackageAnalysis -NonInteractive -OutputDirectory apps -Version %NUGETVERSION% full-build.nuspec || goto :ko
move apps\full-build.%NUGETVERSION%.nupkg apps\full-build.nupkg || goto :ko
verify >nul
goto :eof

:ok
exit /b 0

:ko
exit /b 5
