@echo off
setlocal

set HERE=%~dp0

robocopy bootstrap\bin %HERE%\.full-build\bin /E
robocopy bootstrap\views .full-build\views /E
robocopy bootstrap\projects .full-build\projects /E
robocopy bootstrap\packages .full-build\packages /E
copy bootstrap\bootstrap.sln %HERE%

msbuild /t:Rebuild bootstrap.sln || goto :ko
call publish.cmd || goto :ko

:ok
exit /b 0

:ko
exit /b 5

