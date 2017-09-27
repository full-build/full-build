@echo off
setlocal

set HERE=%~dp0

robocopy .projects bootstrap\projects *.targets /S
robocopy .views bootstrap\views *.targets /S

:ok
exit /b 0
