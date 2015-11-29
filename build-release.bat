echo on
setlocal
set PATH=C:\Program Files (x86)\MSBuild\14.0\Bin;%PATH%

bootstrap\fullbuild install package || goto :ko
bootstrap\fullbuild add view fullbuild * || goto :ko
bootstrap\fullbuild build --clean fullbuild || goto :ko
goto :ok

:ok
exit /b 0

:ko
exit /b 5
