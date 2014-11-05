@setlocal

@taskkill /im tgitcache.exe
@robocopy titi toto /MIR
@robocopy src\.nuget toto\.nuget /MIR
@pushd toto
@echo ************************************************************************************
..\..\src\bin\Debug\FullBuildInterface update workspace || goto :ko

@echo ************************************************************************************
..\..\src\bin\Debug\FullBuildInterface update package || goto :ko

@echo ************************************************************************************
..\..\src\bin\Debug\FullBuildInterface fix source || goto :ko

@echo ************************************************************************************
..\..\src\bin\Debug\FullBuildInterface init view cs with cassandra-sharp cassandra-sharp-contrib || goto :ko

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
