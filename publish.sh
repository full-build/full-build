#! /bin/bash

function failure
{
  exit 5
}

function publish
{
  mono dotnet/fullbuild/bin/fullbuild.exe publish "*" || failure
  cp apps/full-build/* bootstrap/
}


publish

