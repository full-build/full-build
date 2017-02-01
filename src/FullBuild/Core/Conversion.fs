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

module Core.Conversion
open Collections
open Graph


let private convertMsBuild (repos : Repository set) =
    let projects = repos |> Set.filter (fun x -> x.Builder = BuilderType.MSBuild)
                         |> Set.map (fun x -> x.Projects)
                         |> Set.unionMany
    Generators.MSBuild.GenerateProjects projects IoHelpers.XDocSaver
    Generators.MSBuild.ConvertProjects projects IoHelpers.XDocLoader IoHelpers.XDocSaver
    Generators.MSBuild.RemoveUselessStuff projects

let Convert builder (repos : Repository set) =
    match builder with
    | Graph.BuilderType.MSBuild -> convertMsBuild repos
    | Graph.BuilderType.Skip -> ()

let GenerateProjectArtifacts () =
    let graph = Configuration.LoadAnthology () |> Graph.from
    let repos = graph.Repositories

    let builder2repos = repos |> Seq.groupBy (fun x -> x.Builder)
    for (builder, repos) in builder2repos do
        let projects = repos |> Seq.map (fun x -> x.Projects)
                             |> Set.unionMany

        match builder with
        | BuilderType.MSBuild -> Generators.MSBuild.GenerateProjects projects IoHelpers.XDocSaver
        | BuilderType.Skip -> ()
