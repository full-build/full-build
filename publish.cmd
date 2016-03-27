@echo off
setlocal

set HERE=%~dp0

call :dopublish || goto :ko
goto :ok

:dopublish
%HERE%\dotnet\fullbuild\bin\fullbuild publish * || goto :ko
robocopy %HERE%\apps\full-build %HERE%\refbin /MIR
verify >nul
goto :eof

:ok
exit /b 0

:ko
exit /b 5
