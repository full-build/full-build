//   Copyright 2014-2015 Pierre Chalamet
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
open System.IO
open Env
open IoHelpers
open Anthology
open Collections
open Solution



let assertViewExists (viewName : ViewId) =
    let vwDir = GetFolder Env.View
    let vwFile = GetFile (AddExt View viewName.toString) vwDir
    if not vwFile.Exists then failwithf "View %A does not exist" viewName.toString


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
    let defaultView = if defaultFile.Exists then File.ReadAllText (defaultFile.FullName)
                      else "default"

    let printViewInfo viewName = 
        let defaultInfo = if viewName = defaultView then "[default]"
                          else ""
        printfn "%s %s" viewName defaultInfo

    vwDir.EnumerateFiles (AddExt  View "*") |> Seq.iter (fun x -> printViewInfo (Path.GetFileNameWithoutExtension x.Name))


let Describe (viewName : ViewId) =
    assertViewExists viewName

    let vwDir = GetFolder Env.View
    let vwFile = vwDir |> GetFile (AddExt View viewName.toString)
    File.ReadAllLines (vwFile.FullName) |> Seq.iter (fun x -> printfn "%s" x)


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

let FindViewProjects (viewName : ViewId) =
    // load back filter & generate view accordingly
    let vwDir = Env.GetFolder Env.View 
    let vwFile = vwDir |> GetFile (AddExt View viewName.toString)
    let filters = File.ReadAllLines(vwFile.FullName)

    let repoFilters = filters |> Set

    // build: <repository>/<project>
    let antho = Configuration.LoadAnthology ()
    let projects = antho.Projects |> Seq.map (fun x -> (sprintf "%s/%s" x.Repository.toString x.Output.toString, x.ProjectId))
                                  |> Map
    let projectNames = projects |> Seq.map (fun x -> x.Key) |> Set

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


let SaveFileIfNecessary (file : FileInfo) (content : string) =
    let overwrite = (file.Exists |> not) || File.ReadAllText(file.FullName) <> content
    if overwrite then
        File.WriteAllText (file.FullName, content)

let Generate (viewName : ViewId) =
    assertViewExists viewName

    let projects = FindViewProjects viewName

    // generate solution defines
    let slnDefines = GenerateSolutionDefines projects
    let viewDir = GetFolder Env.View
    let slnDefineFile = viewDir |> GetFile (AddExt Targets viewName.toString)
    SaveFileIfNecessary slnDefineFile (slnDefines.ToString())

    // generate solution file
    let wsDir = GetFolder Env.Workspace
    let slnFile = wsDir |> GetFile (AddExt Solution viewName.toString)
    let slnContent = GenerateSolutionContent projects |> Seq.fold (fun s t -> sprintf "%s%s\n" s t) ""
    SaveFileIfNecessary slnFile slnContent


let Graph (viewName : ViewId) (all : bool) =
    assertViewExists viewName

    let antho = Configuration.LoadAnthology ()
    let projects = FindViewProjects viewName |> Set
    let graph = Dgml.GraphContent antho projects all

    let wsDir = Env.GetFolder Env.Workspace
    let graphFile = wsDir |> GetSubDirectory (AddExt Dgml viewName.toString)
    graph.Save graphFile.FullName

let Create (viewName : ViewId) (filters : string list) =
    if filters.Length = 0 then
        failwith "Expecting at least one filter"

    let vwDir = Env.GetFolder Env.View 
    let vwFile = vwDir |> GetFile (AddExt View viewName.toString)
    File.WriteAllLines (vwFile.FullName, filters)

    Generate viewName


// ---------------------------------------------------------------------------------------


let defaultView () =
    let vwDir = GetFolder Env.View
    let defaultFile = vwDir |> GetFile "default"
    if not defaultFile.Exists then failwith "No default view defined"
    let viewName = File.ReadAllText (defaultFile.FullName)
    viewName |> ViewId

let AlterView (viewName : ViewId) (isDefault : bool) =
    assertViewExists viewName

    if isDefault then 
        let vwDir = GetFolder Env.View
        let defaultFile = vwDir |> GetFile "default"
        File.WriteAllText (defaultFile.FullName, viewName.toString)


let Build (maybeViewName : ViewId option) (config : string) (clean : bool) (multithread : bool) (version : string) =
    let viewName = match maybeViewName with
                   | Some x -> x
                   | None -> defaultView()

    assertViewExists viewName

    let vwDir = Env.GetFolder Env.View 
    let vwFile = vwDir |> GetFile (AddExt View viewName.toString)
    if vwFile.Exists |> not then failwithf "Unknown view name %A" viewName.toString

    let wsDir = Env.GetFolder Env.Workspace
    let viewFile = wsDir |> GetFile (AddExt Solution viewName.toString)

    Generate viewName

    let antho = Configuration.LoadAnthology ()
    (Builders.BuildWithBuilder antho.Builder) viewFile config clean multithread version
