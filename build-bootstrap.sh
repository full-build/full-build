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

rm -rf .bin
rm -rf .views
rm -rf .projects
cp -r bootstrap/bin .bin
cp -r bootstrap/views .views
cp -r bootstrap/projects .projects
cp bootstrap/bootstrap.sln .

# msbuild /t:Build /p:SolutionDir=`pwd` /p:SolutionName=bootstrap bootstrap.sln
dotnet build --force bootstrap.sln
dotnet publish -o `pwd`/apps/refbin src/FullBuild || failure
