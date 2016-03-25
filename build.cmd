echo on
setlocal

set VERSION=%1
if [%VERSION%] == [] set VERSION=0.0.0.* 
echo building version %VERSION%

set PATH=C:\Program Files (x86)\MSBuild\14.0\Bin;%PATH%
set HERE=%~dp0

call :dobuild || goto :ko
goto :ok

:dobuild
bootstrap\fullbuild install || goto :ko
bootstrap\fullbuild view all * || goto :ko
bootstrap\fullbuild rebuild --version %VERSION% all || goto :ko
goto :eof

:ok
exit /b 0

:ko
exit /b 5
