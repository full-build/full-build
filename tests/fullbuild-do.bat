@echo off
setlocal

call fb-init.bat

taskkill /im tgitcache.exe 1>NUL 2>NUL
if not exist fullbuild-init call fullbuild-init.bat || goto :ko
robocopy fullbuild-init fullbuild-do /MIR

pushd fullbuild-do

%FULLBUILD% index workspace || goto :ko
copy ..\Template.csproj .full-build || goto :ko
%FULLBUILD% convert projects || goto :ko
%FULLBUILD% init view fb with * || goto :ko
%FULLBUILD% generate view fb || goto :ko
%FULLBUILD% build view fb || goto :ko

:ok
echo *** SUCCESSFUL
exit /b 0

:ko
echo *** FAILURE
exit /b 0