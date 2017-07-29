rem build twice - this is ok:
rem 1/ first time is to bootstrap the build
rem 2/ second time is to build using new version (ie: self build)
rem both build ensure compatibility on version update

@echo off
setlocal

set BUILD_VERSION=%1
set BUILD_STATUS=%2

if "%VSINSTALLDIR%" == "" call "C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\Common7\Tools\VsDevCmd.bat"

call install-tools.cmd || goto :ko
call build-bootstrap.cmd || goto :ko
call build-src.cmd %BUILD_VERSION% || goto :ko
call publish.cmd %BUILD_VERSION% %BUILD_STATUS% || goto :ko
call run-tests.cmd || goto :ko
goto :ok

:ok
cd %HERE%
echo *** BUILD SUCCESSFUL ***
exit /b 0

:ko
echo *** BUILD FAILURE ***
cd %HERE%
exit /b 5
