module FileExtensionsTests

open System
open System.IO
open IoHelpers
open NUnit.Framework
open FsUnit

[<Test>]
let CheckGetSubDirectory () =
    let currDir = new DirectoryInfo (Environment.CurrentDirectory)
    let subdir = "toto" |> GetSubDirectory currDir
    subdir.FullName.Contains("toto") |> should equal true

[<Test>]
let CheckGetFile () =
    let currDir = new DirectoryInfo (Environment.CurrentDirectory)
    let file = "toto" |> GetFile currDir
    file.FullName.Contains("toto") |> should equal true
