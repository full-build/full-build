module Integration

open FsUnit
open NUnit.Framework
open Anthology
open StringHelpers
open System.IO
open System.Diagnostics


let runFB (args : string) =
    printfn "==== running fullbuild %s" args
    let runDir = TestContext.CurrentContext.TestDirectory + "/../../../" 
    let fb = TestContext.CurrentContext.TestDirectory + "/../../../src/FullBuild/bin/fullbuild.exe"
    let psi = ProcessStartInfo (FileName = fb, Arguments = args, UseShellExecute = false, WorkingDirectory = runDir, LoadUserProfile = true, CreateNoWindow = true, RedirectStandardOutput = true)
    use proc = Process.Start (psi)
    use stdout = proc.StandardOutput
    while stdout.EndOfStream |> not do
        let line = stdout.ReadLine()
        printfn "> %s" line

    proc.WaitForExit()
    if proc.ExitCode <> 0 then failwithf "Process failed with error %d" proc.ExitCode

[<Test>]
let CheckSourceBuildIsSameAsBinaryBuild () = 
    let expectedFiles = [ "libproject.dll"
                          "libproject.pdb"
                          "mainproject.exe"
                          "mainproject.exe.config"
                          "mainproject.pdb"
                          "Mono.Cecil.dll"
                          "Mono.Cecil.Mdb.dll"
                          "Mono.Cecil.Pdb.dll"
                          "Mono.Cecil.Rocks.dll" ] |> set

    runFB "view testsrc tests/*"
    runFB "rebuild --mt testsrc"    

    let outputDir = TestContext.CurrentContext.TestDirectory + "/../../../tests/MainProject/bin" |> DirectoryInfo
    let outputFileSrc = outputDir.EnumerateFiles () |> Seq.map (fun x -> x.Name) |> set
    outputFileSrc |> should equal expectedFiles

    runFB "view testbin tests/mainproject"
    runFB "rebuild --mt testbin"
    let outputFileBin = outputDir.EnumerateFiles () |> Seq.map (fun x -> x.Name) |> set
    outputFileBin |> should equal expectedFiles
