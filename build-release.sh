#! /bin/sh
mono bootstrap/fullbuild.exe install
mono bootstrap/fullbuild.exe add view fullbuild *
mono bootstrap/fullbuild.exe build fullbuild
