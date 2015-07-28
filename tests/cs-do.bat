@echo off
setlocal

call fb-init.bat

taskkill /im tgitcache.exe 1>NUL 2>NUL
if not exist cs-init call cs-init.bat || goto :ko
robocopy cs-init cs-do /MIR

pushd cs-do

%FULLBUILD% workspace convert || goto :ko

rem %FULLBUILD% index workspace || goto :ko
rem %FULLBUILD% install packages || goto :ko
rem %FULLBUILD% optimize workspace || goto :ko
rem copy ..\Template.csproj .full-build || goto :ko
rem %FULLBUILD% convert projects || goto :ko
rem %FULLBUILD% init view cs with cassandra-sharp cassandra-sharp-contrib || goto :ko
rem %FULLBUILD% generate view cs || goto :ko
rem %FULLBUILD% build view cs || goto :ko
rem %FULLBUILD% exec "echo %%FULLBUILD_REPO%% & git log -n 1 && echo." || goto :ko
rem %FULLBUILD% exec "git status" || goto :ko
rem %FULLBUILD% check packages || goto :ko
rem %FULLBUILD% use package moq version * || goto :ko
rem %FULLBUILD% check packages || goto :ko
rem %FULLBUILD% upgrade packages || goto :ko
rem %FULLBUILD% check packages || goto :ko
rem %FULLBUILD% list packages || goto :ko
rem %FULLBUILD% list nugets || goto :ko
rem %FULLBUILD% describe view cs || goto :ko
rem %FULLBUILD% list views || goto :ko
rem %FULLBUILD% drop view cs || goto :ko
rem %FULLBUILD% list views || goto :ko
rem %FULLBUILD% bookmark workspace || goto :ko


:ok
echo *** SUCCESSFUL
exit /b 0

:ko
echo *** FAILURE
exit /b 0