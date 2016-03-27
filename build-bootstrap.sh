#! /bin/bash

# build twice - this is not a typo
# first time is to build using old version
# second time is to build using new version
# both build ensure compatibility on version update

function failure
{
  echo BUILD FAILURE
  exit 5
}


mkdir .full-build/bin
mkdir .full-build/views
pushd .full-build
mono ../bootstrap/paket.exe install
popd
cp -r bootstrap/bin .full-build/
cp -r bootstrap/views .full-build/
cp -r bootstrap/projects .full-build/
cp -r bootstrap/packages .full-build/
cp bootstrap/bootstrap.sln .

xbuild /t:Rebuild /p:SolutionDir=`pwd` /p:SolutionName=bootstrap bootstrap.sln

