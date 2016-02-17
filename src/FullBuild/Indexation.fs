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

module Indexation
open System.IO
open IoHelpers
open System.Linq
open System.Xml.Linq
open MsBuildHelpers
open Anthology
open Collections

let FindKnownProjects (repoDir : DirectoryInfo) =
    [AddExt CsProj "*"
     AddExt VbProj "*"
     AddExt FsProj "*"] |> Seq.map (fun x -> repoDir.EnumerateFiles (x, SearchOption.AllDirectories)) 
                        |> Seq.concat

let private ProjectCanBeProcessed (fileName : FileInfo) =
    let xdoc = XDocument.Load (fileName.FullName)
    let fbIgnore = !> xdoc.Descendants(NsMsBuild + "FullBuildIgnore").FirstOrDefault() : string
    match bool.TryParse(fbIgnore) with
    | (true, x) -> not <| x
    | _ -> true

let private ParseRepositoryProjects (parser) (repoRef : RepositoryId) (repoDir : DirectoryInfo) =
    repoDir |> FindKnownProjects 
            |> Seq.filter ProjectCanBeProcessed
            |> Seq.map (parser repoDir repoRef)

let ParseWorkspaceProjects (parser) (wsDir : DirectoryInfo) (repos : Repository seq) = 
    repos |> Seq.map (fun x -> GetSubDirectory x.Name.toString wsDir) 
          |> Seq.filter (fun x -> x.Exists) 
          |> Seq.map (fun x -> ParseRepositoryProjects parser (RepositoryId.from(x.Name)) x)
          |> Seq.concat





type ConflictType =
    | SameGuid of Project*Project
    | SameOutput of Project*Project


let FindConflictsForProject (project1 : Project) (otherProjects : Project list) =
    seq {
        for project2 in otherProjects do
            if project1 <> project2 then
                if project1.UniqueProjectId = project2.UniqueProjectId && (project1.Repository <> project2.Repository || project1.RelativeProjectFile <> project2.RelativeProjectFile) then
                    yield SameGuid (project1, project2)
                else if project1.UniqueProjectId <> project2.UniqueProjectId && project1.Output = project2.Output then
                    yield SameOutput (project1, project2)
    }      

let rec FindConflicts (projects : Project list) =
    seq {
        match projects with
        | h :: t -> yield! FindConflictsForProject h t
                    yield! FindConflicts t
        | _ -> ()
    }






let rec DisplayConflicts (conflicts : ConflictType list) =
    let displayConflict (p1 : Project) (p2 : Project) (msg : string) =
        printfn "Conflict detected between projects (%s) : " msg
        printfn " - %s/%s" p1.Repository.toString p1.RelativeProjectFile.toString 
        printfn " - %s/%s" p2.Repository.toString p2.RelativeProjectFile.toString

    match conflicts with
    | SameGuid (p1, p2) :: tail -> displayConflict p1 p2 "same guid"
                                   DisplayConflicts tail

    | SameOutput (p1, p2) :: tail -> displayConflict p1 p2 "same output"
                                     DisplayConflicts tail
    | [] -> ()



let DetectNewDependencies (projects : ProjectParsing.ProjectDescriptor seq) =
    // add new packages (with correct version requirement)
    let foundPackages = projects |> Seq.map (fun x -> x.Packages) 
                                 |> Seq.concat
    let existingPackages = PaketInterface.ParsePaketDependencies ()
    let packagesToAdd = foundPackages |> Seq.filter (fun x -> Set.contains x.Id existingPackages |> not)
                                      |> Seq.distinctBy (fun x -> x.Id)
                                      |> Set
    packagesToAdd



let MergeProjects (projects : ProjectParsing.ProjectDescriptor seq) (existingProjects : Project set) =
    // merge project
    let foundProjects = projects |> Seq.map (fun x -> x.Project) 
                                 |> Set

    let foundProjectGuids = foundProjects |> Set.map (fun x -> x.ProjectId)

    let allProjects = existingProjects |> Set.filter (fun x -> foundProjectGuids |> Set.contains (x.ProjectId) |> not)
                                       |> Set.union foundProjects
                                       |> List.ofSeq
    allProjects




// this function has 2 side effects:
// * update paket.dependencies (both sources and packages)
// * anthology
let IndexWorkspace () = 
    let wsDir = Env.GetFolder Env.Workspace
    let antho = Configuration.LoadAnthology()
    let repos = antho.Repositories |> Set.map (fun x -> x.Repository)
    let projects = ParseWorkspaceProjects ProjectParsing.ParseProject wsDir repos

    let packagesToAdd = DetectNewDependencies projects
    PaketInterface.AppendDependencies packagesToAdd

    let allProjects = MergeProjects projects antho.Projects
    let conflicts = FindConflicts allProjects |> List.ofSeq
    if conflicts <> [] then
        DisplayConflicts conflicts
        failwith "Conflict(s) detected"

    let newAntho = { antho 
                     with Projects = allProjects |> Set.ofList }
    newAntho

let Optimize (newAntho : Anthology) =
    /// BEGIN HACK : here we optimize anthology and dependencies in order to speed up package retrieval after conversion
    ///              warning: big side effect (anthology and paket.dependencies are modified)
    // automaticaly migrate packages to project - this will avoid retrieving them
    let simplifiedAntho = Simplify.SimplifyAnthologyWithoutPackage newAntho
    Configuration.SaveAnthology simplifiedAntho

    // remove unused packages  - this will avoid downloading them for nothing
    let allPackages = PaketInterface.ParsePaketDependencies ()
    let usedPackages = simplifiedAntho.Projects |> Set.map (fun x -> x.PackageReferences)
                                                |> Set.unionMany
    let unusedPackages = Set.difference allPackages usedPackages
    PaketInterface.RemoveDependencies unusedPackages
    /// END HACK

    simplifiedAntho
