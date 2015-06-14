module ExecTests

open NUnit.Framework
open FsUnit
open Exec
open System

[<Test>]
let CheckExecOk () =
    Exec "cmd" "/c dir" Environment.CurrentDirectory

[<Test>]
let CheckExecFailure () =
    (fun () -> Exec "gloubiboulga" "" System.Environment.CurrentDirectory |> ignore) |> should throw typeof<System.ComponentModel.Win32Exception>

