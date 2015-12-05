//   Copyright 2014-2015 Pierre Chalamet
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

let defaultPSI (command : string) (args : string) (dir : DirectoryInfo) =
    let psi = ProcessStartInfo (FileName = command, Arguments = args, UseShellExecute = false, WorkingDirectory = dir.FullName, LoadUserProfile = true)
    psi

let ExecWithVars checkErrorCode (command : string) (args : string) (dir : DirectoryInfo) (vars : Map<string, string>) = 
    let psi = defaultPSI command args dir

    for var in vars do
        psi.Environment.Add(var.Key, var.Value)

    use proc = Process.Start (psi)
    if proc = null then failwith "Failed to start process"
    proc.WaitForExit()
    checkErrorCode proc.ExitCode

let Exec checkErrorCode (command : string) (args : string) (dir : DirectoryInfo) = 
    ExecWithVars checkErrorCode command args dir Map.empty

let ExecReadLine checkErrorCode (command : string) (args : string) (dir : DirectoryInfo) = 
    let mutable psi = defaultPSI command args dir
    psi.RedirectStandardOutput <- true

    use proc = Process.Start (psi)
    if proc = null then failwith "Failed to start process"
    proc.WaitForExit()
    checkErrorCode proc.ExitCode

    use stm = proc.StandardOutput
    stm.ReadLine ()

