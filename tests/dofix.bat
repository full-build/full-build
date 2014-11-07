@setlocal

@taskkill /im tgitcache.exe
@pushd toto
@echo ************************************************************************************
..\..\src\bin\Debug\FullBuild update workspace || goto :ko

@echo ************************************************************************************
..\..\src\bin\Debug\FullBuild convert projects || goto :ko

@echo ************************************************************************************
..\..\src\bin\Debug\FullBuild update packages || goto :ko

@echo ************************************************************************************
..\..\src\bin\Debug\FullBuild init view cs with cassandra-sharp cassandra-sharp-contrib || goto :ko

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
