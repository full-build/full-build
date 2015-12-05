module FileExtensionsTests

open System
open System.IO
open IoHelpers
open NUnit.Framework
open FsUnit

[<Test>]
let CheckGetSubDirectory () =
    let currDir = IoHelpers.CurrentFolder ()
    let subdir = currDir |> GetSubDirectory "toto"
    subdir.FullName.Contains("toto") |> should equal true

[<Test>]
let CheckCreateSubDirectory () =
    let tmpDir = DirectoryInfo (Path.GetTempPath())
    let dirName = Guid.NewGuid().ToString("D")
    let dir = tmpDir |> CreateSubDirectory dirName
    dir.Exists |> should equal true

[<Test>]
let CheckGetFile () =
    let currDir = IoHelpers.CurrentFolder ()
    let file = currDir |> GetFile "toto" 
    file.FullName.Contains("toto") |> should equal true

[<Test>]
let CheckAddExt () =
    let file = AddExt Targets "toto"
    file |> should equal "toto.targets"
