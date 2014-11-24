set HERE=%~dp0

echo HERE %HERE%
echo APPVEYOR_PROJECT_NAME %APPVEYOR_PROJECT_NAME% 
echo CONFIGURATION %CONFIGURATION% 
echo PLATFORM %PLATFORM% 
echo APPVEYOR_BUILD_VERSION %APPVEYOR_BUILD_VERSION%
echo APPVEYOR_REPO_COMMIT %APPVEYOR_REPO_COMMIT%

7z a %HERE%%CONFIGURATION%.zip %HERE%\src\bin\%CONFIGURATION%\*

%HERE%tools/github-release.exe release ^
                         --user pchalamet ^
                         --repo full-build ^
						 --tag %APPVEYOR_BUILD_VERSION% ^
						 --name "full-build %APPVEYOR_BUILD_VERSION%" ^
                         --description "%APPVEYOR_PROJECT_NAME%-net45-anycpu %CONFIGURATION% %APPVEYOR_BUILD_VERSION%" ^
                         --pre-release || goto :ko

%HERE%tools/github-release.exe upload ^
                     --user pchalamet ^
					 --repo full-build ^
                     --tag %APPVEYOR_BUILD_VERSION% ^
                     --name "%APPVEYOR_PROJECT_NAME%-net45-anycpu.zip" ^
                     --file %HERE%%CONFIGURATION%.zip || goto :ko

:ok
exit /b 0

:ko
exit /b 5