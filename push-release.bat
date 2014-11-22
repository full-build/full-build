rem if "%APPVEYOR_REPO_TAG%" NEQ "True" goto :ok

set HERE=%~dp0

echo pushing %APPVEYOR_PROJECT_NAME% %CONFIGURATION% %PLATFORM% %APPVEYOR_BUILD_VERSION% from %HERE%

%HERE%tools/github-release.exe release ^
                         --user pchalamet ^
                         --repo full-build ^
						 --tag %APPVEYOR_BUILD_VERSION% ^
						 --name "full-build %APPVEYOR_BUILD_VERSION%" ^
                         --description "%APPVEYOR_PROJECT_NAME% %APPVEYOR_BUILD_VERSION% (%APPVEYOR_REPO_COMMIT%)- %PLATFORM% %CONFIGURATION%" ^
                         --pre-release || goto :ko

%HERE%tools/github-release.exe upload ^
                     --user pchalamet ^
					 --repo full-build ^
                     --tag %APPVEYOR_BUILD_VERSION% ^
                     --name "%APPVEYOR_PROJECT_NAME%-net45-%PLATFORM%" ^
                     --file %HERE%src/bin/%CONFIGURATION%.zip || goto :ko

:ok
exit /b 0

:ko
exit /b 5