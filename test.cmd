echo on
setlocal

set PATH=C:\Program Files (x86)\MSBuild\14.0\Bin;%PATH%
set HERE=%~dp0


refbin\fullbuild test * || goto :ko
goto :ok

:ok
exit /b 0

:ko
exit /b 5
