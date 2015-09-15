@echo off
setlocal

call fb-init.bat

taskkill /im tgitcache.exe 1>NUL 2>NUL
if not exist cs-init3 call cs-init3.bat || goto :ko
robocopy cs-init3 paket-do /MIR 

pushd paket-do
rem %FULLBUILD% add repo git paket https://github.com/fsprojects/Paket.git
rem %FULLBUILD% clone paket
%FULLBUILD% convert || goto :ko
%FULLBUILD% add view all * || goto :ko
%FULLBUILD% build all || goto :ko

:ok
echo *** SUCCESSFUL
exit /b 0

:ko
echo *** FAILURE
exit /b 0
