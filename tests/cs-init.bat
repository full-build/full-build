rem @echo off
setlocal

call fb-init.bat

taskkill /im tgitcache.exe 1>NUL 2>NUL
if exist cs-init rmdir /s /q cs-init || goto :ko

%FULLBUILD% setup git https://github.com/pchalamet/cassandra-sharp-full-build c:\BinRepo cs-init

rem create workspace
pushd cs-init

%FULLBUILD% add nuget https://www.nuget.org/api/v2/ || goto :ko
%FULLBUILD% add repo cassandra-sharp msbuild https://github.com/pchalamet/cassandra-sharp || goto :ko
%FULLBUILD% add repo cassandra-sharp-contrib msbuild https://github.com/pchalamet/cassandra-sharp-contrib || goto :ko
%FULLBUILD% clone *  || goto :ko

:ok
echo *** SUCCESSFUL
exit /b 0

:ko
echo *** FAILURE
exit /b 0
