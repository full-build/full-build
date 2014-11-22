rem if "%APPVEYOR_REPO_TAG%" NEQ "True" goto :ok

set HERE=%~dp0

rem git config --global user.email "%APPVEYOR_REPO_COMMIT_AUTHOR%" 1>NUL 2>NUL
rem git config --global user.name "%APPVEYOR_REPO_COMMIT_AUTHOR_EMAIL%" 1>NUL 2>NUL

rem git tag -a %APPVEYOR_BUILD_VERSION% -m "Release vesion %APPVEYOR_BUILD_VERSION%" 1>NUL 2>NUL || goto :ko
rem git push --tags https://%GITHUB_TOKEN%@github.com/pchalamet/full-build.git 1>NUL 2>NUL || goto :ko

echo pushing %APPVEYOR_PROJECT_NAME% %CONFIGURATION% %PLATFORM% %APPVEYOR_BUILD_VERSION% from %HERE%

%HERE%tools/github-release.exe release ^
                         --user %APPVEYOR_REPO_COMMIT_AUTHOR% ^
                         --repo %APPVEYOR_PROJECT_NAME% ^
						 --tag %APPVEYOR_BUILD_VERSION% ^
						 --name "full-build %APPVEYOR_BUILD_VERSION%" ^
                         --description "%APPVEYOR_PROJECT_NAME% %APPVEYOR_BUILD_VERSION% (%APPVEYOR_REPO_COMMIT%)- %PLATFORM% %CONFIGURATION%" ^
                         --pre-release || goto :ko

%HERE%tools/github-release.exe upload ^
                     --user %APPVEYOR_REPO_COMMIT_AUTHOR% ^
					 --repo %APPVEYOR_PROJECT_NAME% ^
                     --tag %APPVEYOR_BUILD_VERSION% ^
                     --name "%APPVEYOR_PROJECT_NAME%-net45-%PLATFORM%" ^
                     --file %HERE%src/bin/%CONFIGURATION%.zip || goto :ko

:ok
exit /b 0

:ko
exit /b 5