module Test
open IoHelpers
open Env
open System.IO
open Anthology


let runnerNUnit (matches : string seq) =
    let wsDir = GetFolder Env.Workspace
    let files = matches |> Seq.fold (fun s t -> sprintf @"%s ""%s""" s t) ""
    let args = sprintf @"%s --where ""cat != Integration"" --noheader --result=nunit2" files
    printf "%s" args
    Exec.Exec "nunit3-console" args wsDir




let testAssembliesWithProvidedRunners (runnerType : TestRunner) files nunitRunner =
    let runner = match runnerType with
                 | NUnit -> nunitRunner
    runner files


let TestAssembliesWithRunners (runnerType : TestRunner) files =
    testAssembliesWithProvidedRunners runnerType files runnerNUnit


let TestAssemblies (filters : string list) =
    let binDir = GetFolder Env.BinOutput
    let dirs = binDir.GetDirectories()
    let prjNames = dirs |> Seq.map (fun x -> (x.Name, x))
    let matchProjects filter = prjNames |> Seq.filter (fun x -> PatternMatching.Match (fst x) filter)

    let matches = filters |> Seq.map matchProjects
                          |> Seq.collect id
                          |> Seq.map (fun (_,y) -> y.EnumerateFiles("*.test*.dll", SearchOption.AllDirectories))
                          |> Seq.collect id
                          |> Seq.map (fun x -> x.FullName)

    let anthology = Configuration.LoadAnthology ()
    for testRunner in anthology.TestRunners do
        TestAssembliesWithRunners testRunner matches