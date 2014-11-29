@echo off
setlocal

taskkill /im tgitcache.exe 1>NUL 2>NUL
if exist cs-init rmdir /s /q cs-init || goto :ko
mkdir cs-init || goto :ko

rem setup environment
fullbuild set config PackageGlobalCache c:\PackageGlobalCache || goto :ko
fullbuild set config RepoType Git || goto :ko
fullbuild set config RepoUrl https://github.com/pchalamet/cassandra-sharp-full-build || goto :ko
fullbuild init workspace cs-init || goto :ko

rem create workspace
pushd cs-init

fullbuild set binrepo c:\BinRepo || goto :ko
fullbuild add nuget https://www.nuget.org/api/v2/ || goto :ko
fullbuild add git repo cassandra-sharp from https://github.com/pchalamet/cassandra-sharp || goto :ko
fullbuild add git repo cassandra-sharp-contrib from https://github.com/pchalamet/cassandra-sharp-contrib || goto :ko
fullbuild clone repo *  || goto :ko

:ok
echo *** SUCCESSFUL
exit /b 0

:ko
echo *** FAILURE
exit /b 0
