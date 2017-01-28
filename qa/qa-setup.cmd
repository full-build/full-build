setlocal
echo on
set HERE=%~dp0
set PAKET=paket.exe
rem set FULLBUILD=%HERE%packages/full-build/tools/fullbuild.exe --verbose
set FULLBUILD=fullbuild.exe --verbose

set PATH=%HERE%packages\NUnit.ConsoleRunner\tools;%PATH%
set PATH=%HERE%packages\Paket\tools;%PATH%
set PATH=%HERE%packages\full-build\tools;%PATH%

set LOCALFBREPO=%HERE%local\full-build
set LOCALCSREPO=%HERE%local\cassandra-sharp
set LOCALCSCREPO=%HERE%local\cassandra-sharp-contrib
set LOCALBIN=%HERE%local\bin
set QAFOLDER=%HERE%qa-setup

set VERSION=%TIME:~0,2%.%TIME:~3,2%.%TIME:~6,2%

robocopy %LOCALFBREPO%-org %LOCALFBREPO% /MIR /NFL /NDL /NJH /NJS /nc /ns /np
robocopy %LOCALCSREPO%-org %LOCALCSREPO% /MIR /NFL /NDL /NJH /NJS /nc /ns /np
robocopy %LOCALCSCREPO%-org %LOCALCSCREPO% /MIR /NFL /NDL /NJH /NJS /nc /ns /np
rmdir /s /q %LOCALBIN%
mkdir %LOCALBIN%

taskkill /im tgitcache.exe
:qa_folder_delete
    if not exist %QAFOLDER% goto :qa_folder_deleted
    rmdir /s /q %QAFOLDER%
goto :qa_folder_delete
:qa_folder_deleted

%FULLBUILD% setup git %LOCALFBREPO% %LOCALBIN% %QAFOLDER% || goto :ko
cd %QAFOLDER% || goto :ko

pushd .full-build
echo framework: net45 > paket.dependencies
popd

%FULLBUILD% nuget add https://www.nuget.org/api/v2/ || goto :ko
%FULLBUILD% repo add cassandra-sharp %LOCALCSREPO% || goto :ko
%FULLBUILD% repo add cassandra-sharp-contrib %LOCALCSCREPO% || goto :ko
%FULLBUILD% clone * || goto :ko
%FULLBUILD% branch || goto :ko
%FULLBUILD% repo list || goto :ko
%FULLBUILD% convert * || goto :ko
%FULLBUILD% convert --check * || goto :ko
%FULLBUILD% install || goto :ko
%FULLBUILD% convert * || goto :ko
%FULLBUILD% view all * || goto :ko
%FULLBUILD% view csc cassandra-sharp-contrib/* || goto :ko
%FULLBUILD% view list || goto :ko
%FULLBUILD% view describe all || goto :ko
%FULLBUILD% graph all || goto :ko
%FULLBUILD% build all || goto :ko
%FULLBUILD% view drop csc || goto :ko
%FULLBUILD% app add cqlplus.zip zip cqlplus || goto :ko

pushd .full-build
git add *
git commit -am "qa"
git push origin master:master
popd

pushd cassandra-sharp
git add *
git commit -am "qa"
git push origin master:master
popd

pushd cassandra-sharp-contrib
git add *
git commit -am "qa"
git push origin master:master
popd

%FULLBUILD% publish --full --push %VERSION% cqlplus.zip || goto :ko
%FULLBUILD% app list || goto :ko
%FULLBUILD% app list --version %VERSION% || goto :ko

:ok
cd %HERE%
echo *** SUCCESSFUL
exit /b 0

:ko
cd %HERE%
echo *** FAILURE
exit /b 5
