src\FullBuild\bin\FullBuild.exe publish * || goto :ko
robocopy apps\full-build bootstrap || goto :ko
goto :ok

:ok
exit /b 0

:ko
exit /b 5
