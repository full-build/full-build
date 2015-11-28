module Test
open IoHelpers
open Env
open System.IO
open Anthology




let checkErrorCode err =
    if err <> 0 then failwithf "Process failed with error %d" err

let private checkedExec = 
    Exec.Exec checkErrorCode

let excludeListToArgs (excludes : string list) =
    match excludes with
    | [] -> ""
    | [x] -> let excludeArgs = sprintf "cat != %s" x
             sprintf "--where %A" excludeArgs
    | x :: tail -> let excludeArgs = excludes |> Seq.fold (fun s t -> sprintf "%s && cat != %s" s t) ("")
                   sprintf "--where %A" excludeArgs

let runnerNUnit (matches : string seq) (excludes : string list) =
    let wsDir = GetFolder Env.Workspace
    let files = matches |> Seq.fold (fun s t -> sprintf @"%s %A" s t) ""
    let excludeArgs = excludeListToArgs excludes
    let args = sprintf @"%s %s --noheader ""--result=TestResult.xml;format=nunit2""" files excludeArgs 
    checkedExec "nunit3-console" args wsDir

let testAssembliesWithProvidedRunners (runnerType : TestRunnerType) files nunitRunner =
    let runner = match runnerType with
                 | NUnit -> nunitRunner
    runner files


let TestAssembliesWithRunners (runnerType : TestRunnerType) files =
    testAssembliesWithProvidedRunners runnerType files runnerNUnit


let TestAssemblies (filters : string list) =
    let anthology = Configuration.LoadAnthology ()
    let binDir = GetFolder Env.BinOutput
    let prjNames = anthology.Projects |> Seq.map (fun x -> (sprintf "%s/%s" x.Repository.toString x.Output.toString, binDir |> IoHelpers.GetSubDirectory x.Output.toString))

    let matchProjects filter = prjNames |> Seq.filter (fun x -> PatternMatching.Match (fst x) filter)

    let matches = filters |> Seq.map matchProjects
                          |> Seq.collect id
                          |> Seq.map (fun (_,y) -> y.EnumerateFiles("*.test*.dll", SearchOption.AllDirectories))
                          |> Seq.collect id
                          |> Seq.map (fun x -> x.FullName)

    TestAssembliesWithRunners anthology.Tester matches ["Integration"]
