rem http://stackoverflow.com/questions/25349296/how-to-release-automatically-your-artifact-to-github

if "%APPVEYOR_REPO_TAG%" NEQ "True" goto :ok

git tag -a %APPVEYOR_BUILD_VERSION% -m "Release vesion %APPVEYOR_BUILD_VERSION%" || goto :ko
git push --tags || goto :ko

echo Creating release...
echo {"tag_name": "%PLATFORM%_%APPVEYOR_BUILD_VERSION%","target_commitish": "%APPVEYOR_REPO_BRANCH%","name": "%APPVEYOR_PROJECT_NAME% v%APPVEYOR_BUILD_VERSION% for %PLATFORM% devices","body": "Release of %APPVEYOR_PROJECT_NAME% v%APPVEYOR_BUILD_VERSION%\n Commit by %APPVEYOR_REPO_COMMIT_AUTHOR% \n%APPVEYOR_REPO_COMMIT_MESSAGE%","draft": false,"prerelease": true} > json.json
curl -# -XPOST -H 'Content-Type:application/json' -H 'Accept:application/json' --data-binary @json.json https://api.github.com/repos/%APPVEYOR_REPO_NAME%/releases?access_token=%GITHUB_TOKEN% -o response.json || goto :ko
del json.json

echo Search the release id...
type response.json | findrepl id | findrepl /O:1:1 >> raw_id.txt
del response.json
echo Refining the id...
set /p raw_id_release=<raw_id.txt
set raw_id_release2=%raw_id_release:*"id": =%
set id_release=%raw_id_release2:,=%
echo The ID is %id_release%
del raw_id.txt

echo Uploading artifact to Github...
curl -# -XPOST -H "Authorization:token %GITHUB_TOKEN%" -H "Content-Type:application/octet-stream" --data-binary @%APPVEYOR_PROJECT_NAME%.zip https://uploads.github.com/repos/%APPVEYOR_REPO_NAME%/releases/%id_release%/assets?name=%APPVEYOR_PROJECT_NAME%.zip || goto :ko
echo done
:ok
exit /b 0

:ko
exit /b 5