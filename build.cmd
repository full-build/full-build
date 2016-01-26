echo on
setlocal

set VERSION=%1
set VERSION=%1
if [%VERSION%] == [] set VERSION=0.0.0.* 
echo building version %VERSION%

set PATH=C:\Program Files (x86)\MSBuild\14.0\Bin;%PATH%
set HERE=%~dp0


bootstrap\fullbuild install package || goto :ko
bootstrap\fullbuild add view fullbuild * || goto :ko
bootstrap\fullbuild rebuild --version %VERSION% fullbuild || goto :ko
goto :ok

:ok
exit /b 0

:ko
exit /b 5
