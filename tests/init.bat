taskkill /im tgitcache.exe
rmdir /s /q titi
mkdir titi
..\src\bin\Debug\FullBuild init workspace titi
:ok
exit /b 0

:ko
