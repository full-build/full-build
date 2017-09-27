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

module Commands.Test
open Collections

let TestAssemblies (filters : string set) (excludes : string set) =
    let graph = Graph.load()
    let viewRepository = Views.from graph
    let selectedViews = PatternMatching.FilterMatch viewRepository.Views (fun x -> x.Name) filters

    // first set binding redirects on output only
    let wsDir = Env.GetFolder Env.Folder.Workspace
    let projects = selectedViews |> Set.map (fun x -> x.Projects)
                                 |> Set.unionMany
                                 |> Set.filter (fun x -> x.HasTests)

    // then test assemblies
    let runner2assemblies = projects |> Seq.groupBy (fun x -> x.Repository.Tester)
    for runner, assemblies in runner2assemblies do
        let bins = assemblies |> Seq.map (fun x -> x.BinFile) |> set
        (Core.TestRunners.TestWithTestRunner runner) bins excludes
