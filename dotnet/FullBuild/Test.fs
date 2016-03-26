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

module Test
open Env
open System.IO
open Anthology



let TestAssemblies (filters : string list) (excludes : string list) =
    let wsDir = Env.GetFolder Env.Workspace
    let fullPathForProject x = wsDir |> IoHelpers.GetSubDirectory (AnthologyBridge.RelativeProjectFolderFromWorkspace x)
                                     |> IoHelpers.GetSubDirectory "bin"

    let anthology = Configuration.LoadAnthology ()
    let prjNames = anthology.Projects 
                    |> Seq.map (fun x -> (sprintf "%s/%s" x.Repository.toString x.Output.toString, fullPathForProject x))
                    |> Seq.filter (fun (_, y) -> y.Exists)
                    |> Seq.toList

    let matchProjects filter = prjNames |> Seq.filter (fun x -> PatternMatching.Match (fst x) filter)

    let matches = filters |> Seq.map matchProjects
                          |> Seq.collect id
                          |> Seq.map (fun (_,y) -> y.EnumerateFiles("*.test*.dll", SearchOption.AllDirectories))
                          |> Seq.collect id
                          |> Seq.map (fun x -> x.FullName)
                          |> Seq.toList

    match matches.Length with
    | 0 -> printfn "[WARNING] No test found"
    | _ -> (TestRunners.TestWithTestRunner anthology.Tester) matches excludes
