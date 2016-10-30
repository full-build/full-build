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

module Commands.View
open Env
open IoHelpers
open Collections
open Graph

let private GenerateSlnAndTargets (view:Views.View) = 
    let projects = view.Projects
    if projects = Set.empty then printfn "WARNING: empty project selection"
    // generate solution defines
    let slnDefines = Env.GetSolutionDefinesFile view.Name
    let slnDefinesContent = Generators.Solution.GenerateSolutionDefines projects
    SaveFileIfNecessary slnDefines (slnDefinesContent.ToString())
    // generate solution file
    let slnFile = Env.GetSolutionFile view.Name
    let slnContent = Generators.Solution.GenerateSolutionContent projects |> Seq.fold (fun s t -> sprintf "%s%s\n" s t) ""
    SaveFileIfNecessary slnFile slnContent

let Add (cmd : CLI.Commands.AddView) =
    if cmd.Filters.Length = 0 && not cmd.Modified then
        failwith "Expecting at least one filter"
    
    let graph = Configuration.LoadAnthology() |> Graph.from
    let viewRepository = Views.from graph
    let view = viewRepository.CreateView cmd.Name
                                         (cmd.Filters |> Set.ofList)
                                         cmd.DownReferences
                                         cmd.UpReferences
                                         cmd.Modified
                                         cmd.AppFilter
                                         Graph.BuilderType.MSBuild
    // save view information first
    view.Save None
    view |> GenerateSlnAndTargets

let Drop name =
    let graph = Configuration.LoadAnthology() |> Graph.from
    let viewRepository = Views.from graph
    name |> viewRepository.OpenView 
         |> (fun x -> x.Delete())

let Describe name =
    let graph = Configuration.LoadAnthology() |> Graph.from
    let viewRepository = Views.from graph
    let view = name |> viewRepository.OpenView
    let builder = StringHelpers.toString view.Builder
    view.Filters |> Seq.iter (fun x -> printfn "%s" x)

let Build (cmd : CLI.Commands.BuildView) =
    let graph = Configuration.LoadAnthology() |> Graph.from
    let viewRepository = Views.from graph
    let view = match cmd.Name with
               | Some x -> x |> viewRepository.OpenView
               | None -> match viewRepository.DefaultView with
                         | None -> failwith "Can't determine view name"
                         | Some x -> x

    let wsDir = Env.GetFolder Env.Folder.Workspace
    let slnFile = wsDir |> IoHelpers.GetFile (IoHelpers.AddExt IoHelpers.Extension.Solution view.Name)
    Core.Builders.BuildWithBuilder view.Builder slnFile cmd.Config cmd.Clean cmd.Multithread cmd.Version

let Alter (cmd : CLI.Commands.AlterView) =
    let graph = Configuration.LoadAnthology() |> Graph.from
    let viewRepository = Views.from graph
    let view = cmd.Name |> viewRepository.OpenView
    let depView = viewRepository.CreateView view.Name
                                            view.Filters
                                            (cmd.DownReferences = Some true) ? (true, view.DownReferences)
                                            (cmd.UpReferences = Some true) ? (true, view.UpReferences)
                                            view.Modified
                                            view.AppFilter
                                            view.Builder

    let projects = depView.Projects
    if projects = Set.empty then printfn "WARNING: empty project selection"

    depView.Save cmd.Default

let Open (cmd : CLI.Commands.OpenView) =
    let graph = Configuration.LoadAnthology() |> Graph.from
    let viewRepository = Views.from graph
    let view = cmd.Name |> viewRepository.OpenView
    let slnFile = Env.GetSolutionFile view.Name 
    let slnDefinesFile = Env.GetSolutionDefinesFile view.Name 
    if slnFile.Exists |> not || slnDefinesFile.Exists |> not then
        view |> GenerateSlnAndTargets
    Exec.SpawnWithVerb slnFile.FullName "open"

let OpenFullBuildView (cmd : CLI.Commands.FullBuildView) =
    let view = System.IO.FileInfo(cmd.FilePath) |> ViewSerializer.Load
    { CLI.Commands.OpenView.Name = view.Name }  |> Open

let Graph (cmd : CLI.Commands.GraphView) =
    let graph = Configuration.LoadAnthology() |> Graph.from
    let viewRepository = Views.from graph
    let view = cmd.Name |> viewRepository.OpenView
    let projects = view.Projects
    let wsDir = Env.GetFolder Env.Folder.Workspace
    let graphFile = wsDir |> GetSubDirectory (AddExt Dgml cmd.Name)

    let xgraph = Generators.Dgml.GraphContent projects cmd.All
    xgraph.Save graphFile.FullName
