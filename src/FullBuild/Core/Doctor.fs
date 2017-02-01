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


let private projectConsistencyCheck (antho : Anthology.Anthology) =
    let projectRefs = antho.Projects |> Set.map (fun x -> x.ProjectReferences) |> Set.unionMany
    let knownProjects = antho.Projects |> Set.map (fun x -> x.ProjectId)
    let unknowns  = projectRefs - knownProjects
    if unknowns <> Set.empty then
        let err = unknowns |> Seq.fold (fun s t -> sprintf "%s\n- %s" s t.toString) "Found invalid project references:"
        failwith err
    antho

let private repositoryConsistencyCheck (antho : Anthology.Anthology) =
    let repoRefs = antho.Projects |> Set.map (fun x -> x.Repository)
    let knownRepos = antho.Repositories |> Set.map (fun x -> x.Repository.Name)
    let unknowns  = repoRefs - knownRepos
    if unknowns <> Set.empty then
        let err = unknowns |> Seq.fold (fun s t -> sprintf "%s\n- %s" s t.toString) "Found invalid repository references:"
        failwith err
    antho

let private consistencyCheck antho = 
    antho |> projectConsistencyCheck
          |> repositoryConsistencyCheck


// check .fbprojects is available in each buildable repository
let checkFbProjectsInRepo (antho : Anthology.Anthology) =
    let wsDir = Env.GetFolder Env.Folder.Workspace
    let repoWithoutProjects = antho.Repositories |> Seq.filter (fun x -> x.Builder = Anthology.BuilderType.MSBuild)
                                                 |> Seq.filter (fun x -> (wsDir |> IoHelpers.GetSubDirectory x.Repository.Name.toString).Exists |> not)
                                                 |> List.ofSeq
    if repoWithoutProjects <> List.empty then
        let err = repoWithoutProjects |> Seq.fold (fun s t -> sprintf "%s\n- %s" s t.Repository.Name.toString) "Found repositories without .fbprojects:"
        failwith err
    antho


let checkArtifactDir (antho : Anthology.Anthology) =
    // check artifact directory
    let artifactDir = antho.Binaries |> System.IO.DirectoryInfo
    if artifactDir.Exists |> not then 
        failwithf "Artifacts directory %A is not available" antho.Binaries
    antho

let Check () =
    let antho = Configuration.LoadAnthology ()
    antho |> checkFbProjectsInRepo
          |> consistencyCheck
          |> checkArtifactDir
          |> ignore
