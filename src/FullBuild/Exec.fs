﻿//   Copyright 2014-2016 Pierre Chalamet
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

let private defaultPSI (command : string) (args : string) (dir : DirectoryInfo) (vars : Map<string, string>) =
    let psi = ProcessStartInfo (FileName = command,
                                Arguments = args,
                                UseShellExecute = false, 
                                WorkingDirectory = dir.FullName, 
                                LoadUserProfile = true, 
                                RedirectStandardOutput = true, 
                                RedirectStandardError = true,
                                StandardOutputEncoding = System.Text.Encoding.GetEncoding(850))
    for var in vars do
        psi.EnvironmentVariables.Add(var.Key, var.Value)
    psi
    

let private supervisedExec onOut onErr onEnd (command : string) (args : string) (dir : DirectoryInfo) (vars : Map<string, string>) =
    let psi = defaultPSI command args dir vars
    use proc = Process.Start (psi)
    if proc |> isNull then failwith "Failed to start process"

    let rec read processLine (stm : System.IO.TextReader) buffer =
        let line = stm.ReadLine()
        if line |> isNull then buffer
        else
            processLine line
            read processLine stm buffer@[line]

    let asyncOut = async { return read onOut proc.StandardOutput List.empty |> MonitorCommand.Out }
    let asyncErr = async { return read onErr proc.StandardError List.empty |> MonitorCommand.Err }
    let asyncCode = async { proc.WaitForExit(); return proc.ExitCode |> MonitorCommand.End }
    let res = [ asyncCode ; asyncOut ; asyncErr ] |> Async.Parallel |> Async.RunSynchronously 
    match res.[0], res.[1], res.[2] with
    | MonitorCommand.End code, MonitorCommand.Out out, MonitorCommand.Err err -> onEnd code out err
    | _ -> failwith "Unexpected results"



let ExecBuffered checkError =
    supervisedExec ignore ignore checkError

let Exec checkError = 
    supervisedExec (printfn "%s") (printfn "%s") checkError

let ExecSingleLine checkError (command : string) (args : string) (dir : DirectoryInfo) (vars : Map<string, string>) =
    let mutable res : string = null
    let firstLine code out err =
        checkError code out err        
        match out with
        | x :: _ -> res <- x
        | [] -> failwith "No data found"

    supervisedExec (printfn "%s") (printfn "%s") firstLine command args dir vars
    res


let Spawn (command : string) (args : string) =
    let psi = ProcessStartInfo (FileName = command, UseShellExecute = false, Arguments = args)
    use proc = Process.Start (psi)
    ()

let SpawnWithVerb (command : string) (verb : string) =
    let psi = ProcessStartInfo (FileName = command, UseShellExecute = true, Verb = verb)
    use proc = Process.Start (psi)
    ()
