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

module Integration

open FsUnit
open NUnit.Framework
open StringHelpers
open System.IO
open System.Diagnostics
open Collections


let runFB (args : string) =
    printfn "==== running fullbuild %s" args
    System.Environment.CurrentDirectory <- TestContext.CurrentContext.TestDirectory
    let wsDir = Env.GetFolder Env.Folder.Workspace
    let graph = Graph.load()
    let fbProject = graph.Projects |> Seq.find (fun x -> x.ProjectId = "fullbuild")
    let bin = fbProject.BinFile "net452" "Debug"
    let fb = wsDir |> FsHelpers.GetFile bin
    let psi = ProcessStartInfo (FileName = fb.FullName, Arguments = args, UseShellExecute = false, WorkingDirectory = wsDir.FullName, LoadUserProfile = true, CreateNoWindow = true, RedirectStandardOutput = true)
    use proc = Process.Start (psi)
    use stdout = proc.StandardOutput
    while stdout.EndOfStream |> not do
        let line = stdout.ReadLine()
        printfn "> %s" line

    proc.WaitForExit()
    if proc.ExitCode <> 0 then failwithf "Process failed with error %d" proc.ExitCode


[<Test>]
let CheckSourceBuildIsSameAsBinaryBuild () = 
    let expectedFilesMono = [ "libproject.dll"
                              "libproject.dll.mdb"
                              "mainproject.exe"
                              "mainproject.exe.config"
                              "mainproject.exe.mdb"
                              "Zlib.Portable.dll" ] |> Seq.map (fun x -> x.ToLower()) |> Set

    let expectedFilesWindows = [ "libproject.dll"
                                 "libproject.pdb"
                                 "mainproject.exe"
                                 "mainproject.exe.config"
                                 "mainproject.pdb"
                                 "Zlib.Portable.dll" ] |> Seq.map (fun x -> x.ToLower()) |> Set

    let expectedFiles = Env.IsMono() ? (expectedFilesMono, expectedFilesWindows)
                         
    printfn "******************* BUILDING SOURCES ***********************"
    runFB "--verbose view testsrc tests/*"
    runFB "--verbose rebuild testsrc"    

    let wsDir = Env.GetFolder Env.Folder.Workspace
    let graph = Graph.load()
    let fbProject = graph.Projects |> Seq.find (fun x -> x.ProjectId = "mainproject")
    let bin = fbProject.BinFile "net452" "Release"
    let outputBin = wsDir |> FsHelpers.GetFile bin
    let outputFileSrc = outputBin.Directory.EnumerateFiles () |> Seq.map (fun x -> x.Name.ToLower()) |> set
    outputFileSrc |> should equal expectedFiles

    printfn "******************* BUILDING BINARIES ***********************"
    runFB "--verbose view testbin tests/mainproject"
    runFB "--verbose rebuild testbin"
    let outputFileBin = outputBin.Directory.EnumerateFiles () |> Seq.map (fun x -> x.Name.ToLower()) |> set
    outputFileBin |> should equal expectedFiles
