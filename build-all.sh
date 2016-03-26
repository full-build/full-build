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


function build 
{
  exec ./build.sh $1 || failure
  exec ./publish.sh || failure
}

function testbuild
{
  exec ./test.sh || failure
}

VERSION=$1
build $VERSION
build $VERSION
testbuild 

