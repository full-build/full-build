// Copyright (c) 2014-2015, Pierre Chalamet
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//     * Redistributions of source code must retain the above copyright
//       notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright
//       notice, this list of conditions and the following disclaimer in the
//       documentation and/or other materials provided with the distribution.
//     * Neither the name of Pierre Chalamet nor the
//       names of its contributors may be used to endorse or promote products
//       derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL PIERRE CHALAMET BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
module Exec

open System.Diagnostics
open System.IO

let defaultPSI (command : string) (args : string) (dir : DirectoryInfo) =
    let psi = ProcessStartInfo (FileName = command, Arguments = args, UseShellExecute = false, WorkingDirectory = dir.FullName, LoadUserProfile = true)
    psi

let ExecWithArgs (command : string) (args : string) (dir : DirectoryInfo) (vars : Map<string, string>) = 
    let psi = defaultPSI command args dir

    for var in vars do
        psi.EnvironmentVariables.Add (var.Key, var.Value)

    use proc = Process.Start (psi)
    if proc = null then failwith "Failed to start process"
    proc.WaitForExit()
    if proc.ExitCode <> 0 then printf "[WARNING] Process exited with error code %i" proc.ExitCode
    if proc.ExitCode > 5 then failwithf "Process failed with error %d" proc.ExitCode

let Exec (command : string) (args : string) (dir : DirectoryInfo) = 
    ExecWithArgs command args dir Map.empty

let ExecReadLine (command : string) (args : string) (dir : DirectoryInfo) = 
    let mutable psi = defaultPSI command args dir
    psi.RedirectStandardOutput <- true

    use proc = Process.Start (psi)
    if proc = null then failwith "Failed to start process"
    proc.WaitForExit()
    if proc.ExitCode <> 0 then failwithf "Process failed with error %d" proc.ExitCode
    use stm = proc.StandardOutput
    stm.ReadLine ()

