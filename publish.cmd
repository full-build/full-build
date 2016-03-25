@echo off
setlocal

call :dopublish || goto :ko
goto :ok

:dopublish
bootstrap\fullbuild publish * || goto :ko
robocopy apps\full-build bootstrap
verify >nul
goto :eof

:ok
exit /b 0

:ko
exit /b 5
