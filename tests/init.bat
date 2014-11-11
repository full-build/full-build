taskkill /im tgitcache.exe
rmdir /s /q titi
mkdir titi

..\src\bin\Debug\FullBuild init workspace titi || goto :ko

pushd titi
..\..\src\bin\Debug\FullBuild add git repo cassandra-sharp from https://github.com/pchalamet/cassandra-sharp
..\..\src\bin\Debug\FullBuild add git repo cassandra-sharp-contrib from https://github.com/pchalamet/cassandra-sharp-contrib
..\..\src\bin\Debug\FullBuild clone repo * || goto :ko
popd

:ok
exit /b 0

:ko
