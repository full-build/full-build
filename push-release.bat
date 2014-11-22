if "%APPVEYOR_REPO_TAG%" NEQ "True" goto :ok

set HERE=%~dp0

git tag -d %APPVEYOR_BUILD_VERSION% || goto :ko
git push origin :refs/tags/%APPVEYOR_BUILD_VERSION% || goto :ko

git tag -a %APPVEYOR_BUILD_VERSION% -m "Release vesion %APPVEYOR_BUILD_VERSION%" || goto :ko
git push --tags || goto :ko

%HERE%tools\github-release.exe release ^
                         --user pchalamet ^
                         --repo full-build ^
						 --tag %APPVEYOR_BUILD_VERSION% ^
						 --name "full-build %APPVEYOR_BUILD_VERSION%" ^
                         --description "%APPVEYOR_PROJECT_NAME% %APPVEYOR_BUILD_VERSION% - %PLATFORM% %CONFIGURATION%" ^
                         --pre-release || goto :ko

%HERE%tools\github-release upload ^
                     --user pchalamet ^
					 --repo full-build ^
                     --tag %APPVEYOR_BUILD_VERSION% ^
                     --name "%APPVEYOR_PROJECT_NAME%-net45-%PLATFORM%" ^
                     --file %HERE%/src/bin/%CONFIGURATION%.zip || goto :ko

:ok
exit /b 0

:ko
exit /b 5