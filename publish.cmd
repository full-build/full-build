@echo off
setlocal

set VERSION=%1
set VERSTATUS=%2
if [%VERSION%] == [] set VERSION=0.0.0
if [%VERSTATUS%] neq [] set VERSTATUS=-%VERSTATUS%

echo publishing version '%VERSION%' with status '%VERSTATUS%'

set HERE=%~dp0

call :dopublish || goto :ko
goto :ok

:dopublish
%HERE%\src\fullbuild\bin\fullbuild publish --version %VERSION%%VERSTATUS% * || goto :ko
robocopy %HERE%\apps\full-build %HERE%\refbin /MIR

verify >nul
goto :eof

:ok
exit /b 0

:ko
exit /b 5
