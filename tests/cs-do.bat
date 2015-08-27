@echo off
setlocal

call fb-init.bat

taskkill /im tgitcache.exe 1>NUL 2>NUL
if not exist cs-init call cs-init.bat || goto :ko
robocopy cs-init cs-do /MIR 

pushd cs-do
robocopy c:\src\Paket\bin .paket /MIR


%FULLBUILD% workspace index || goto :ko
%FULLBUILD% package install || goto :ko
%FULLBUILD% package simplify || goto :ko
%FULLBUILD% workspace convert || goto :ko
%FULLBUILD% view create cs using cassandra-sharp || goto :ko
%FULLBUILD% view create csc using cassandra-sharp-contrib || goto :ko
%FULLBUILD% view create all using * || goto :ko
%FULLBUILD% view list || goto :ko
%FULLBUILD% view describe cs || goto :ko
%FULLBUILD% view describe csc || goto :ko
%FULLBUILD% view describe all || goto :ko
%FULLBUILD% view graph all || goto :ko




rem msbuild csc.sln
rem msbuild cs.sln

rem %FULLBUILD% view drop cs || goto :ko
rem %FULLBUILD% view list || goto :ko

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
