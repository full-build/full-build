@setlocal
set HERE=%~dp0

set fullbuild=%HERE%\..\src\bin\debug\FullBuild.exe

@taskkill /im tgitcache.exe
@robocopy titi toto /MIR
@robocopy src\.nuget toto\.nuget /MIR
@pushd toto

%FULLBUILD% index workspace || goto :ko
copy ..\Template.csproj .full-build
%FULLBUILD% convert projects || goto :ko
%FULLBUILD% init view cs with cassandra-sharp cassandra-sharp-contrib || goto :ko
%FULLBUILD% generate view cs || goto :ko
%FULLBUILD% build view cs
%FULLBUILD% exec "echo %%FULLBUILD_REPO%% & git log -n 1 | find ""commit"" && echo." || goto :ko
%FULLBUILD% exec "git status" || got :ko

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
