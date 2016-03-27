@echo off
setlocal

set HERE=%~dp0

pushd %HERE%\.full-build
%HERE%\tools\paket.exe install || goto :failure
popd
robocopy bootstrap\bin %HERE%\.full-build\bin /E
robocopy bootstrap\views .full-build\views /E
robocopy bootstrap\projects .full-build\projects /E
robocopy bootstrap\packages .full-build\packages /E
copy bootstrap\bootstrap.sln %HERE%

msbuild /t:Rebuild bootstrap.sln || goto :failure
call publish.cmd || goto :failure

:ok
exit /b 0

:failure
exit /b 5

