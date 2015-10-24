@echo off
setlocal

call fb-init.bat

taskkill /im tgitcache.exe 1>NUL 2>NUL
if not exist cs-init call cs-init.bat || goto :ko
robocopy cs-init cs-do /MIR 

pushd cs-do
%FULLBUILD% clone * || goto :ko
%FULLBUILD% index || goto :ko
%FULLBUILD% convert || goto :ko
%FULLBUILD% add view all * || goto :ko
%FULLBUILD% add view csc cassandra-sharp-contrib/* || goto :ko
%FULLBUILD% list view || goto :ko
%FULLBUILD% describe view all || goto :ko
%FULLBUILD% graph all || goto :ko
%FULLBUILD% build all || goto :ko
%FULLBUILD% drop view csc || goto :ko
%FULLBUILD% outdated || goto :ko
%FULLBUILD% update || goto :ko
%FULLBUILD% push || goto :ko


:ok
echo *** SUCCESSFUL
exit /b 0

:ko
echo *** FAILURE
exit /b 0
