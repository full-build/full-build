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

module Core.Conversion
open Collections


let private convertMsBuild (repos : Graph.Repository set) =
    let antho = Configuration.LoadAnthology ()
    let projects = antho.Projects |> Set.filter (fun x -> repos |> Set.exists (fun y -> y.Name = x.Repository.toString))
    Generators.MSBuild.GenerateProjects projects IoHelpers.XDocSaver
    Generators.MSBuild.ConvertProjects projects IoHelpers.XDocLoader IoHelpers.XDocSaver
    Generators.MSBuild.RemoveUselessStuff projects

let Convert builder (repos : Graph.Repository set) =
    match builder with
    | Graph.BuilderType.MSBuild -> convertMsBuild repos
    | Graph.BuilderType.Skip -> ()

let GenerateProjectArtifacts () =
    let antho = Configuration.LoadAnthology ()
    let repos = antho.Repositories

    let builder2repos = repos |> Seq.groupBy (fun x -> x.Builder)

    for builder2repo in builder2repos do
        let (builder, brepos) = builder2repo
        let repos = brepos |> Seq.map (fun x -> x.Repository.Name) |> Set.ofSeq
        let projects = antho.Projects |> Set.filter (fun x -> repos |> Set.contains x.Repository)
        Generators.MSBuild.GenerateProjects projects IoHelpers.XDocSaver
