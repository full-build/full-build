module WellKnownFolderTests


open System
open System.IO
open Env
open NUnit.Framework
open FsUnit


[<Test>]
let CheckIsWorkspaceFolder () =
    let currDir = TestContext.CurrentContext.TestDirectory |> DirectoryInfo
    currDir |> IsWorkspaceFolder |> should equal false


[<Test>]
let CheckLicense () =
    Env.CheckLicense ()
