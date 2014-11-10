taskkill /im tgitcache.exe
rmdir /s /q titi
mkdir titi

set PATH=%PATH%;c:\dev\projects\full-build\src\bin\Debug

FullBuild init workspace titi || goto :ko

pushd titi
FullBuild clone repo * || goto :ko
popd

:ok
exit /b 0

:ko
