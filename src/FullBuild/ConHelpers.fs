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

module ConHelpers

open System


let consoleLock = System.Object()

let ConsoleDisplay (c : ConsoleColor) (s : string) =
    let oldColor = Console.ForegroundColor
    try
        Console.ForegroundColor <- c
        Console.WriteLine(s)
    finally
        Console.ForegroundColor <- oldColor


let DisplayInfo msg = ConsoleDisplay ConsoleColor.Cyan ("- " + msg)
let DisplayError msg = ConsoleDisplay ConsoleColor.Red msg

let PrintOutput info (execResult : IO.ExecResult) =
    let rec printl lines =
        match lines with
        | line :: tail -> printfn "%s" line; printl tail
        | [] -> ()

    let display () =
        info |> DisplayInfo
        execResult.Out |> printl
        execResult.Error |> printl
        execResult

    lock consoleLock display

