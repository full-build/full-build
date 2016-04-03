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

module View
open Env
open IoHelpers
open Anthology
open Collections
open Solution



let checkErrorCode err =
    if err <> 0 then failwithf "Process failed with error %d" err



let filterClonedRepositories (wsDir : System.IO.DirectoryInfo) (repo : Repository) =
    let repoDir = wsDir |> GetSubDirectory repo.Name.toString
    let exists = repoDir.Exists
    exists

let findViewProjects (viewId : ViewId) =
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
    let projectRefs = match view.SourceOnly with
                      | true -> AnthologyGraph.ComputeProjectSelectionClosureSourceOnly antho.Projects selectedProjectGuids |> Set
                      | _ -> AnthologyGraph.ComputeProjectSelectionClosure antho.Projects selectedProjectGuids |> Set

    let projects = antho.Projects |> Set.filter (fun x -> projectRefs |> Set.contains x.ProjectId)
    projects



let generate (viewId : ViewId) =
    let projects = findViewProjects viewId

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
    let view = Configuration.LoadView viewId
    let builderInfo = view.Parameters |> Seq.fold (+) (sprintf "[%s] " view.Builder.toString)
    printfn "%s" builderInfo
    view.Filters |> Seq.iter (fun x -> printfn "%s" x)


let Graph (viewName : ViewId) (all : bool) =
    let antho = Configuration.LoadAnthology ()
    let projects = findViewProjects viewName |> Set
    let graph = Dgml.GraphContent antho projects all

    let wsDir = Env.GetFolder Env.Workspace
    let graphFile = wsDir |> GetSubDirectory (AddExt Dgml viewName.toString)
    graph.Save graphFile.FullName

let Create (viewId : ViewId) (filters : string list) (all : bool) =
    if filters.Length = 0 then
        failwith "Expecting at least one filter"

    let view = { Filters = filters |> Set
                 Builder = BuilderType.MSBuild
                 Parameters = Set.empty 
                 SourceOnly = all }
    Configuration.SaveView viewId view

    generate viewId

// ---------------------------------------------------------------------------------------


let defaultView () =
    let vwDir = GetFolder Env.View
    let defaultFile = vwDir |> GetFile "default"
    if not defaultFile.Exists then failwith "No default view defined"
    let viewName = System.IO.File.ReadAllText (defaultFile.FullName)
    viewName |> ViewId

let GenerateView (maybeViewName : ViewId option) =
    let viewName = match maybeViewName with
                   | Some x -> x
                   | None -> defaultView()

    let vwDir = Env.GetFolder Env.View 
    let vwFile = vwDir |> GetFile (AddExt View viewName.toString)
    if vwFile.Exists |> not then failwithf "Unknown view name %A" viewName.toString

    let wsDir = Env.GetFolder Env.Workspace
    let viewFile = wsDir |> GetFile (AddExt Solution viewName.toString)

    generate viewName
    viewFile



let AlterView (viewId : ViewId) (isDefault : bool) =
    if isDefault then 
        let vwDir = GetFolder Env.View
        let defaultFile = vwDir |> GetFile "default"
        System.IO.File.WriteAllText (defaultFile.FullName, viewId.toString)

let OpenView (viewId : ViewId) =
    let viewFile = GenerateView (Some viewId)
    Exec.ExecVerb viewFile.FullName "open"


let Build (maybeViewName : ViewId option) (config : string) (clean : bool) (multithread : bool) (version : string option) =
    let viewFile = GenerateView maybeViewName

    let antho = Configuration.LoadAnthology ()
    // TODO: should build with Fake too
    (Builders.BuildWithBuilder BuilderType.MSBuild) viewFile config clean multithread version
