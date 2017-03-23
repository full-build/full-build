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

module Configuration

open Env
open Collections
open Anthology
open System.IO

type WorkspaceConfiguration =
    { Repositories : Repository list }

let LoadGlobals() : Globals =
    let artifactsFile = GetGlobalsFile ()
    let artifacts = GlobalsSerializer.Load artifactsFile
    artifacts

let SaveGlobals (globals : Globals) =
    let artifactsFile = GetGlobalsFile ()
    GlobalsSerializer.Save artifactsFile globals

let LoadAnthology() : Anthology =
    let globalsFile = GetGlobalsFile()
    let globals = GlobalsSerializer.Load globalsFile

    // load global anthology and override with local anthologies
    let globalAnthologyFile = GetGlobalAnthologyFile()
    let mutable globalAnthology = if globalAnthologyFile.Exists then AnthologySerializer.Load globalAnthologyFile
                                  else { Applications = Set.empty ; Projects = Set.empty }

    for repo in globals.Repositories do
        let localAnthologyFile = GetLocalAnthologyFile repo.Repository.Name
        if localAnthologyFile.Exists then
            let localAnthology = AnthologySerializer.Load localAnthologyFile
            globalAnthology <- { globalAnthology
                                 with Projects = globalAnthology.Projects |> Set.filter (fun x -> x.Repository <> repo.Repository.Name)
                                                                          |> Set.union localAnthology.Projects
                                      Applications = globalAnthology.Applications |> Set.filter (fun x -> localAnthology.Applications |> Set.exists (fun y -> x.Name = y.Name) |> not)
                                                                                  |> Set.union localAnthology.Applications }

    globalAnthology


let SaveConsolidatedAnthology (antho : Anthology) =
    let artifactsFile = GetGlobalsFile ()
    AnthologySerializer.Save artifactsFile antho


let SaveAnthology (antho : Anthology) =
    let wsDir = Env.GetFolder Env.Folder.Workspace
    let repo2projects = antho.Projects |> Seq.groupBy (fun x -> x.Repository) |> dict
    let repo2apps = antho.Applications |> Seq.groupBy (fun x -> antho.Projects |> Seq.find (fun y -> y.ProjectId = x.Project) |> (fun x -> x.Repository))
                                       |> dict
    for repo2project in repo2projects do
        let repo = repo2project.Key
        let repoDir = wsDir |> IoHelpers.GetSubDirectory repo.toString
        if repoDir.Exists then
            let localProjects = repo2project.Value |> set
            let localApps = match repo2apps.TryGetValue(repo2project.Key) with
                            | true, x -> x |> Set.ofSeq
                            | false, _ -> Set.empty
            let localAntho = { Projects = localProjects
                               Applications = localApps }
            let localAnthologyFile = GetLocalAnthologyFile repo
            AnthologySerializer.Save localAnthologyFile localAntho
