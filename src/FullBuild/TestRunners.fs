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

module TestRunners
open Anthology
open Env




let checkErrorCode err =
    if err < 0 then failwithf "Process failed with error %d" err

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

let runnerFake (matches : string seq) (excludes : string list) =
    ()

let chooseTestRunner (runnerType : TestRunnerType) nunitRunner fakeRunner =
    let runner = match runnerType with
                 | TestRunnerType.NUnit -> nunitRunner
                 | TestRunnerType.Fake -> fakeRunner
    runner

let TestWithTestRunner (runnerType : TestRunnerType) =
    chooseTestRunner runnerType runnerNUnit runnerFake
