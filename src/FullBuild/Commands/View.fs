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
//open Anthology
open Collections
open Graph

//let FindViewApplications viewId =
//    let antho = Configuration.LoadAnthology ()
//    let viewProjectIds = viewId |> Configuration.LoadView 
//                              |> FindViewProjects 
//                              |> Set.map(fun x->x.ProjectId)
//    antho.Applications |> Set.filter(fun x -> x.Projects |> Set.intersect viewProjectIds <> Set.empty)
//
//
//let generate (viewId : ViewId) (view : View) =
//    let legacyProjects = FindViewProjects view
//
//    // HACK BEGIN
//    let graph = Configuration.LoadAnthology() |> Graph.from
//    let projects = graph.Projects |> Seq.filter (fun x -> legacyProjects |> Set.exists (fun y -> y.ProjectId.toString = x.ProjectId))
//                                   |> set
//    // HACK END
//
//    // generate solution defines
//    let slnDefines = GenerateSolutionDefines projects
//    let viewDir = GetFolder Env.Folder.View
//    let slnDefineFile = viewDir |> GetFile (AddExt Targets viewId.toString)
//    SaveFileIfNecessary slnDefineFile (slnDefines.ToString())
//
//    // generate solution file
//    let wsDir = GetFolder Env.Folder.Workspace
//    let slnFile = wsDir |> GetFile (AddExt Solution viewId.toString)
//    let slnContent = GenerateSolutionContent projects |> Seq.fold (fun s t -> sprintf "%s%s\n" s t) ""
//    SaveFileIfNecessary slnFile slnContent
//
//let Drop (viewName : ViewId) =
//    let vwDir = GetFolder Env.Folder.View
//    let vwFile = vwDir |> GetFile (AddExt View viewName.toString)
//    if vwFile.Exists then vwFile.Delete()
//
//    let vwDefineFile = vwDir |> GetFile (AddExt Targets viewName.toString)
//    if vwDefineFile.Exists then vwDefineFile.Delete()
//
//    let wsDir = GetFolder Env.Folder.Workspace
//    let slnFile = wsDir |> GetFile (AddExt Solution viewName.toString)
//    if slnFile.Exists then slnFile.Delete()
//
//let List () =
//    let vwDir = GetFolder Env.Folder.View
//    let defaultFile = vwDir |> GetFile "default"
//    let defaultView = if defaultFile.Exists then System.IO.File.ReadAllText (defaultFile.FullName)
//                      else ""
//
//    let printViewInfo viewName =
//        let defaultInfo = if viewName = defaultView then "[default]"
//                          else ""
//        printfn "%s %s" viewName defaultInfo
//
//    vwDir.EnumerateFiles (AddExt View "*") |> Seq.iter (fun x -> printViewInfo (System.IO.Path.GetFileNameWithoutExtension x.Name))
//
//
//let Describe (viewId : ViewId) =
//    let view = Configuration.LoadView viewId
//    let builderInfo = view.Parameters |> Seq.fold (+) (sprintf "[%s] " view.Builder.toString)
//    printfn "%s" builderInfo
//    view.Filters |> Seq.iter (fun x -> printfn "%s" x)
//
//
//let Graph (viewId : ViewId) (all : bool) =
//    let view = Configuration.LoadView viewId
//    let legacyProjects = FindViewProjects view |> Set
//
//    // HACK BEGIN
//    let graph = Configuration.LoadAnthology() |> Graph.from
//    let projects = graph.Projects |> Seq.filter (fun x -> legacyProjects |> Set.exists (fun y -> y.ProjectId.toString = x.ProjectId))
//                                  |> set
//    // HACK END
//
//    let graph = Dgml.GraphContent projects all
//
//    let wsDir = Env.GetFolder Env.Folder.Workspace
//    let graphFile = wsDir |> GetSubDirectory (AddExt Dgml viewId.toString)
//    graph.Save graphFile.FullName
//
//
//
//let Create (viewId : ViewId) (filters : string list) (forceSrc : bool) (forceParents : bool) (modified : bool) =
//
//let CreatePending (viewId : ViewId) =
//    let getPendingRepositories () = seq {
//        let antho = Configuration.LoadAnthology()
//        let baseline = Configuration.LoadBaseline()
//        let wsDir = Env.GetFolder Env.Folder.Workspace
//    
//        for bookmark in baseline.Bookmarks do
//            let repoDir = wsDir |> GetSubDirectory bookmark.Repository.toString
//            if repoDir.Exists then
//                let repo = antho.Repositories |> Seq.find (fun x -> x.Repository.Name = bookmark.Repository)
//                let revision = Vcs.Log antho.Vcs wsDir repo.Repository bookmark.Version
//                if revision <> null then
//                    yield repo.Repository
//    }
//    let modifiedReposFilter = getPendingRepositories () |> Seq.map(fun x -> x.Name.toString |> sprintf "%s/*") |> Seq.toList
//    Create viewId modifiedReposFilter false true true
//
//// ---------------------------------------------------------------------------------------
//
//
//let defaultView () =
//    let vwDir = GetFolder Env.Folder.View
//    let defaultFile = vwDir |> GetFile "default"
//    if not defaultFile.Exists then failwith "No default view defined"
//    let viewName = System.IO.File.ReadAllText (defaultFile.FullName)
//    viewName |> ViewId
//
//let getViewName (maybeViewName : ViewId option) =
//    let viewName = match maybeViewName with
//                   | Some x -> x
//                   | None -> defaultView()
//    viewName
//
//let getViewFile (view : ViewId) =
//    let vwDir = Env.GetFolder Env.Folder.View
//    let vwFile = vwDir |> GetFile (AddExt View view.toString)
//    if vwFile.Exists |> not then failwithf "Unknown view name %A" view.toString
//
//    let wsDir = Env.GetFolder Env.Folder.Workspace
//    let viewFile = wsDir |> GetFile (AddExt Solution view.toString)
//    viewFile
//
//let GenerateView (viewId : ViewId) =
//    let view = Configuration.LoadView viewId
//    generate viewId view
//
//
//let AlterView (viewId : ViewId) (forceDefault : bool option) (forceSource : bool option) (forceParents : bool option) =
//    match forceDefault with               
//    | Some true ->  let vwDir = GetFolder Env.Folder.View
//                    let defaultFile = vwDir |> GetFile "default"
//                    System.IO.File.WriteAllText (defaultFile.FullName, viewId.toString)
//    | _ -> ()
//
//    let mutable view = Configuration.LoadView viewId
//    match forceSource with
//    | Some source -> view <- { view with SourceOnly = source }
//    | _ -> ()
//
//    match forceParents with
//    | Some parents -> view <- { view with Parents = parents }
//    | _ -> ()
//
//    Configuration.SaveView viewId view
//    GenerateView viewId
//
//
//let OpenView (viewId : ViewId) =
//    let view = Configuration.LoadView viewId
//    let viewFile = getViewFile viewId
//    Exec.SpawnWithVerb viewFile.FullName "open"
//
//
//let Build (maybeViewName : ViewId option) (config : string) (clean : bool) (multithread : bool) (version : string option) =
//    let viewId = getViewName maybeViewName
//    let viewFile = getViewFile viewId
//    Builders.BuildWithBuilder (global.Graph.BuilderType.MSBuild) viewFile config clean multithread version
//
//
//


//let computeBaselineDifferences (oldBaseline : Graph.Baseline) (newBaseline : Graph.Baseline) =
//    let changes = Set.difference newBaseline.Bookmarks oldBaseline.Bookmarks
//    let projects = changes |> Set.map (fun x -> x.Repository.Projects)
//                           |> Set.unionMany
//    projects

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

//    let modProjects = if cmd.Modified then computeBaselineDifferences graph.Baseline (graph.CreateBaseline false)
//                      else Set.empty
//    let viewProjects = GraphHelpers.ComputeClosure (view.Projects + modProjects)
//    let depProjects = if cmd.References then GraphHelpers.ComputeTransitiveReferences viewProjects  
//                      else Set.empty
//    let refProjects = if cmd.ReferencedBy then GraphHelpers.ComputeTransitiveReferencedBy viewProjects
//                      else Set.empty
//    let projects = viewProjects + depProjects + refProjects + modProjects
    let projects = view.Projects

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
    Plumbing.Builders.BuildWithBuilder view.Builder slnFile cmd.Config cmd.Clean cmd.Multithread cmd.Version

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
    let projects = view.Projects |> set
    let wsDir = Env.GetFolder Env.Folder.Workspace
    let graphFile = wsDir |> GetSubDirectory (AddExt Dgml cmd.Name)

    let xgraph = Generators.Dgml.GraphContent projects cmd.All
    xgraph.Save graphFile.FullName
