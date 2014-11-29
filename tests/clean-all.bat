@echo off
taskkill /im tgitcache.exe 1>NUL 2>NUL

call :cleanup cs || goto :ko
call :cleanup rc || goto :ko
call :cleanup fclp || goto :ko

echo *** SUCCESSFUL
exit /b 0

:ko
echo *** FAILURE
exit /b 5


:cleanup
if exist %1-init rmdir /s /q %1-init
if exist %1-do rmdir /s /q %1-do
goto :eof
