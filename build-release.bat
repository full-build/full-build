pushd src

.nuget\nuget restore || goto :ko
msbuild /p:Configuration=Release full-build.sln || goto :ko

:ok
popd
exit /b 0

:ko
popd
exit /b 5
