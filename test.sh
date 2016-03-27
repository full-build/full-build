#! /bin/bash

mono refbin/fullbuild.exe test "*" || exit 5

