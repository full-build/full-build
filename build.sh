#! /bin/bash


function failure
{
  exit 5
}


function build
{
  mono bootstrap/fullbuild.exe install || failure
  mono bootstrap/fullbuild.exe view all "*" || failure
  mono bootstrap/fullbuild.exe rebuild --version $1 all || failure
}

VERSION=$1
if [ -z "$VERSION" ]; then 
  VERSION=0.0.0.*
fi 

echo building version $VERSION
build $VERSION

