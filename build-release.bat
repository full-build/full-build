echo on
set PATH=C:\Program Files (x86)\MSBuild\14.0\Bin;%PATH%

bootstrap\fullbuild install || goto :ko
bootstrap\fullbuild add view all * || goto :ko
bootstrap\fullbuild build all

:ok
exit /b 0

:ko
exit /b 5
