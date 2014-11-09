taskkill /im tgitcache.exe
rmdir /s /q titi
mkdir titi
..\src\bin\Debug\FullBuild init workspace titi || goto :ko

pushd titi
..\..\src\bin\Debug\FullBuild init view cs with cassandra-sharp  cassandra-sharp-contrib || goto :ko
popd

:ok
exit /b 0

:ko
