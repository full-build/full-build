echo on
set PATH=C:\Program Files (x86)\MSBuild\14.0\Bin;%PATH%

bootstrap\fullbuild install
bootstrap\fullbuild add view all using *
msbuild /p:Configuration=Release all.sln || goto :ko

:ok
exit /b 0

:ko
exit /b 5
