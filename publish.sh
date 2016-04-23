#! /bin/bash

function failure
{
  exit 5
}

mono src/fullbuild/bin/fullbuild.exe publish "*" || failure
cp -r apps/full-build ./refbin

