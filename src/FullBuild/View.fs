﻿//   Copyright 2014-2016 Pierre Chalamet
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

module View
open Env
open IoHelpers
open Anthology
open Collections
open Solution


//let assertViewExists (viewName : ViewId) =
//    let vwDir = GetFolder Env.View
//    let vwFile = GetFile (AddExt View viewName.toString) vwDir
//    if not vwFile.Exists then failwithf "View %A does not exist" viewName.toString


let Drop (viewName : ViewId) =
    let vwDir = GetFolder Env.View
    let vwFile = vwDir |> GetFile (AddExt View viewName.toString)
    if vwFile.Exists then vwFile.Delete()

    let vwDefineFile = vwDir |> GetFile (AddExt Targets viewName.toString)
    if vwDefineFile.Exists then vwDefineFile.Delete()

    let wsDir = GetFolder Env.Workspace
    let slnFile = wsDir |> GetFile (AddExt Solution viewName.toString)
    if slnFile.Exists then slnFile.Delete()

let List () =
    let vwDir = GetFolder Env.View
    let defaultFile = vwDir |> GetFile "default"
    let defaultView = if defaultFile.Exists then System.IO.File.ReadAllText (defaultFile.FullName)
                      else ""

    let printViewInfo viewName = 
        let defaultInfo = if viewName = defaultView then "[default]"
                          else ""
        printfn "%s %s" viewName defaultInfo

    vwDir.EnumerateFiles (AddExt View "*") |> Seq.iter (fun x -> printViewInfo (System.IO.Path.GetFileNameWithoutExtension x.Name))


let Describe (viewId : ViewId) =
    let vwDir = GetFolder Env.View
    let view = Configuration.LoadView viewId
    let builderInfo = view.Parameters |> Seq.fold (+) (sprintf "[%s] " view.Builder.toString)
    printfn "%s" builderInfo
    view.Filters |> Seq.iter (fun x -> printfn "%s" x)

// find all referencing projects of a project
let private referencingProjects (projects : Project set) (current : ProjectId) =
    projects |> Set.filter (fun x -> x.ProjectReferences |> Set.contains current)

let rec private computePaths (findParents : ProjectId -> Project set) (goal : ProjectId set) (path : ProjectId set) (current : Project) =
    let currentId = current.ProjectId
    let parents = findParents currentId
    let newPath = Set.add currentId path
    let paths = parents |> Set.map (computePaths findParents goal (Set.add currentId path))
                        |> Set.unionMany
    if Set.contains currentId goal then Set.union newPath paths
    else paths

let ComputeProjectSelectionClosure (allProjects : Project set) (goal : ProjectId set) =
    let findParents = referencingProjects allProjects

    let seeds = allProjects |> Set.filter (fun x -> Set.contains (x.ProjectId) goal)
    let transitiveClosure = seeds |> Set.map (computePaths findParents goal Set.empty)
                                  |> Set.unionMany
    transitiveClosure

let filterClonedRepositories (wsDir : System.IO.DirectoryInfo) (repo : Repository) =
    let repoDir = wsDir |> GetSubDirectory repo.Name.toString
    let exists = repoDir.Exists
    exists

let FindViewProjects (viewId : ViewId) =
    // load back filter & generate view accordingly
    let wsDir = Env.GetFolder Folder.Workspace
    let view = Configuration.LoadView viewId
    let repoFilters = view.Filters |> Set

    let antho = Configuration.LoadAnthology ()

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

    let matches = repoFilters |> Set.map matchRepoProject
                              |> Set.unionMany
    let selectedProjectGuids = projects |> Map.filter (fun k _ -> Set.contains k matches)
                                        |> Seq.map (fun x -> x.Value)
                                        |> Set

    // find projects
    let antho = Configuration.LoadAnthology ()
    let projectRefs = ComputeProjectSelectionClosure antho.Projects selectedProjectGuids |> Set
    let projects = antho.Projects |> Set.filter (fun x -> projectRefs |> Set.contains x.ProjectId)
    projects



let Generate (viewId : ViewId) =
    let projects = FindViewProjects viewId

    // generate solution defines
    let slnDefines = GenerateSolutionDefines projects
    let viewDir = GetFolder Env.View
    let slnDefineFile = viewDir |> GetFile (AddExt Targets viewId.toString)
    SaveFileIfNecessary slnDefineFile (slnDefines.ToString())

    // generate solution file
    let wsDir = GetFolder Env.Workspace
    let slnFile = wsDir |> GetFile (AddExt Solution viewId.toString)
    let slnContent = GenerateSolutionContent projects |> Seq.fold (fun s t -> sprintf "%s%s\n" s t) ""
    SaveFileIfNecessary slnFile slnContent


let Graph (viewName : ViewId) (all : bool) =
    let antho = Configuration.LoadAnthology ()
    let projects = FindViewProjects viewName |> Set
    let graph = Dgml.GraphContent antho projects all

    let wsDir = Env.GetFolder Env.Workspace
    let graphFile = wsDir |> GetSubDirectory (AddExt Dgml viewName.toString)
    graph.Save graphFile.FullName

let Create (viewId : ViewId) (filters : string list) =
    if filters.Length = 0 then
        failwith "Expecting at least one filter"

    let view = { Filters = filters |> Set
                 Builder = BuilderType.MSBuild
                 Parameters = Set.empty }
    Configuration.SaveView viewId view

    Generate viewId

// ---------------------------------------------------------------------------------------


let defaultView () =
    let vwDir = GetFolder Env.View
    let defaultFile = vwDir |> GetFile "default"
    if not defaultFile.Exists then failwith "No default view defined"
    let viewName = System.IO.File.ReadAllText (defaultFile.FullName)
    viewName |> ViewId

let AlterView (viewId : ViewId) (isDefault : bool) =
    if isDefault then 
        let vwDir = GetFolder Env.View
        let defaultFile = vwDir |> GetFile "default"
        System.IO.File.WriteAllText (defaultFile.FullName, viewId.toString)




let Build (maybeViewName : ViewId option) (config : string) (clean : bool) (multithread : bool) (version : string) =
    let viewName = match maybeViewName with
                   | Some x -> x
                   | None -> defaultView()

    let vwDir = Env.GetFolder Env.View 
    let vwFile = vwDir |> GetFile (AddExt View viewName.toString)
    if vwFile.Exists |> not then failwithf "Unknown view name %A" viewName.toString

    let wsDir = Env.GetFolder Env.Workspace
    let viewFile = wsDir |> GetFile (AddExt Solution viewName.toString)

    Generate viewName

    let antho = Configuration.LoadAnthology ()
    // TODO: should build with Fake too
    (Builders.BuildWithBuilder BuilderType.MSBuild) viewFile config clean multithread version
