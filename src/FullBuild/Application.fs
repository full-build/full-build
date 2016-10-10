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

module Application
open Collections
open IoHelpers
open Env
open PatternMatching
open Anthology

let asyncPublish (app : Graph.Application) =
    async {
        DisplayHighlight app.Name
        Publishers.PublishWithPublisher app
    }

let Publish (viewName : string option) (filters : string list) (mt : bool) =
    let graph = Configuration.LoadAnthology () |> Graph.from
    let applications = match viewName with
                       | None -> graph.Applications |> set
                       | Some viewId -> let view = graph.Views |> Seq.find (fun x -> x.Name = viewId)
                                        view.Projects |> set
                                                      |> Set.map (fun x -> x.Applications |> Set)
                                                      |> Set.unionMany

    let appFilters = filters |> Set
    let matchApps filter = applications |> Set.map(fun x -> x.Name)
                                        |> Set.filter (fun x -> PatternMatching.Match x filter)
    let matches = appFilters |> Set.map matchApps
                             |> Set.unionMany

    let apps = applications |> Set.filter (fun x -> matches |> Set.contains x.Name)
                            |> Seq.map asyncPublish

    let maxThrottle = if mt then (System.Environment.ProcessorCount*2) else 1
    apps |> Threading.throttle maxThrottle |> Async.Parallel |> Async.RunSynchronously |> ignore

    let appFolder = Env.GetFolder Env.Folder.AppOutput
    appFolder.EnumerateDirectories(".tmp-*") |> Seq.iter IoHelpers.ForceDelete

let displayApp (app : Anthology.Application) =
    printf "%s [" app.Name.toString
    for project in app.Projects do
        printf "%s " project.toString
    printfn "] => %s" app.Publisher.toString

let List () =
    let antho = Configuration.LoadAnthology ()
    antho.Applications |> Seq.iter displayApp

let Add (appName : ApplicationId) (projects : ProjectId list) (publisher : Anthology.PublisherType) =
    let antho = Configuration.LoadAnthology ()
    let app = { Name = appName
                Publisher = publisher
                Projects = projects |> set }

    let newAntho = { antho
                     with Applications = antho.Applications |> Set.add app }

    Configuration.SaveAnthology newAntho

let Drop (appName : ApplicationId) =
    let antho = Configuration.LoadAnthology ()

    let newAntho = { antho
                     with Applications = antho.Applications |> Set.filter (fun x -> x.Name <> appName) }

    Configuration.SaveAnthology newAntho


let updateProjectBindings (project : Project) =
    let wsDir = Env.GetFolder Env.Folder.Workspace
    let prjFile = wsDir |> GetSubDirectory project.Repository.toString
                        |> GetFile project.RelativeProjectFile.toString
    let prjDir = prjFile.Directory
    Bindings.UpdateProjectBindingRedirects prjDir


let filterClonedRepositories (wsDir : System.IO.DirectoryInfo) (repo : Repository) =
    let repoDir = wsDir |> GetSubDirectory repo.Name.toString
    let exists = repoDir.Exists
    exists


let BindProject (filters : string list) =
    let antho = Configuration.LoadAnthology()
    let wsDir = Env.GetFolder Folder.Workspace
    let prjFilters = filters |> Set

    // select only available repositories
    let availableRepos = antho.Repositories |> Set.map (fun x -> x.Repository)
                                            |> Set.filter (filterClonedRepositories wsDir)
                                            |> Set.map(fun x -> x.Name)

    // build: <repository>/<project>
    let projects = antho.Projects
                   |> Seq.filter (fun x -> availableRepos |> Set.contains x.Repository)
                   |> Seq.map (fun x -> (sprintf "%s/%s" x.Repository.toString x.Output.toString, x.ProjectId))
                   |> Map
    let projectNames = projects |> Seq.map (fun x -> x.Key) |> set

    let matchRepoProject filter =
        projectNames |> Set.filter (fun x -> PatternMatching.Match x filter)

    let matches = prjFilters |> Set.map matchRepoProject
                             |> Set.unionMany
    let selectedProjectIds = projects |> Map.filter (fun k _ -> Set.contains k matches)
                                      |> Seq.map (fun x -> x.Value)
                                      |> Set

    antho.Projects |> Set.filter (fun x -> Set.contains x.ProjectId selectedProjectIds)
                   |> Set.iter updateProjectBindings
