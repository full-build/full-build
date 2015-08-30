copy bin\*.dll bootstrap\fullbuild || goto :ko
copy bin\*.exe bootstrap\fullbuild || goto :ko

:ok
exit /b 0

:ko
exit /b 5
