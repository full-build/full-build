//   Copyright 2014-2016 Pierre Chalamet
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

let defaultPSI (command : string) (args : string) (dir : DirectoryInfo) (redirectStdout : bool) =
    let psi = ProcessStartInfo (FileName = command, Arguments = args, UseShellExecute = false, WorkingDirectory = dir.FullName, LoadUserProfile = true, RedirectStandardOutput = redirectStdout)
    psi
    
let ExecWithVars checkErrorCode (command : string) (args : string) (dir : DirectoryInfo) (vars : Map<string, string>) =
    let psi = defaultPSI command args dir false

    for var in vars do
        psi.EnvironmentVariables.Add(var.Key, var.Value)

    use proc = Process.Start (psi)
    if proc = null then failwith "Failed to start process"
    proc.WaitForExit()
    checkErrorCode proc.ExitCode

let Exec checkErrorCode (command : string) (args : string) (dir : DirectoryInfo) =
    ExecWithVars checkErrorCode command args dir Map.empty

let ExecWithVarsGetOutput checkErrorCode (command : string) (args : string) (dir : DirectoryInfo) (vars : Map<string, string>) =
    let psi = defaultPSI command args dir true

    for var in vars do
        psi.EnvironmentVariables.Add(var.Key, var.Value)

    use proc = Process.Start (psi)
    if proc = null then failwith "Failed to start process"
    proc.WaitForExit()
    checkErrorCode proc.ExitCode
    proc.StandardOutput.ReadToEnd()

let ExecGetOutput checkErrorCode (command : string) (args : string) (dir : DirectoryInfo) =
    ExecWithVarsGetOutput checkErrorCode command args dir Map.empty

let SpawnWithVerb (command : string) (verb : string) =
    let psi = ProcessStartInfo (FileName = command, UseShellExecute = true, Verb = verb)
    use proc = Process.Start (psi)
    ()

let Spawn (command : string) (args : string) =
    let psi = ProcessStartInfo (FileName = command, UseShellExecute = false, Arguments = args)
    use proc = Process.Start (psi)
    ()

let ExecReadLine checkErrorCode (command : string) (args : string) (dir : DirectoryInfo) =
    let mutable psi = defaultPSI command args dir false
    psi.RedirectStandardOutput <- true

    use proc = Process.Start (psi)
    if proc = null then failwith "Failed to start process"
    proc.WaitForExit()
    checkErrorCode proc.ExitCode

    use stm = proc.StandardOutput
    stm.ReadLine ()

