@echo off
setlocal

call fb-init.bat

taskkill /im tgitcache.exe 1>NUL 2>NUL
if exist rc-init rmdir /s /q rc-init || goto :ko
mkdir rc-init || goto :ko

rem setup environment
%FULLBUILD% init workspace rc-init from git https://github.com/pchalamet/cassandra-sharp-full-build || goto :ko

rem create workspace
pushd rc-init

%FULLBUILD% add nuget https://www.nuget.org/api/v2/ || goto :ko
%FULLBUILD% add git repo fcpl from https://github.com/VesaKarvonen/Recalled.git || goto :ko
%FULLBUILD% clone repo *  || goto :ko

:ok
echo *** SUCCESSFUL
exit /b 0

:ko
echo *** FAILURE
exit /b 0
