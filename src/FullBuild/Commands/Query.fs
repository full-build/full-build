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

module Commands.Query
open Graph
open Collections

let private queryUnusedProjects (graph : Graph) =
    let rootProjects = graph.Applications |> Set.map (fun x -> x.Project)
    let allUsedProjects = Project.TransitiveReferences rootProjects
    let projectsUnitTests = allUsedProjects |> Set.map (fun x -> x.ReferencedBy |> Set.filter (fun y -> y.HasTests))
                                            |> Set.unionMany
                                            |> Project.TransitiveReferences
    let unusedProjects = graph.Projects - (allUsedProjects + projectsUnitTests)

    if 0 < unusedProjects.Count then 
        ConsoleHelpers.DisplayInfo "Unused projects"
        let groupedProjects = unusedProjects |> Seq.groupBy (fun x -> x.Repository)
        for repo, projects in groupedProjects do
            printfn "%s" repo.Name
            for project in projects do            
                printfn "    %s" project.Output.Name
    else
        printfn "No unused projects found"


let private queryPackages (projects : Project set) =
    let unittestsprojects = projects |> Set.filter (fun x -> x.HasTests)
    let otherprojects = projects - unittestsprojects

    let packages = otherprojects |> Set.map (fun x -> x.PackageReferences)
                                 |> Set.unionMany

    let packagesunittests = (unittestsprojects |> Set.map (fun x -> x.PackageReferences)
                                              |> Set.unionMany) - packages

    if 0 < packages.Count || 0 < packagesunittests.Count then
        ConsoleHelpers.DisplayInfo "Used packages"
        for package in packages do
            printfn "%s" package.Name

        ConsoleHelpers.DisplayInfo "Used packages in unit tests"
        for package in packagesunittests do
            printfn "%s" package.Name
    else
        printfn "No packages found"

let Query (queryInfo : CLI.Commands.Query) =
    let graph = Graph.load()

    if queryInfo.UnusedProjects then queryUnusedProjects graph

    if queryInfo.UsedPackages then 
        match queryInfo.View with
        | Some viewName -> let views = Views.from graph
                           let view = views.Views |> Seq.find (fun x -> x.Name = viewName)
                           queryPackages view.Projects
        | _ -> queryPackages graph.Projects

    if queryInfo.References then
        let (src, dst) = match (queryInfo.Source, queryInfo.Destination) with
                         | (Some x, Some y) -> (x, y)
                         | (_,_) -> failwithf "Expecting source and destination repositories"

        let (srcName, dstName) = (src.toString, dst.toString)
        let srcRepo = graph.Repositories |> Seq.find (fun x -> x.Name = srcName)
        let dstRepo = graph.Repositories |> Seq.find (fun x -> x.Name = dstName)
        for project in srcRepo.Projects do
            for refProject in project.References do
                if refProject.Repository = dstRepo then
                    printfn "%s -> %s" project.ProjectId refProject.ProjectId

