module ExecTests

open NUnit.Framework
open FsUnit
open Exec
open System
open System.IO

[<Test>]
let CheckExecOk () =
    let currDir = DirectoryInfo(Environment.CurrentDirectory)
    Exec "cmd" "/c dir" currDir


[<Test>]
let CheckExecFailure () =
    let currDir = DirectoryInfo(Environment.CurrentDirectory)
    (fun () -> Exec "gloubiboulga" "" currDir |> ignore) |> should throw typeof<System.ComponentModel.Win32Exception>

