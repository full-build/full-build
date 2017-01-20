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

module Commands.Application
open Collections
open IoHelpers
open Env
open PatternMatching

let private asyncPublish (version : string) (app : Graph.Application) =
    async {
        DisplayHighlight app.Name
        Core.Publishers.PublishWithPublisher version app
    }

let private displayApp (app : Graph.Application) =
    printfn "%s" app.Name

let private displayAppVersion ((app : Graph.Application), (tag : Baselines.TagInfo)) =
    let tag = tag.Format()
    printfn "%s : %s" app.Name tag


let private checkAppHasVersion (version : string) (graph : Graph.Graph) (app : Graph.Application) =
    async {
        let version = Core.BuildArtifacts.FetchVersionsForArtifact graph app |> List.tryFind (fun x -> x.Format().Contains(version))                                                                                                       
        return if version.IsSome then Some app
               else None
    }

let private getLastVersionForApp (graph : Graph.Graph) (app : Graph.Application) =
    async {
        let versions = Core.BuildArtifacts.FetchVersionsForArtifact graph app
        return match versions |> List.tryLast with
               | None -> None
               | Some version -> Some (app, version)
    }

let Publish (pubInfo : CLI.Commands.PublishApplications) =
    let graph = Configuration.LoadAnthology () |> Graph.from
    let baselines = Baselines.from graph
    let baseline = baselines.Baseline
    let tagInfo = baseline.Info
    let version = tagInfo.Format()

    let viewRepository = Views.from graph
    let applications = match pubInfo.View with
                       | None -> graph.Applications
                       | Some viewId -> let view = viewRepository.Views |> Seq.find (fun x -> x.Name = viewId)
                                        view.Projects |> Set.map (fun x -> x.Applications)
                                                      |> Set.unionMany

    let apps = PatternMatching.FilterMatch applications (fun x -> x.Name) (set pubInfo.Filters)
    let maxThrottle = if pubInfo.Multithread then (System.Environment.ProcessorCount*2) else 1
    apps |> Seq.map (asyncPublish version)
         |> Threading.throttle maxThrottle |> Async.Parallel |> Async.RunSynchronously 
         |> ignore

    let appFolder = Env.GetFolder Env.Folder.AppOutput
    appFolder.EnumerateDirectories(".tmp-*") |> Seq.iter IoHelpers.ForceDelete

    // copy bin content
    if pubInfo.Push then
        let baselineRepository = Baselines.from graph
        let newBaseline = baselineRepository.Baseline
        Core.BuildArtifacts.Publish graph newBaseline.Info

let List (appInfo : CLI.Commands.ListApplications) =
    let graph = Configuration.LoadAnthology () |> Graph.from
    match appInfo.Version with
    | None -> let maxThrottle = System.Environment.ProcessorCount*4
              graph.Applications |> Seq.filter (fun (x : Graph.Application) -> x.Publisher = Graph.PublisherType.Zip)
                                 |> Seq.map (getLastVersionForApp graph)
                                 |> Threading.throttle maxThrottle |> Async.Parallel |> Async.RunSynchronously
                                 |> Seq.choose id
                                 |> Seq.iter displayAppVersion
    | Some version -> let maxThrottle = System.Environment.ProcessorCount*4
                      graph.Applications |> Seq.filter (fun (x : Graph.Application) -> x.Publisher = Graph.PublisherType.Zip)
                                         |> Seq.map (checkAppHasVersion version graph)
                                         |> Threading.throttle maxThrottle |> Async.Parallel |> Async.RunSynchronously
                                         |> Seq.choose id
                                         |> Seq.iter displayApp

let Add (addInfo : CLI.Commands.AddApplication) =
    let graph = Configuration.LoadAnthology () |> Graph.from
    let projects = PatternMatching.FilterMatch graph.Projects (fun x -> x.Output.Name) addInfo.Projects
    let newGraph = graph.CreateApp addInfo.Name addInfo.Publisher projects
    newGraph.Save()

let Drop (appName : string) =
    let graph = Configuration.LoadAnthology () |> Graph.from
    let app = graph.Applications |> Seq.find (fun x -> x.Name = appName)
    let newGraph = app.Delete()
    newGraph.Save()

let BindProject (bindInfo : CLI.Commands.BindProject) =
    let graph = Configuration.LoadAnthology() |> Graph.from
    let wsDir = Env.GetFolder Folder.Workspace

    // select only available repositories
    let availableProjects = graph.Repositories |> Set.filter (fun x -> x.IsCloned)
                                               |> Set.map (fun x -> x.Projects)
                                               |> Set.unionMany

    let projects = PatternMatching.FilterMatch availableProjects (fun x -> sprintf "%s/%s" x.Repository.Name x.Output.Name) bindInfo.Filters
    projects |> Set.iter(fun project ->
                            printfn "Binding %s/%s" project.Repository.Name project.ProjectId
                            project |> Core.Bindings.UpdateProjectBindingRedirects)
