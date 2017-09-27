@echo off
setlocal

set HERE=%~dp0

robocopy .full-build\projects bootstrap\projects *.targets /S

:ok
exit /b 0
