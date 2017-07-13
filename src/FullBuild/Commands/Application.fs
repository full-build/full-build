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

let private asyncPublish (version : string) (app : Graph.Application) =
    async {
        try
            DisplayInfo app.Name
            Core.Publishers.PublishWithPublisher version app
        with
            exn -> raise (System.ApplicationException(sprintf "Failed to publish application %A" app.Name, exn))
    }

let private displayApp (app : Graph.Application) =
    printfn "%s" app.Name

let private displayAppVersion ((app : Graph.Application), (buildInfo : Baselines.BuildInfo)) =
    let tag = buildInfo.Format()
    printfn "%s : %s" app.Name tag


let private checkAppHasVersion (version : string) (graph : Graph.Graph) (app : Graph.Application) =
    async {
        let version = Core.BuildArtifacts.FetchVersionsForArtifact graph app |> List.tryFind (fun x -> x.Format().Contains(version))
        return if version.IsSome then Some app
               else None
    }

let private getLastVersionForApp (graph : Graph.Graph) (branch : string) (app : Graph.Application) =
    async {
        let versions = Core.BuildArtifacts.FetchVersionsForArtifact graph app
                            |> List.filter (fun x -> x.Branch = branch)
        return match versions |> List.tryLast with
               | None -> None
               | Some version -> Some (app, version)
    }

let Publish (pubInfo : CLI.Commands.PublishApplications) =
    let graph = Graph.load()

    // build a semver version
    let buildNumber = match pubInfo.Version with
                      | Some x -> x
                      | None -> "0.0.0"
    let status = match pubInfo.Status with
                 | None -> ""
                 | Some x -> sprintf "-%s" x
    let version = sprintf "%s%s" buildNumber status

    let viewRepository = Views.from graph
    let applications = match pubInfo.View with
                       | None -> graph.Applications
                       | Some viewId -> let view = viewRepository.Views |> Seq.find (fun x -> x.Name = viewId)
                                        view.Projects |> Set.map (fun x -> x.Applications)
                                                      |> Set.unionMany

    let apps = PatternMatching.FilterMatch applications (fun x -> x.Name) (set pubInfo.Filters)
    apps |> Threading.ParExec (asyncPublish version)
         |> ignore

    let appFolder = Env.GetFolder Env.Folder.AppOutput
    appFolder.EnumerateDirectories(".tmp-*") |> Seq.iter IoHelpers.ForceDelete

let List (appInfo : CLI.Commands.ListApplications) =
    let graph = Graph.load()
    let branch = Configuration.LoadBranch()
    match appInfo.Version with
    | None -> graph.Applications |> Seq.filter (fun (x : Graph.Application) -> x.Publisher = Graph.PublisherType.Zip)
                                 |> Threading.ParExec (getLastVersionForApp graph branch)
                                 |> Seq.choose id
                                 |> Seq.iter displayAppVersion
    | Some version -> graph.Applications |> Seq.filter (fun (x : Graph.Application) -> x.Publisher = Graph.PublisherType.Zip)
                                         |> Threading.ParExec (checkAppHasVersion version graph)
                                         |> Seq.choose id
                                         |> Seq.iter displayApp

let Add (addInfo : CLI.Commands.AddApplication) =
    let graph = Graph.load()
    let projects = PatternMatching.FilterMatch graph.Projects (fun x -> x.Output.Name) addInfo.Projects
    if projects.Count <> 1 then failwith "Selection leads to more than one project"
    let project = projects |> Seq.exactlyOne
    let newGraph = graph.CreateApp addInfo.Name addInfo.Publisher project
    newGraph.Save()

let Drop (appName : string) =
    let graph = Graph.load()
    let app = graph.Applications |> Seq.find (fun x -> x.Name = appName)
    let newGraph = app.Delete()
    newGraph.Save()

let BindProject (bindInfo : CLI.Commands.BindProject) =
    let graph = Graph.load()
    let wsDir = Env.GetFolder Folder.Workspace

    // select only available repositories
    let availableProjects = graph.Repositories |> Set.filter (fun x -> x.IsCloned)
                                               |> Set.map (fun x -> x.Projects)
                                               |> Set.unionMany

    let projects = PatternMatching.FilterMatch availableProjects (fun x -> sprintf "%s/%s" x.Repository.Name x.Output.Name) bindInfo.Filters
    projects |> Set.iter(fun project ->
                            printfn "Binding %s/%s" project.Repository.Name project.ProjectId
                            project |> Core.Bindings.UpdateProjectBindingRedirects)
