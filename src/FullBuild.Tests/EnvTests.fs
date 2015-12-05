module WellKnownFolderTests


open System
open System.IO
open Env
open NUnit.Framework
open FsUnit


[<Test>]
let CheckIsWorkspaceFolder () =
    let currDir = IoHelpers.CurrentFolder ()
    currDir |> IsWorkspaceFolder |> should equal false
