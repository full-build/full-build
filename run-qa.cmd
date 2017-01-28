setlocal
set HERE=%~dp0
set PATH=%HERE%refbin;%PATH%

cd qa
call qa-prepare.cmd || goto :eof
call qa-setup.cmd || goto :eof
call qa-init.cmd || goto :eof

:ok
cd %HERE%
echo *** QA SUCCESSFUL
exit /b 0

:ko
cd %HERE%
echo *** QA FAILURE
exit /b 5
