rem if "%APPVEYOR_REPO_TAG%" NEQ "True" goto :ok

set HERE=%~dp0

echo HERE %HERE%
echo APPVEYOR_PROJECT_NAME %APPVEYOR_PROJECT_NAME% 
echo CONFIGURATION %CONFIGURATION% 
echo PLATFORM %PLATFORM% 
echo APPVEYOR_BUILD_VERSION %APPVEYOR_BUILD_VERSION%
echo APPVEYOR_REPO_BRANCH %APPVEYOR_REPO_BRANCH%
echo APPVEYOR_REPO_COMMIT %APPVEYOR_REPO_COMMIT%

7z a %CONFIGURATION%.zip .\src\bin\%CONFIGURATION%\*

%HERE%tools/github-release.exe release ^
                         --user pchalamet ^
                         --repo full-build ^
						 --tag %APPVEYOR_BUILD_VERSION% ^
						 --name "full-build %APPVEYOR_BUILD_VERSION%" ^
                         --description "%APPVEYOR_PROJECT_NAME% %APPVEYOR_BUILD_VERSION% %PLATFORM% %CONFIGURATION% %APPVEYOR_REPO_COMMIT%" ^
                         --pre-release || goto :ko

%HERE%tools/github-release.exe upload ^
                     --user pchalamet ^
					 --repo full-build ^
                     --tag %APPVEYOR_REPO_BRANCH% ^
                     --name "%APPVEYOR_PROJECT_NAME%-net45-%PLATFORM%" ^
                     --file %HERE%%CONFIGURATION%.zip || goto :ko

:ok
exit /b 0

:ko
exit /b 5