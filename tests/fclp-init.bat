@echo off
setlocal

taskkill /im tgitcache.exe 1>NUL 2>NUL
if exist fclp-init rmdir /s /q fclp-init || goto :ko
mkdir fclp-init || goto :ko

rem setup environment
fullbuild set config PackageGlobalCache c:\PackageGlobalCache || goto :ko
fullbuild set config RepoType Git || goto :ko
fullbuild set config RepoUrl https://github.com/pchalamet/cassandra-sharp-full-build || goto :ko
fullbuild init workspace fclp-init || goto :ko

rem create workspace
pushd fclp-init

fullbuild set binrepo c:\BinRepo || goto :ko
fullbuild add nuget https://www.nuget.org/api/v2/ || goto :ko
fullbuild add git repo fcpl from https://github.com/fclp/fluent-command-line-parser.git || goto :ko
fullbuild clone repo *  || goto :ko

:ok
echo *** SUCCESSFUL
exit /b 0

:ko
echo *** FAILURE
exit /b 0
