@setlocal

@taskkill /im tgitcache.exe
@pushd toto

..\..\src\bin\Debug\FullBuild index workspace || goto :ko
..\..\src\bin\Debug\FullBuild convert projects || goto :ko
..\..\src\bin\Debug\FullBuild build view cs || goto :ko

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
