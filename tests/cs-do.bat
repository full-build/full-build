rem @echo off
setlocal

taskkill /im tgitcache.exe 1>NUL 2>NUL
if not exist cs-init call cs-init.bat || goto :ko
robocopy cs-init cs-do /MIR /NP /NFL /NDL

pushd cs-do

fullbuild index workspace || goto :ko
copy ..\Template.csproj .full-build || goto :ko
fullbuild convert projects || goto :ko
fullbuild init view cs with cassandra-sharp cassandra-sharp-contrib || goto :ko
fullbuild generate view cs || goto :ko
fullbuild build view cs || goto :ko
fullbuild exec "echo %fullbuild_REPO% & git log -n 1 && echo." || goto :ko
fullbuild exec "git status" || goto :ko
fullbuild check packages || goto :ko
fullbuild use package moq version * || goto :ko
fullbuild check packages || goto :ko
fullbuild list packages || goto :ko
fullbuild list nugets || goto :ko


:ok
echo *** SUCCESSFUL
exit /b 0

:ko
echo *** FAILURE
exit /b 0