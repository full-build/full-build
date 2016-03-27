@echo off
setlocal

set HERE=%~dp0

pushd %HERE%\.full-build
%HERE%\bootstrap\paket.exe install || goto :failure
popd
robocopy bootstrap\bin %HERE%\.full-build\bin /MIR
robocopy bootstrap\views .full-build\views /MIR
robocopy bootstrap\projects .full-build\projects /MIR
robocopy bootstrap\packages .full-build\packages /MIR
copy bootstrap\bootstrap.sln %HERE%

msbuild /t:Rebuild bootstrap.sln || goto :failure

:ok
exit /b 0

:failure
exit /b 5

