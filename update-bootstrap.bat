copy bin\FSharp.Configuration.dll bootstrap\ || goto :ko
copy bin\FSharp.Core.dll bootstrap\ || goto :ko
copy bin\SharpYaml.dll bootstrap\ || goto :ko
copy bin\FullBuild.exe bootstrap\ || goto :ko
copy bin\FullBuild.exe.config bootstrap\ || goto :ko
goto :ok

:ok
exit /b 0

:ko
exit /b 5
