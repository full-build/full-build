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
    let viewFolder = Env.GetFolder Env.Folder.View
    let views = viewFolder.EnumerateFiles ("*" |> IoHelpers.AddExt IoHelpers.Extension.View)
                |> Seq.map (fun x -> Path.GetFileNameWithoutExtension(x.Name))

    let matchViews filter = views |> Seq.filter (fun x -> PatternMatching.Match x filter)

    let matches = filters 
                  |> Seq.map matchViews
                  |> Seq.collect id
                  |> Seq.map (View.findViewProjects << Configuration.LoadView << ViewId.from)
                  |> Set
                  |> Set.unionMany
                  |> Set.filter (fun x -> x.HasTests)
                  |> Set.map (fun x -> sprintf "%s/bin/%s" x.relativeProjectFolderFromWorkspace x.outputFile)

    let anthology = Configuration.LoadAnthology ()
    (TestRunners.TestWithTestRunner anthology.Tester) matches excludes
