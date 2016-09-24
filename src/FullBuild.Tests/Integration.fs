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
let CheckBuildSources () = 
    runFB "view testsrc tests/*"
    runFB "rebuild --mt testsrc"

    runFB "view testbin tests/mainproject"
    runFB "rebuild --mt testbin"
