@echo off
setlocal

call fb-init.bat

taskkill /im tgitcache.exe 1>NUL 2>NUL
if exist cs-init rmdir /s /q cs-init || goto :ko

rem setup environment
%FULLBUILD% init workspace cs-init from git https://github.com/pchalamet/cassandra-sharp-full-build || goto :ko

rem create workspace
pushd cs-init

%FULLBUILD% add nuget https://www.nuget.org/api/v2/ || goto :ko
%FULLBUILD% add git repo cassandra-sharp from https://github.com/pchalamet/cassandra-sharp || goto :ko
%FULLBUILD% add git repo cassandra-sharp-contrib from https://github.com/pchalamet/cassandra-sharp-contrib || goto :ko
%FULLBUILD% clone repo *  || goto :ko

:ok
echo *** SUCCESSFUL
exit /b 0

:ko
echo *** FAILURE
exit /b 0
