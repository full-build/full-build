echo on
setlocal

set VERSION=%1
if [%VERSION%] == [] set VERSION=999.0.0
echo building version %VERSION%
set VERSION=%VERSION%

set HERE=%~dp0
set PATH=%VSINSTALLDIR%\MSBuild\%VisualStudioVersion%\Bin;%PATH%

call :dobuild || goto :ko
goto :ok

:dobuild
refbin\fullbuild install || goto :ko
refbin\fullbuild view fullbuild src/* || goto :ko
refbin\fullbuild rebuild --version %VERSION% fullbuild || goto :ko
goto :eof

:ok
exit /b 0

:ko
exit /b 5
