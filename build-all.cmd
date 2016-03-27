rem build twice - this is not a typo
rem first time is to build using old version
rem second time is to build using new version
rem both build ensure compatibility on version update

@echo off
setlocal

call :bootstrapbuild || goto :ko
call :build %1 || goto :ko
call :test || goto :ko
goto :ok

:bootstrapbuild
call build-bootstrap.cmd || goto :ko
goto :eof

:build
call build.cmd %1 || goto :ko
call publish.cmd || goto :ko
goto :eof

:test
call test.cmd || goto :ko
goto :eof

:ok
exit /b 0

:ko
echo BUILD FAILURE
exit /b 5
