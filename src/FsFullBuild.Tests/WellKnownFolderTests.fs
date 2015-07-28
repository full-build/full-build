module WellKnownFolderTests


open System
open System.IO
open WellknownFolders
open NUnit.Framework
open FsUnit


[<Test>]
let CheckIsWorkspaceFolder () =
    let currDir = new DirectoryInfo (Environment.CurrentDirectory)
    currDir |> IsWorkspaceFolder |> should equal false
