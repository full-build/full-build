@echo off
setlocal

call fb-init.bat

taskkill /im tgitcache.exe 1>NUL 2>NUL
if exist fclp-init rmdir /s /q fclp-init || goto :ko
mkdir fclp-init || goto :ko

rem setup environment
%FULLBUILD% init workspace fclp-init from git https://github.com/pchalamet/cassandra-sharp-full-build || goto :ko

rem create workspace
pushd fclp-init

%FULLBUILD% add nuget https://www.nuget.org/api/v2/ || goto :ko
%FULLBUILD% add git repo fcpl from https://github.com/fclp/fluent-command-line-parser.git || goto :ko
%FULLBUILD% clone repo *  || goto :ko

:ok
echo *** SUCCESSFUL
exit /b 0

:ko
echo *** FAILURE
exit /b 0
