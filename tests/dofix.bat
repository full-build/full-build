@setlocal

@taskkill /im tgitcache.exe
@pushd toto
@echo ************************************************************************************
..\..\src\bin\Debug\FullBuildInterface /A:AnthologyUpdate || goto :ko

@echo ************************************************************************************
..\..\src\bin\Debug\FullBuildInterface /A:PkgUpdate || goto :ko

@echo ************************************************************************************
..\..\src\bin\Debug\FullBuildInterface /A:Fix || goto :ko

@echo ************************************************************************************
..\..\src\bin\Debug\FullBuildInterface /A:GenSln /V:cs /R:cassandra-sharp /R:cassandra-sharp-contrib || goto :ko

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
