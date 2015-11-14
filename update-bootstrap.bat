bin\fullbuild\FullBuild.exe publish full-build || goto :ko
robocopy apps\full-build bootstrap /MIR || goto :ko
goto :ok

:ok
exit /b 0

:ko
exit /b 5
