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
open Anthology
open Collections
open IoHelpers
open Env



let Publish (filters : string list) =
    let antho = Configuration.LoadAnthology ()
    let appNames = antho.Applications |> Set.map (fun x -> x.Name.toString)

    let appFilters = filters |> Set
    let matchApps filter = appNames |> Set.filter (fun x -> PatternMatching.Match x filter)
    let matches = appFilters |> Set.map matchApps
                             |> Set.unionMany
                             |> Set.map ApplicationId.from

    for appName in matches do
        let app = antho.Applications |> Seq.find (fun x -> x.Name = appName)
        (Publishers.PublishWithPublisher app.Publisher) app

let List () =
    let antho = Configuration.LoadAnthology ()
    antho.Applications |> Seq.iter (fun x -> printfn "%s" (x.Name.toString))

let Add (appName : ApplicationId) (project : ProjectId) (publisher : Anthology.PublisherType) =
    let antho = Configuration.LoadAnthology ()
    let app = { Name = appName
                Publisher = publisher
                Project = project }

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

let BindProject (prj : ProjectId) =
    let antho = Configuration.LoadAnthology()
    let maybeProject = antho.Projects |> Seq.tryFind (fun x -> x.ProjectId = prj)
    match maybeProject with
    | Some project -> updateProjectBindings project
    | None -> failwithf "Unknown project"
