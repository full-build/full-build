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

let Add (cmd : CLI.Commands.AddView) =
    if cmd.Filters.Length = 0 && not cmd.Modified then
        failwith "Expecting at least one filter"
    
    let graph = Configuration.LoadAnthology() |> Graph.from
    let view = graph.CreateView cmd.Name
                                (cmd.Filters |> Set.ofList)
                                Set.empty
                                cmd.References
                                cmd.ReferencedBy
                                cmd.Modified
                                Graph.BuilderType.MSBuild

    let projects = view.Projects
    if projects = Set.empty then printfn "WARNING: empty project selection"

    // save view information first
    view.Save None

    // generate solution defines
    let slnDefines = Generators.Solution.GenerateSolutionDefines projects
    let viewDir = GetFolder Env.Folder.View
    let slnDefineFile = viewDir |> GetFile (AddExt Targets view.Name)
    SaveFileIfNecessary slnDefineFile (slnDefines.ToString())

    // generate solution file
    let wsDir = GetFolder Env.Folder.Workspace
    let slnFile = wsDir |> GetFile (AddExt Solution view.Name)
    let slnContent = Generators.Solution.GenerateSolutionContent projects |> Seq.fold (fun s t -> sprintf "%s%s\n" s t) ""
    SaveFileIfNecessary slnFile slnContent

let Drop name =
    let graph = Configuration.LoadAnthology() |> Graph.from
    let view = graph.Views |> Seq.tryFind (fun x -> x.Name = name)
    match view with 
    | Some x -> x.Delete()
    | None -> ()

let List () =
    let graph = Configuration.LoadAnthology() |> Graph.from
    let views = graph.Views
    let defaultView = graph.DefaultView

    let printViewInfo view =
        let defaultInfo = (defaultView = Some view) ? ("[default]", "")
        printfn "%s %s" view.Name defaultInfo

    views |> Seq.iter printViewInfo

let Describe name =
    let graph = Configuration.LoadAnthology() |> Graph.from
    let view = graph.Views |> Seq.find (fun x -> x.Name = name)
    let builder = StringHelpers.toString view.Builder
    let builderInfo = view.Parameters |> Seq.fold (+) (sprintf "[%s] " builder)
    printfn "%s" builderInfo
    view.Filters |> Seq.iter (fun x -> printfn "%s" x)

let Build (cmd : CLI.Commands.BuildView) =
    let graph = Configuration.LoadAnthology() |> Graph.from
    let view = match cmd.Name with
               | Some x -> graph.Views |> Seq.find (fun y -> y.Name = x)
               | None -> match graph.DefaultView with
                         | None -> failwith "Can't determine view name"
                         | Some x -> x

    let wsDir = Env.GetFolder Env.Folder.Workspace
    let slnFile = wsDir |> IoHelpers.GetFile (IoHelpers.AddExt IoHelpers.Extension.Solution view.Name)
    Tools.Builders.BuildWithBuilder view.Builder slnFile cmd.Config cmd.Clean cmd.Multithread cmd.Version

let Alter (cmd : CLI.Commands.AlterView) =
    let graph = Configuration.LoadAnthology() |> Graph.from
    let view = graph.Views |> Seq.find (fun x -> x.Name = cmd.Name)
    let depView = graph.CreateView view.Name
                                   view.Filters
                                   view.Parameters
                                   (cmd.Source = Some true) ? (true, view.References)
                                   (cmd.Parents = Some true) ? (true, view.ReferencedBy)
                                   view.Modified
                                   view.Builder

    let projects = depView.Projects
    if projects = Set.empty then printfn "WARNING: empty project selection"

    depView.Save cmd.Default

let Open (cmd : CLI.Commands.OpenView) =
    let graph = Configuration.LoadAnthology() |> Graph.from
    let view = graph.Views |> Seq.find (fun x -> x.Name = cmd.Name)
    let wsDir = Env.GetFolder Env.Folder.Workspace
    let slnFile = wsDir |> IoHelpers.GetFile (IoHelpers.AddExt IoHelpers.Extension.Solution view.Name)
    Exec.SpawnWithVerb slnFile.FullName "open"

let Graph (cmd : CLI.Commands.GraphView) =
    let graph = Configuration.LoadAnthology() |> Graph.from
    let view = graph.Views |> Seq.find (fun x -> x.Name = cmd.Name)
    let projects = view.Projects
    let wsDir = Env.GetFolder Env.Folder.Workspace
    let graphFile = wsDir |> GetSubDirectory (AddExt Dgml cmd.Name)

    let xgraph = Generators.Dgml.GraphContent projects cmd.All
    xgraph.Save graphFile.FullName
