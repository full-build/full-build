rem build twice - this is not a typo
rem first time is to build using old version
rem second time is to build using new version
rem both build ensure compatibility on version update
call :build
call :build
goto :ok

:build
call build-release.bat || goto :ko
call update-bootstrap.bat || goto :ko
goto :eof

:ok
exit /b 0

:ko
exit /b 5
