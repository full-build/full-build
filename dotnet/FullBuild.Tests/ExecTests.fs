module ExecTests

open NUnit.Framework
open FsUnit
open System
open System.IO


let private checkErrorCode err =
    if err <> 0 then failwithf "Process failed with error %d" err

let private checkedExec = 
    Exec.Exec checkErrorCode

[<Test>]
let CheckExecOk () =
    let currDir = IoHelpers.CurrentFolder ()
    checkedExec "cmd" "/c dir >nul" currDir


[<Test>]
let CheckExecFailure () =
    let currDir = IoHelpers.CurrentFolder ()
    (fun () -> checkedExec "gloubiboulga" "" currDir |> ignore) |> should throw typeof<System.ComponentModel.Win32Exception>

