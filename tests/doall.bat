@setlocal

set PATH=%PATH%;c:\dev\projects\full-build\src\bin\Debug

@taskkill /im tgitcache.exe
@robocopy titi toto /MIR
@robocopy src\.nuget toto\.nuget /MIR
@pushd toto

FullBuild index workspace || goto :ko
copy ..\Template.csproj .full-build
FullBuild convert projects || goto :ko
FullBuild init view cs with cassandra-sharp cassandra-sharp-contrib || goto :ko
FullBuild generate view cs || goto :ko
FullBuild build view cs
fullbuild exec "echo %%FULLBUILD_REPO%% & git log -n 1 | find ""commit"" && echo." || goto :ko

popd

:ok
@echo *********************************************
@echo SUCCESS
@echo *********************************************
exit /b 0

:ko
@echo *********************************************
@echo FAILURE
@echo *********************************************
popd
@exit /b 5
