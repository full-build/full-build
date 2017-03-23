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

module Doctor


let private projectConsistencyCheck ((error,antho,globals) : bool * Anthology.Anthology * Anthology.Globals) =
    let projectRefs = antho.Projects |> Set.map (fun x -> x.ProjectReferences) |> Set.unionMany
    let knownProjects = antho.Projects |> Set.map (fun x -> x.ProjectId)
    let unknowns  = projectRefs - knownProjects
    let hasErrors = unknowns <> Set.empty
    if hasErrors then
        let err = unknowns |> Seq.fold (fun s t -> sprintf "%s- %s\n" s t.toString) ""
        IoHelpers.DisplayError "Invalid projects references:"
        printfn "%s" err
    (error || hasErrors, antho, globals)

let private repositoryConsistencyCheck ((error,antho,globals) : bool * Anthology.Anthology * Anthology.Globals) =
    let repoRefs = antho.Projects |> Set.map (fun x -> x.Repository)
    let knownRepos = globals.Repositories |> Set.map (fun x -> x.Repository.Name)
    let unknowns  = repoRefs - knownRepos
    let hasErrors = unknowns <> Set.empty
    if hasErrors then
        let err = unknowns |> Seq.fold (fun s t -> sprintf "%s- %s\n" s t.toString) ""
        IoHelpers.DisplayError "Invalid repositories references:"
        printfn "%s" err
    (error || unknowns <> Set.empty, antho, globals)

let private consistencyCheck antho =
    antho |> projectConsistencyCheck
          |> repositoryConsistencyCheck


// check .fbprojects is available in each buildable repository
let checkFbProjectsInRepo ((error,antho,globals) : bool * Anthology.Anthology * Anthology.Globals) =
    let wsDir = Env.GetFolder Env.Folder.Workspace
    let repoWithoutProjects = globals.Repositories |> Seq.filter (fun x -> x.Builder = Anthology.BuilderType.MSBuild)
                                                   |> Seq.filter (fun x -> (wsDir |> IoHelpers.GetSubDirectory x.Repository.Name.toString).Exists)
                                                   |> Seq.filter (fun x -> (wsDir |> IoHelpers.GetSubDirectory x.Repository.Name.toString |> IoHelpers.GetFile ".fbprojects").Exists |> not)
                                                   |> List.ofSeq

    let hasErrors = repoWithoutProjects <> List.empty
    if hasErrors then
        let err = repoWithoutProjects |> Seq.fold (fun s t -> sprintf "%s- %s\n" s t.Repository.Name.toString) ""
        IoHelpers.DisplayError "Found non indexed repositories:"
        printfn "%s" err
    (error || hasErrors, antho, globals)


let checkApps ((error,antho,globals) : bool * Anthology.Anthology * Anthology.Globals) =
    let appProjects = antho.Applications |> Set.map (fun x -> x.Project)
    let knownProjects = antho.Projects |> Set.map (fun x -> x.ProjectId)
    let unknowns  = appProjects - knownProjects
    let hasErrors = unknowns <> Set.empty
    if hasErrors then
        let err = unknowns |> Seq.fold (fun s t -> sprintf "%s- %s\n" s t.toString) ""
        IoHelpers.DisplayError "Found invalid application references:"
        printfn "%s" err
    (error || hasErrors, antho, globals)


let checkArtifactDir ((error,antho,globals) : bool * Anthology.Anthology * Anthology.Globals) =
    // check artifact directory
    let artifactDir = globals.Binaries |> System.IO.DirectoryInfo
    let hasErrors = artifactDir.Exists |> not
    if hasErrors then
        let err = sprintf "- %s\n" globals.Binaries
        IoHelpers.DisplayError "Artifacts folder is not available:"
        printfn "%s" err
    (error || hasErrors, antho)

let Check () =
    let antho = Configuration.LoadAnthology ()
    let globals = Configuration.LoadGlobals ()
    (false, antho, globals) |> checkFbProjectsInRepo
                            |> checkApps
                            |> consistencyCheck
                            |> checkArtifactDir
                            |> fst
