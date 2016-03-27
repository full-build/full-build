#! /bin/bash


function failure
{
  echo BUILD FAILURE
  exit 5
}


function build
{
  mono refbin/fullbuild.exe install || failure
  mono refbin/fullbuild.exe view all "*" || failure
  mono refbin/fullbuild.exe rebuild --version $1 all || failure
}

VERSION=$1
if [ -z "$VERSION" ]; then 
  VERSION=0.0.0.*
fi 

echo building version $VERSION
build $VERSION

