@setlocal

@taskkill /im tgitcache.exe
@robocopy titi toto /MIR
@robocopy src\.nuget toto\.nuget /MIR
@pushd toto
@echo ************************************************************************************
..\..\src\bin\Debug\FullBuild update workspace || goto :ko

@echo ************************************************************************************
..\..\src\bin\Debug\FullBuild convert sources || goto :ko

@echo ************************************************************************************
..\..\src\bin\Debug\FullBuild init view cs with cassandra-sharp cassandra-sharp-contrib || goto :ko
..\..\src\bin\Debug\FullBuild update view cs || goto :ko

msbuild cs.sln || goto :ko

popd

:ok
@echo *********************************************
@echo SUCCESS
@echo *********************************************
exit /b 0

:ko
@echo *********************************************
@echo FAILURE
@echo *********************************************
popd
@exit /b 5
