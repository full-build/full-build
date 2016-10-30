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

module Commands.Application
open Collections
open IoHelpers
open Env
open PatternMatching

let private asyncPublish (app : Graph.Application) =
    async {
        DisplayHighlight app.Name
        Core.Publishers.PublishWithPublisher app
    }

let private displayApp (app : Graph.Application) =
    printf "%s [" app.Name
    for project in app.Projects do
        printf "%s " project.Output.Name
    printfn "] => %s" (StringHelpers.toString app.Publisher)

let Publish (pubInfo : CLI.Commands.PublishApplications) =
    let graph = Configuration.LoadAnthology () |> Graph.from
    let viewRepository = Views.from graph
    let applications = match pubInfo.View with
                       | None -> graph.Applications
                       | Some viewId -> let view = viewId |> viewRepository.OpenView
                                        view.Projects |> Set.map (fun x -> x.Applications)
                                                      |> Set.unionMany

    let apps = PatternMatching.FilterMatch applications (fun x -> x.Name) (set pubInfo.Filters)                
    let runApps = apps |> Seq.map asyncPublish

    let maxThrottle = if pubInfo.Multithread then (System.Environment.ProcessorCount*2) else 1
    runApps |> Threading.throttle maxThrottle |> Async.Parallel |> Async.RunSynchronously |> ignore

    let appFolder = Env.GetFolder Env.Folder.AppOutput
    appFolder.EnumerateDirectories(".tmp-*") |> Seq.iter IoHelpers.ForceDelete

let List () =
    let graph = Configuration.LoadAnthology () |>Graph.from
    graph.Applications |> Seq.iter displayApp

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
        