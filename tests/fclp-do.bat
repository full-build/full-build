@echo off
setlocal

call fb-init.bat

taskkill /im tgitcache.exe 1>NUL 2>NUL
if not exist fclp-init call fclp-init.bat || goto :ko
robocopy fclp-init fclp-do /MIR

pushd fclp-do

%FULLBUILD% index workspace || goto :ko
copy ..\Template.csproj .full-build || goto :ko
%FULLBUILD% convert projects || goto :ko
%FULLBUILD% init view cs with * || goto :ko
%FULLBUILD% generate view cs || goto :ko
%FULLBUILD% build view cs || goto :ko
%FULLBUILD% exec "echo %%FULLBUILD_REPO%% & git log -n 1 && echo." || goto :ko
%FULLBUILD% exec "git status" || goto :ko
%FULLBUILD% check packages || goto :ko
%FULLBUILD% list packages || goto :ko
%FULLBUILD% list nugets || goto :ko

:ok
echo *** SUCCESSFUL
exit /b 0

:ko
echo *** FAILURE
exit /b 0