pushd src

set PATH=C:\Program Files (x86)\MSBuild\14.0\Bin;%PATH%

msbuild /p:Configuration=Release full-build.sln || goto :ko

:ok
popd
exit /b 0

:ko
popd
exit /b 5
