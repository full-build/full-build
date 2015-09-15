set HERE=%~dp0

echo HERE %HERE%
echo APPVEYOR_PROJECT_NAME %APPVEYOR_PROJECT_NAME% 
echo CONFIGURATION %CONFIGURATION% 
echo PLATFORM %PLATFORM% 
echo APPVEYOR_BUILD_VERSION %APPVEYOR_BUILD_VERSION%
echo APPVEYOR_REPO_COMMIT %APPVEYOR_REPO_COMMIT%
echo APPVEYOR_REPO_COMMIT_MESSAGE  %APPVEYOR_REPO_COMMIT_MESSAGE% 
echo APPVEYOR_BUILD_NUMBER  %APPVEYOR_BUILD_NUMBER% 

7z a %HERE%%CONFIGURATION%.zip %HERE%\bin\*

%HERE%tools/github-release.exe release ^
                         --user pchalamet ^
                         --repo full-build ^
						 --tag %APPVEYOR_BUILD_VERSION% ^
						 --name "%APPVEYOR_PROJECT_NAME% %CONFIGURATION% %PLATFORM% %APPVEYOR_BUILD_VERSION%" ^
                         --description "%APPVEYOR_REPO_COMMIT% %APPVEYOR_REPO_COMMIT_MESSAGE%" ^
                         --pre-release || goto :ko

%HERE%tools/github-release.exe upload ^
                     --user pchalamet ^
					 --repo full-build ^
                     --tag %APPVEYOR_BUILD_VERSION% ^
                     --name "%APPVEYOR_PROJECT_NAME%-net45-anycpu-%APPVEYOR_BUILD_NUMBER%.zip" ^
                     --file %HERE%%CONFIGURATION%.zip || goto :ko

:ok
exit /b 0

:ko
exit /b 5
