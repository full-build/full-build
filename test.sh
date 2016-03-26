#! /bin/bash

mono bootstrap/fullbuild.exe test * || exit 5

