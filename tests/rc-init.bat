@echo off
setlocal

taskkill /im tgitcache.exe 1>NUL 2>NUL
if exist rc-init rmdir /s /q rc-init || goto :ko
mkdir rc-init || goto :ko

rem setup environment
fullbuild set config PackageGlobalCache c:\PackageGlobalCache || goto :ko
fullbuild set config RepoType Git || goto :ko
fullbuild set config RepoUrl https://github.com/pchalamet/cassandra-sharp-full-build || goto :ko
fullbuild init workspace rc-init || goto :ko

rem create workspace
pushd rc-init

fullbuild set binrepo c:\BinRepo || goto :ko
fullbuild add nuget https://www.nuget.org/api/v2/ || goto :ko
fullbuild add git repo fcpl from https://github.com/VesaKarvonen/Recalled.git || goto :ko
fullbuild clone repo *  || goto :ko

:ok
echo *** SUCCESSFUL
exit /b 0

:ko
echo *** FAILURE
exit /b 0
