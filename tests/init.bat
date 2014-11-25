@echo off
setlocal

taskkill /im tgitcache.exe 1>NUL 2>NUL
if exist titi rmdir /s /q titi || goto :ko
mkdir titi || goto :ko

rem setup environment
fullbuild set config PackageGlobalCache c:\PackageGlobalCache || goto :ko
fullbuild set config RepoType Git || goto :ko
fullbuild set config RepoUrl https://github.com/pchalamet/cassandra-sharp-full-build || goto :ko
fullbuild init workspace titi || goto :ko

rem create workspace
pushd titi

fullbuild add git repo cassandra-sharp from https://github.com/pchalamet/cassandra-sharp || goto :ko
fullbuild add git repo cassandra-sharp-contrib from https://github.com/pchalamet/cassandra-sharp-contrib || goto :ko
fullbuild clone repo *  || goto :ko

:ok
echo *** SUCCESSFUL
exit /b 0

:ko
echo *** FAILURE
exit /b 0
