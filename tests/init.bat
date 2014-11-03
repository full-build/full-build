taskkill /im tgitcache.exe
rmdir /s /q titi
mkdir titi
..\src\bin\Debug\FullBuildInterface /A:Init /W:titi
:ok
exit /b 0

:ko
