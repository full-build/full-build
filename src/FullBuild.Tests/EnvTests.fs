module WellKnownFolderTests


open System
open System.IO
open Env
open NUnit.Framework
open FsUnit


[<Test>]
let CheckIsWorkspaceFolder () =
    let currDir = DirectoryInfo (Environment.CurrentDirectory)
    currDir |> IsWorkspaceFolder |> should equal false
