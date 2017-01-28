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
    let currFolder = TestContext.CurrentContext.TestDirectory
    try
        System.Environment.CurrentDirectory <- TestContext.CurrentContext.TestDirectory
        let wsDir = Env.GetFolder Env.Folder.Workspace
        let fb = wsDir |> IoHelpers.GetFile  "src/FullBuild/bin/fullbuild.exe"
        let psi = ProcessStartInfo (FileName = fb.FullName, Arguments = args, UseShellExecute = false, WorkingDirectory = wsDir.FullName, LoadUserProfile = true, CreateNoWindow = true, RedirectStandardOutput = true)
        use proc = Process.Start (psi)
        use stdout = proc.StandardOutput
        while stdout.EndOfStream |> not do
            let line = stdout.ReadLine()
            printfn "> %s" line

        proc.WaitForExit()
        if proc.ExitCode <> 0 then failwithf "Process failed with error %d" proc.ExitCode
    finally
        System.Environment.CurrentDirectory <- currFolder


[<Test>]
let CheckSourceBuildIsSameAsBinaryBuild () = 
    let expectedFilesMono = [ "libproject.dll"
                              "libproject.dll.mdb"
                              "mainproject.exe"
                              "mainproject.exe.config"
                              "mainproject.exe.mdb"
                              "Zlib.Portable.dll" 
                              "Zlib.Portable.xml" ] |> set

    let expectedFilesWindows = [ "libproject.dll"
                                 "libproject.pdb"
                                 "mainproject.exe"
                                 "mainproject.exe.config"
                                 "mainproject.pdb"
                                 "Zlib.Portable.dll" 
                                 "Zlib.Portable.xml" ] |> set

    let expectedFiles = Env.IsMono() ? (expectedFilesMono, expectedFilesWindows)
                         
    runFB "view testsrc tests/*"
    runFB "rebuild testsrc"    

    let outputDir = TestContext.CurrentContext.TestDirectory + "/../../../tests/MainProject/bin" |> DirectoryInfo
    let outputFileSrc = outputDir.EnumerateFiles () |> Seq.map (fun x -> x.Name) |> set
    outputFileSrc |> should equal expectedFiles

    runFB "view testbin tests/mainproject"
    runFB "rebuild testbin"
    let outputFileBin = outputDir.EnumerateFiles () |> Seq.map (fun x -> x.Name) |> set
    outputFileBin |> should equal expectedFiles
