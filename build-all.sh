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


function bootstrapBuild
{
  ./build-bootstrap.sh || failure
}

function build 
{
  ./build.sh $1 || failure
  ./publish.sh || failure
}

function testbuild
{
  ./test.sh || failure
}

VERSION=$1
bootstrapBuild
build $VERSION
testbuild 

