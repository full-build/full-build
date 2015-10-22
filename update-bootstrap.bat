copy bin\fullbuild\FSharp.Configuration.dll bootstrap\ || goto :ko
copy bin\fullbuild\FSharp.Core.dll bootstrap\ || goto :ko
copy bin\fullbuild\SharpYaml.dll bootstrap\ || goto :ko
copy bin\fullbuild\FullBuild.exe bootstrap\ || goto :ko
copy bin\fullbuild\FullBuild.exe.config bootstrap\ || goto :ko
goto :ok

:ok
exit /b 0

:ko
exit /b 5
