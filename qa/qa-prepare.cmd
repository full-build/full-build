setlocal
set HERE=%~dp0
set LOCALFOLDER=%HERE%local
set LOCALFBREPO=%HERE%local\full-build-org
set LOCALCSREPO=%HERE%local\cassandra-sharp-org
set LOCALCSCREPO=%HERE%local\cassandra-sharp-contrib-org
set LOCALBIN=%HERE%local\bin-org

set PAKET=paket.exe
set PATH=%PATH%;%HERE%..\tools

cd %HERE%
rmdir /s /q %LOCALFOLDER%

rem create binaries folder
mkdir %LOCALBIN%

rem creating master repository
mkdir %LOCALFBREPO%
git init %LOCALFBREPO%
pushd %LOCALFBREPO%
git config --local receive.denyCurrentBranch updateInstead
echo dummy > dummy.txt
git add dummy.txt
git commit -am "initial commit"
popd

rem cloning repositories
git clone https://github.com/pchalamet/cassandra-sharp %LOCALCSREPO%
pushd %LOCALCSREPO%
git config --local receive.denyCurrentBranch updateInstead
popd

git clone https://github.com/pchalamet/cassandra-sharp-contrib %LOCALCSCREPO%
pushd %LOCALCSCREPO%
git config --local receive.denyCurrentBranch updateInstead
popd

rem fetch tools
%PAKET% restore || goto :ko
type %HERE%paket.lock
