﻿module FileExtensionsTests

open System
open System.IO
open IoHelpers
open NUnit.Framework
open FsUnit

[<Test>]
let CheckGetSubDirectory () =
    let currDir = DirectoryInfo (Environment.CurrentDirectory)
    let subdir = currDir |> GetSubDirectory "toto"
    subdir.FullName.Contains("toto") |> should equal true

[<Test>]
let CheckCreateSubDirectory () =
    let tmpDir = DirectoryInfo (Path.GetTempPath())
    let dirName = Guid.NewGuid().ToString("D")
    let dir = CreateSubDirectory tmpDir dirName
    dir.Exists |> should equal true

[<Test>]
let CheckGetFile () =
    let currDir = DirectoryInfo (Environment.CurrentDirectory)
    let file = currDir |> GetFile "toto" 
    file.FullName.Contains("toto") |> should equal true

[<Test>]
let CheckAddExt () =
    let file = AddExt "toto" Targets
    file |> should equal "toto.targets"
