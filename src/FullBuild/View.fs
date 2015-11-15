// Copyright (c) 2014-2015, Pierre Chalamet
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//     * Redistributions of source code must retain the above copyright
//       notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright
//       notice, this list of conditions and the following disclaimer in the
//       documentation and/or other materials provided with the distribution.
//     * Neither the name of Pierre Chalamet nor the
//       names of its contributors may be used to endorse or promote products
//       derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL PIERRE CHALAMET BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
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

let Generate (viewName : ViewId) =
    let projects = FindViewProjects viewName

    // generate solution defines
    let slnDefines = GenerateSolutionDefines projects
    let viewDir = GetFolder Env.View
    let slnDefineFile = viewDir |> GetFile (AddExt Targets viewName.toString)
    slnDefines.Save (slnDefineFile.FullName)

    // generate solution file
    let wsDir = GetFolder Env.Workspace
    let slnFile = wsDir |> GetFile (AddExt Solution viewName.toString)
    let slnContent = GenerateSolutionContent projects
    File.WriteAllLines (slnFile.FullName, slnContent)

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

let ExternalBuild (config : string) (target : string) (viewFile : FileInfo) =
    let wsDir = Env.GetFolder Env.Workspace
    //let args = sprintf "/nologo /p:Configuration=%s /v:m %A" config viewFile.Name
    let args = sprintf "/nologo /t:%s /p:Configuration=%s %A" target config viewFile.Name

    if Env.IsMono () then Exec.Exec "xbuild" args wsDir
    else Exec.Exec "msbuild" args wsDir

let Build (name : ViewId) (config : string) (forceRebuild : bool) =
    let target = if forceRebuild then "Rebuild"
                 else "Build"

    let vwDir = Env.GetFolder Env.View 
    let vwFile = vwDir |> GetFile (AddExt View name.toString)
    if vwFile.Exists |> not then failwithf "Unknown view name %A" name.toString

    let wsDir = Env.GetFolder Env.Workspace
    let viewFile = wsDir |> GetFile (AddExt Solution name.toString)
    let shouldRefresh = viewFile.Exists || vwFile.CreationTime > viewFile.CreationTime
    if shouldRefresh then name |> Generate

    viewFile |> ExternalBuild config target
