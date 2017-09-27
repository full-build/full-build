rem @echo off
setlocal

set HERE=%~dp0.

robocopy bootstrap\bin %HERE%\.full-build\bin /E
robocopy bootstrap\views .full-build\views /E
robocopy bootstrap\projects .full-build\projects /E
copy bootstrap\bootstrap.sln %HERE%

msbuild /t:Restore /p:SolutionDir="%HERE%" /p:SolutionName="bootstrap" bootstrap.sln || goto :ko
msbuild /t:Rebuild bootstrap.sln || goto :ko

echo publishing with built version
%HERE%\src\fullbuild\bin\debug\net452\fullbuild publish --version 0.0.0 full-build || goto :ko
robocopy %HERE%\apps\full-build %HERE%\refbin /MIR
rmdir /s /q %HERE%\apps
verify > nul

:ok
exit /b 0

:ko
exit /b 5

