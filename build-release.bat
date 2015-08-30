set PATH=C:\Program Files (x86)\MSBuild\14.0\Bin;%PATH%

bootstrap\fullbuild package install
bootstrap\fullbuild view create all using *
msbuild /p:Configuration=Release *.sln || goto :ko

:ok
exit /b 0

:ko
exit /b 5
