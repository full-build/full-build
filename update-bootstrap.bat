copy bin\*.dll bootstrap\ || goto :ko
copy bin\*.exe bootstrap\ || goto :ko

:ok
exit /b 0

:ko
exit /b 5
