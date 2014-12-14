@echo off
setlocal

call fb-init.bat

taskkill /im tgitcache.exe 1>NUL 2>NUL
if exist fullbuild-init rmdir /s /q fullbuild-init || goto :ko

rem setup environment
%FULLBUILD% set config binrepo c:\BinRepo
%FULLBUILD% set config repoType git
%FULLBUILD% set config repoUrl https://github.com/pchalamet/cassandra-sharp-full-build
%FULLBUILD% init workspace fullbuild-init || goto :ko

rem create workspace
pushd fullbuild-init

%FULLBUILD% add nuget https://www.nuget.org/api/v2/ || goto :ko
%FULLBUILD% add git repo full-build from https://github.com/pchalamet/full-build || goto :ko
%FULLBUILD% clone repo *  || goto :ko

:ok
echo *** SUCCESSFUL
exit /b 0

:ko
echo *** FAILURE
exit /b 0
