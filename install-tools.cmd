@echo off
setlocal

set HERE=%~dp0

pushd %HERE%.full-build
%HERE%tools\paket.bootstrapper.exe || goto :failure
%HERE%tools\paket.exe install || goto :failure
popd

:ok
exit /b 0

:failure
exit /b 5

