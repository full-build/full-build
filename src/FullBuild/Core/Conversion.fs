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
open System.IO
open System.Xml.Linq



let SxSXDocSaver (fileName : FileInfo) (xdoc : XDocument) =
    let newFileName = sprintf "%s-full-build%s"
                            (fileName.FullName |> System.IO.Path.GetFileNameWithoutExtension)
                            fileName.Extension
    let newFile = fileName.Directory |> IoHelpers.GetFile newFileName
    IoHelpers.XDocSaver newFile xdoc

let private convertMsBuild (repos : Repository set) (sxs : bool) =
    let projects = repos |> Set.map (fun x -> x.Projects)
                         |> Set.unionMany
    let projSaver = sxs ? (SxSXDocSaver, IoHelpers.XDocSaver)

    Generators.MSBuild.GenerateProjects projects IoHelpers.XDocSaver
    Generators.MSBuild.ConvertProjects projects IoHelpers.XDocLoader projSaver
    if sxs |> not then Generators.MSBuild.RemoveUselessStuff projects

let Convert builder (repos : Repository set) (sxs : bool) =
    match builder with
    | Graph.BuilderType.MSBuild -> convertMsBuild repos sxs
    | Graph.BuilderType.Skip -> ()

let GenerateProjectArtifacts () =
    let graph = Graph.load()
    let repos = graph.Repositories

    let builder2repos = repos |> Seq.groupBy (fun x -> x.Builder)
    for (builder, repos) in builder2repos do
        let projects = repos |> Seq.map (fun x -> x.Projects)
                             |> Set.unionMany

        match builder with
        | BuilderType.MSBuild -> Generators.MSBuild.GenerateProjects projects IoHelpers.XDocSaver
        | BuilderType.Skip -> ()
