@echo off
setlocal

taskkill /im tgitcache.exe 1>NUL 2>NUL
robocopy titi toto /MIR /NP /NFL /NDL /NJH /NJS

pushd toto

fullbuild index workspace || goto :ko
copy ..\Template.csproj .full-build || goto :ko
fullbuild convert projects || goto :ko
fullbuild init view cs with cassandra-sharp cassandra-sharp-contrib || goto :ko
fullbuild generate view cs || goto :ko
fullbuild build view cs || goto :ko
fullbuild exec "echo %fullbuild_REPO% & git log -n 1 && echo." || goto :ko
fullbuild exec "git status" || goto :ko

:ok
echo *** SUCCESSFUL
exit /b 0

:ko
echo *** FAILURE
exit /b 0