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

module Exec

open System.Diagnostics
open System.IO


[<NoComparison; RequireQualifiedAccess>]
type private MonitorCommand =
    | Out of string list
    | Err of string list
    | End of int

type ExecResult =
    { ResultCode: int
      Info : string
      Out: string list
      Error: string list }

let private defaultPSI (command : string) (args : string) (dir : DirectoryInfo) (vars : Map<string, string>) redirect =
    let psi = ProcessStartInfo (FileName = command,
                                Arguments = args,
                                UseShellExecute = false,
                                WorkingDirectory = dir.FullName,
                                LoadUserProfile = true)
    for var in vars do
        psi.EnvironmentVariables.Add(var.Key, var.Value)

    if redirect then
        psi.RedirectStandardOutput <- true
        psi.RedirectStandardError <- true

    psi


let private supervisedExec redirect (command : string) (args : string) (dir : DirectoryInfo) (vars : Map<string, string>) =
    let psi = defaultPSI command args dir vars redirect
    use proc = Process.Start (psi)
    if proc |> isNull then failwith "Failed to start process"

    let rec read (stm : System.IO.TextReader) buffer =
        let line = stm.ReadLine()
        if line |> isNull then buffer
        else
            read stm buffer@[line]

    let asyncOut = if redirect then async { return read proc.StandardOutput List.empty |> MonitorCommand.Out }
                               else async { return List.empty |> MonitorCommand.Out }
    let asyncErr = if redirect then async { return read proc.StandardError List.empty |> MonitorCommand.Err }
                               else async { return List.empty |> MonitorCommand.Err }
    let asyncCode = async { proc.WaitForExit(); return proc.ExitCode |> MonitorCommand.End }
    let res = [ asyncCode ; asyncOut ; asyncErr ] |> Async.Parallel |> Async.RunSynchronously
    match res.[0], res.[1], res.[2] with
    | MonitorCommand.End code, MonitorCommand.Out out, MonitorCommand.Err err -> { ResultCode=code; Out=out; Error=err; Info = sprintf "%s %s @ %s" command args dir.FullName }
    | _ -> failwith "Unexpected results"

let Exec =
    supervisedExec false

let ExecGetOutput =
    supervisedExec true

let private resultToError execResult =
    if execResult.ResultCode <> 0 then (sprintf "Operation '%s' failed with error %d" execResult.Info execResult.ResultCode) |> Some
    else None

let GetOutput execResult =
    match execResult |> resultToError with
    | Some error -> failwith error
    | None -> execResult.Out

let CheckResponseCode execResult =
    match execResult |> resultToError with
    | Some error -> failwith error
    | None -> ()

let CheckMultipleResponseCode execResults =
    let errors = execResults |> Seq.choose (fun execResult -> execResult |> resultToError)
    if errors |> Seq.isEmpty |> not then
        errors |> String.concat System.Environment.NewLine |> failwith

let Spawn (command : string) (args : string) (verb : string) =
    let psi = ProcessStartInfo (FileName = command, UseShellExecute = true, Arguments = args, Verb = verb)
    use proc = Process.Start (psi)
    ()
