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

module Commands.Test
open Collections

let TestAssemblies (cmd : CLI.Commands.TestAssemblies) =
    let graph = Configuration.LoadAnthology() |> Graph.from
    let viewRepository = Views.from graph
    let selectedView = cmd.Views |> Seq.map(viewRepository.OpenView) 

    // first set binding redirects on output only
    let wsDir = Env.GetFolder Env.Folder.Workspace
    let projects = selectedView |> Seq.map(fun x -> x.Projects)
                                |> Set.unionMany
                                |> Set.filter (fun x -> x.HasTests)
    let artifactDirs = projects |> Set.map (fun x -> sprintf "%s/%s" x.Repository.Name x.ProjectFile)
                                |> Seq.map (fun x -> wsDir |> IoHelpers.GetFile x)
                                |> Seq.map (fun x -> x.Directory)
                                |> Seq.map (fun x -> x |> IoHelpers.GetSubDirectory "bin")
    artifactDirs |> Seq.iter Core.Bindings.UpdateArtifactBindingRedirects

    // then test assemblies
    let assemblies = projects |> Set.map (fun x -> x.BinFile)
    (Core.TestRunners.TestWithTestRunner graph.TestRunner) assemblies cmd.Excludes
    