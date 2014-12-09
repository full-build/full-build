rem @echo off
setlocal

taskkill /im tgitcache.exe 1>NUL 2>NUL
if not exist cs-error-init call cs-error-init.bat || goto :ko
robocopy cs-error-init cs-error-do /MIR

pushd cs-error-do

fullbuild index workspace || goto :ko
copy ..\Template.csproj .full-build || goto :ko
fullbuild convert projects || goto :ko

:ok
echo *** SUCCESSFUL
exit /b 0

:ko
echo *** FAILURE
exit /b 0