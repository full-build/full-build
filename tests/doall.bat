@setlocal

set PATH=%PATH%;c:\dev\projects\full-build\src\bin\Debug

@taskkill /im tgitcache.exe
@robocopy titi toto /MIR
@robocopy src\.nuget toto\.nuget /MIR
@pushd toto

@echo ************************************************************************************
FullBuild update workspace || goto :ko

rem copy ..\Template.csproj .full-build

@echo ************************************************************************************
FullBuild convert projects || goto :ko

@echo ************************************************************************************
..\..\src\bin\Debug\FullBuild init view cs with cassandra-sharp  cassandra-sharp-contrib || goto :ko
FullBuild update view cs || goto :ko

msbuild cs.sln || goto :ko

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
