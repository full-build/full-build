rem @echo off
setlocal

call fb-init.bat

taskkill /im tgitcache.exe 1>NUL 2>NUL
if not exist cs-init call cs-init.bat || goto :ko
robocopy cs-init cs-do /MIR

pushd cs-do

%FULLBUILD% index workspace || goto :ko
%FULLBUILD% install packages || goto :ko
%FULLBUILD% optimize workspace || goto :ko
copy ..\Template.csproj .full-build || goto :ko
%FULLBUILD% convert projects || goto :ko
%FULLBUILD% init view cs with cassandra-sharp cassandra-sharp-contrib || goto :ko
%FULLBUILD% generate view cs || goto :ko
%FULLBUILD% build view cs || goto :ko
%FULLBUILD% exec "echo %%FULLBUILD_REPO%% & git log -n 1 && echo." || goto :ko
%FULLBUILD% exec "git status" || goto :ko
%FULLBUILD% check packages || goto :ko
%FULLBUILD% use package moq version * || goto :ko
%FULLBUILD% check packages || goto :ko
%FULLBUILD% list packages || goto :ko
%FULLBUILD% list nugets || goto :ko
%FULLBUILD% describe view cs || goto :ko
%FULLBUILD% list views || goto :ko
%FULLBUILD% drop view cs || goto :ko
%FULLBUILD% list views || goto :ko
%FULLBUILD% bookmark workspace || goto :ko


:ok
echo *** SUCCESSFUL
exit /b 0

:ko
echo *** FAILURE
exit /b 0