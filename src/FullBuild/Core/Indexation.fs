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

module Core.Indexation
open System.IO
open IoHelpers
open System.Linq
open System.Xml.Linq
open XmlHelpers
open Anthology
open Collections

let private projectCanBeProcessed (fileName : FileInfo) =
    let xdoc = XDocument.Load (fileName.FullName)
    let fbIgnore = !> xdoc.Descendants(NsMsBuild + "FullBuildIgnore").FirstOrDefault() : string
    match bool.TryParse(fbIgnore) with
    | (true, x) -> not <| x
    | _ -> true

let private parseRepositoryProjects (parser) (repoRef : RepositoryId) (repoDir : DirectoryInfo) =
    repoDir |> IoHelpers.FindKnownProjects
            |> Seq.filter projectCanBeProcessed
            |> Seq.map (parser repoDir repoRef)

let private printParseStatus (repoDir : DirectoryInfo) =
    let repo = RepositoryId.from(repoDir.Name)
    IoHelpers.DisplayHighlight repo.toString
    repoDir

let private parseWorkspaceProjects parser (wsDir : DirectoryInfo) (repos : Repository seq) =
    repos |> Seq.map (fun x -> GetSubDirectory x.Name.toString wsDir)
          |> Seq.filter (fun x -> x.Exists)
          |> Seq.map printParseStatus
          |> Seq.map (fun x -> parseRepositoryProjects parser (RepositoryId.from(x.Name)) x)
          |> Seq.concat
          |> Seq.toList


// NOTE: should be private
type ConflictType =
    | SameGuid of Project*Project
    | SameOutput of Project*Project


let private findConflictsForProject (project1 : Project) (otherProjects : Project list) =
    seq {
        for project2 in otherProjects do
            if project1 <> project2 then
                if project1.UniqueProjectId = project2.UniqueProjectId && (project1.Repository <> project2.Repository || project1.RelativeProjectFile <> project2.RelativeProjectFile) then
                    yield SameGuid (project1, project2)
                else if project1.UniqueProjectId <> project2.UniqueProjectId && project1.Output = project2.Output then
                    yield SameOutput (project1, project2)
    }

// NOTE: should be private
let rec findConflicts (projects : Project list) =
    seq {
        match projects with
        | h :: t -> yield! findConflictsForProject h t
                    yield! findConflicts t
        | _ -> ()
    }






let rec private displayConflicts (conflicts : ConflictType list) =
    let displayConflict (p1 : Project) (p2 : Project) (msg : string) =
        printfn "Conflict detected between projects (%s) : " msg
        printfn " - %s/%s" p1.Repository.toString p1.RelativeProjectFile.toString
        printfn " - %s/%s" p2.Repository.toString p2.RelativeProjectFile.toString

    match conflicts with
    | SameGuid (p1, p2) :: tail -> displayConflict p1 p2 "same guid"
                                   displayConflicts tail

    | SameOutput (p1, p2) :: tail -> displayConflict p1 p2 "same output"
                                     displayConflicts tail
    | [] -> ()



let private detectNewDependencies (projects : Parsers.MSBuild.ProjectDescriptor seq) =
    // add new packages (with correct version requirement)
    let foundPackages = projects |> Seq.map (fun x -> x.Packages)
                                 |> Seq.concat
    let existingPackages = Tools.Paket.ParsePaketDependencies ()
    let packagesToAdd = foundPackages |> Seq.filter (fun x -> Set.contains x.Id existingPackages |> not)
                                      |> Seq.distinctBy (fun x -> x.Id)
                                      |> Set
    packagesToAdd



let MergeProjects (newProjects : Project set) (existingProjects : Project set) =
    // this is the repositories we are dealing with
    let foundRepos = newProjects |> Seq.map (fun x -> x.Repository)
                                 |> Set

    // this is the projects that will be removed from current anthology
    let removedProjectIds = existingProjects |> Set.filter (fun x -> foundRepos |> Set.contains x.Repository)
                                             |> Set.map (fun x -> x.ProjectId)

    // the new projects
    let newProjectIds = newProjects |> Seq.map (fun x -> x.ProjectId)
                                    |> Set

    // what will be chopped
    let reallyRemovedProjects = Set.difference removedProjectIds newProjectIds

    // projects that won't be touched
    let remainingProjects = existingProjects |> Set.filter (fun x -> foundRepos |> Set.contains x.Repository |> not)

    // ensure no pending references on deleted projects
    let referencesOnRemovedProjects = remainingProjects |> Set.map (fun x -> Set.intersect x.ProjectReferences reallyRemovedProjects)
                                                        |> Set.unionMany
    if (referencesOnRemovedProjects <> Set.empty) then
        printfn "Failure to deleted still referenced projects:"
        for referenceOnRemovedProject in referencesOnRemovedProjects do
            printfn "  %s" referenceOnRemovedProject.toString
        failwithf "Failure to deleted still referenced projects"

    // we can safely replace now
    Set.union remainingProjects newProjects


// WARNING: paket.dependencies modified
let IndexWorkspace (grepos : Graph.Repository set) =
    let wsDir = Env.GetFolder Env.Folder.Workspace
    let antho = Configuration.LoadAnthology()
    let repos = antho.Repositories |> Set.filter (fun x -> grepos |> Set.exists (fun y -> y.Name = x.Repository.Name.toString))
                                   |> Set.map (fun x -> x.Repository)
    let parsedProjects = parseWorkspaceProjects Parsers.MSBuild.ParseProject wsDir repos

    let projects = parsedProjects |> Seq.map (fun x -> x.Project)
                                  |> Set
    let allProjects = MergeProjects projects antho.Projects |> Set.toList
    let conflicts = findConflicts allProjects |> List.ofSeq
    if conflicts <> [] then
        displayConflicts conflicts
        failwith "Conflict(s) detected"

    let anthoWithIndexedProjects = { antho
                                     with Projects = allProjects |> Set.ofList }

    let newAntho = Core.Simplify.SimplifyAnthologyWithoutPackage anthoWithIndexedProjects

    /// here we optimize anthology and dependencies in order to speed up package retrieval after conversion
    /// warning: big side effect (paket.dependencies is modified)
    // automaticaly migrate packages to project - this will avoid retrieving them
    // remove unused packages  - this will avoid downloading them for nothing
    let packagesToAdd = detectNewDependencies parsedProjects
    Tools.Paket.AppendDependencies packagesToAdd

    let allPackages = Tools.Paket.ParsePaketDependencies ()
    let usedPackages = antho.Projects |> Set.map (fun x -> x.PackageReferences)
                                      |> Set.unionMany
    let unusedPackages = allPackages - usedPackages
    Tools.Paket.RemoveDependencies unusedPackages

    // if changes then install packages
    if packagesToAdd <> Set.empty || unusedPackages <> Set.empty then
        Core.Package.InstallPackages newAntho.NuGets

    newAntho

let SaveAnthologyProjects (previousAntho : Anthology) (repos : Graph.Repository set) (antho : Anthology) =
    let untouchedPreviousProjects = previousAntho.Projects |> Set.filter (fun x -> repos |> Set.exists (fun y -> y.Name = x.Repository.toString) |> not)
    let untouchedProjects = antho.Projects |> Set.filter (fun x -> repos |> Set.exists (fun y -> y.Name = x.Repository.toString) |> not)

    if untouchedPreviousProjects <> untouchedProjects then
        printfn "Missing repositories for indexation"
        let prevGroups = untouchedPreviousProjects |> Seq.groupBy (fun x -> x.Repository)
                                                   |> Seq.map (fun (repo, prjs) -> repo, prjs |> set)
                                                   |> dict
        let currGroups = untouchedProjects |> Seq.groupBy (fun x -> x.Repository)
                                           |> Seq.map (fun (repo, prjs) -> repo, prjs |> set)
                                           |> dict

        for kvp in prevGroups do
            let prevProjects = kvp.Value
            let currProjects = currGroups.[kvp.Key]
            if prevProjects <> currProjects then
                printfn "%s" kvp.Key.toString

        failwithf "Missing repositories for indexation"

    antho
