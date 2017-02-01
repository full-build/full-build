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

type WorkspaceConfiguration = 
    { Repositories : Repository list }

let LoadArtifacts() : ArtifactsSerializer.Artifacts =
    let artifactsFile = GetArtifactsFile ()
    let artifacts = ArtifactsSerializer.Load artifactsFile
    artifacts

let private loadProjects() : Project set =
    let projectsFile = GetProjectsFile ()
    let projects = ProjectsSerializer.Load projectsFile
    projects

let private saveProjectsRepository (repo : RepositoryId) (projects : Project set) =
    let wsDir = Env.GetFolder Env.Folder.Workspace
    let repoDir = wsDir |> IoHelpers.GetSubDirectory repo.toString
    let projectsFile = repoDir |> IoHelpers.GetFile ".fbprojects"
    ProjectsSerializer.Save projectsFile projects    

let private loadProjectsRepository (repo : RepositoryId) : Project set =
    let wsDir = Env.GetFolder Env.Folder.Workspace
    let repoDir = wsDir |> IoHelpers.GetSubDirectory repo.toString
    if repoDir.Exists |> not then failwithf "Can't find .fbprojects in repository %A" repo.toString
    let projectsFile = repoDir |> IoHelpers.GetFile ".fbprojects"
    ProjectsSerializer.Load projectsFile


let LoadAnthology() : Anthology = 
    let artifacts = LoadArtifacts()
    let mutable projects = loadProjects()
    let wsDir = Env.GetFolder Env.Folder.Workspace
    for repo in artifacts.Repositories do
        let repoDir = wsDir |> IoHelpers.GetSubDirectory repo.Repository.Name.toString
        if repoDir.Exists then
            let repoProjects = loadProjectsRepository repo.Repository.Name
            projects <- projects |> Set.filter (fun x -> x.Repository <> repo.Repository.Name)
                                 |> Set.union repoProjects

    let antho = AnthologySerializer.Deserialize artifacts projects
    antho


let SaveAnthology (antho : Anthology) = 
    let (artifacts, projects) = AnthologySerializer.Serialize antho

    let artifactsFile = GetArtifactsFile ()
    ArtifactsSerializer.Save artifactsFile artifacts

    let wsDir = Env.GetFolder Env.Folder.Workspace
    let repo2projects = projects |> Seq.groupBy (fun x -> x.Repository) |> dict
    for repo2project in repo2projects do
        let repo = repo2project.Key
        let repoProjects = repo2project.Value
        let repoDir = wsDir |> IoHelpers.GetSubDirectory repo.toString
        if repoDir.Exists then
            let repoProjects = projects |> Set.filter (fun x -> x.Repository = repo)
            saveProjectsRepository repo repoProjects

let SaveConsolidatedAnthology (antho : Anthology) =
    let (artifacts, projects) = AnthologySerializer.Serialize antho

    let artifactsFile = GetArtifactsFile ()
    ArtifactsSerializer.Save artifactsFile artifacts

    let projectsFile = GetProjectsFile()
    ProjectsSerializer.Save projectsFile projects


let LoadView (viewId :ViewId) : View =
    let viewFile = GetViewFile viewId.toString 
    if not viewFile.Exists then failwithf "View %A does not exist" viewId.toString
    ViewSerializer.Load viewFile

let DefaultView () : ViewId option =
    let vwFolder = Env.GetFolder Env.Folder.View
    let defaultFile = vwFolder |> IoHelpers.GetFile "default"
    if defaultFile.Exists then
        let viewName = System.IO.File.ReadAllText(defaultFile.FullName)
        Some (Anthology.ViewId viewName)
    else    
        None

let DeleteDefaultView() =
    let vwFolder = Env.GetFolder Env.Folder.View
    let defaultFile = vwFolder |> IoHelpers.GetFile "default"
    if defaultFile.Exists then
        defaultFile.Delete()

let DeleteView (viewId : ViewId) =
    let vwDir = Env.GetFolder Env.Folder.View
    let wsDir = Env.GetFolder Env.Folder.Workspace
    let viewFile = vwDir |> IoHelpers.GetFile (IoHelpers.AddExt IoHelpers.Extension.View viewId.toString)
    let targetFile = vwDir |> IoHelpers.GetFile (IoHelpers.AddExt IoHelpers.Extension.Targets viewId.toString)
    let slnFile =  wsDir |> IoHelpers.GetFile (IoHelpers.AddExt IoHelpers.Extension.Solution viewId.toString)
    let defaultFile = vwDir |> IoHelpers.GetFile "default"
    if viewFile.Exists then viewFile.Delete()
    if targetFile.Exists then targetFile.Delete()
    if slnFile.Exists then slnFile.Delete()
    if DefaultView() = Some viewId && defaultFile.Exists then
        defaultFile.Delete()

let private setDefaultView (viewId : ViewId) =
    let vwFolder = Env.GetFolder Env.Folder.View
    let defaultFile = vwFolder |> IoHelpers.GetFile "default"
    System.IO.File.WriteAllText (defaultFile.FullName, viewId.toString)

let ViewExistsAndNotCorrupted viewName =
    [| viewName |> Env.GetSolutionFile
       viewName |> Env.GetSolutionDefinesFile
       viewName |> Env.GetViewFile |] |> Array.forall(fun x -> x.Exists)

let SaveView (viewId : ViewId) view (isDefault : bool option) =
    let viewFile = GetViewFile viewId.toString
    ViewSerializer.Save viewFile view
    match isDefault with
    | None -> ()
    | Some false -> if DefaultView () = Some viewId then DeleteDefaultView()
    | Some true -> setDefaultView viewId

let ViewExists viewName =
    let viewFile = GetViewFile viewName 
    viewFile.Exists

let LoadBranch () : string =
    let file = Env.GetBranchFile ()
    let branch = System.IO.File.ReadAllText(file.FullName)
    branch

let SaveBranch (branch : string) : unit =
    let file = Env.GetBranchFile ()
    System.IO.File.WriteAllText(file.FullName, branch)
