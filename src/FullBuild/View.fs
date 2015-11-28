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
open StringHelpers
open System.Xml.Linq
open MsBuildHelpers
open Configuration
open Collections
open Solution


let checkErrorCode err =
    if err <> 0 then failwithf "Process failed with error %d" err

let private checkedExec = 
    Exec.Exec checkErrorCode



let Drop (viewName : ViewId) =
    let vwDir = GetFolder Env.View
    let vwFile = GetFile (AddExt View viewName.toString) vwDir
    vwFile.Delete()

    let vwDefineFile = GetFile (AddExt Targets viewName.toString) vwDir
    vwDefineFile.Delete()

    let wsDir = Env.GetFolder Env.Workspace
    let viewFile = GetFile (AddExt Solution viewName.toString) wsDir
    viewFile.Delete ()

let List () =
    let vwDir = GetFolder Env.View
    vwDir.EnumerateFiles (AddExt  View "*") |> Seq.iter (fun x -> printfn "%s" (Path.GetFileNameWithoutExtension (x.Name)))

let Describe (viewName : ViewId) =
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

let builderMSBuild (config : string) (target : string) (viewFile : FileInfo) =
    let wsDir = Env.GetFolder Env.Workspace
    //let args = sprintf "/nologo /p:Configuration=%s /v:m %A" config viewFile.Name
    let args = sprintf "/nologo /t:%s /p:Configuration=%s %A" target config viewFile.Name

    if Env.IsMono () then checkedExec "xbuild" args wsDir
    else checkedExec "msbuild" args wsDir


let buildWithProvidedBuilder (builderType : BuilderType) config target viewFile msbuildBuilder =
    let builder = match builderType with
                  | BuilderType.MSBuild -> msbuildBuilder

    builder config target viewFile


let buildWithBuilder (builder : BuilderType) config target viewFile =
    buildWithProvidedBuilder builder config target viewFile builderMSBuild


let Build (name : ViewId) (config : string) (forceRebuild : bool) =
    let vwDir = Env.GetFolder Env.View 
    let vwFile = vwDir |> GetFile (AddExt View name.toString)
    if vwFile.Exists |> not then failwithf "Unknown view name %A" name.toString

    let wsDir = Env.GetFolder Env.Workspace
    let viewFile = wsDir |> GetFile (AddExt Solution name.toString)

    Generate name

    let target = if forceRebuild then "Rebuild"
                 else "Build"

    if forceRebuild then
        let binDir = wsDir |> GetSubDirectory Env.MSBUILD_BIN_OUTPUT
        if binDir.Exists then binDir.Delete (true)

    let antho = Configuration.LoadAnthology ()
    buildWithBuilder antho.Builder config target viewFile

