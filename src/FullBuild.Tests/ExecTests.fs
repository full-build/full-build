//   Copyright 2014-2017 Pierre Chalamet
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.

module ExecTests

open NUnit.Framework
open FsUnit


[<Test>]
let CheckExecOk () =
    if not <| Env.IsMono() then
        let currDir = IoHelpers.CurrentFolder ()
        Exec.Exec "cmd" "/c dir >nul" currDir Map.empty |> Exec.CheckResponseCode

[<Test>]
let CheckExecFailure () =
    let currDir = IoHelpers.CurrentFolder ()
    (fun () -> Exec.Exec "gloubiboulga" "" currDir Map.empty |> Exec.CheckResponseCode |> ignore) |> should throw typeof<System.ComponentModel.Win32Exception>

