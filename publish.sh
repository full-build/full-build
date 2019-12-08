#! /bin/bash

function failure
{
  exit 5
}

dotnet publish -o `pwd`/apps/refbin src/FullBuild || failure

