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

let Query (queryInfo : CLI.Commands.Query) =
    let graph = Graph.load()

    if queryInfo.UnusedProjects then queryUnusedProjects graph

    if queryInfo.References then
        let (src, dst) = match (queryInfo.Source, queryInfo.Destination) with
                         | (Some x, Some y) -> (x, y)
                         | (_,_) -> failwithf "Expecting source and destination repositories"

        let (srcName, dstName) = (src.toString, dst.toString)

        let srcFilter = srcName |> Set.singleton
        let srcProjects = PatternMatching.FilterMatch graph.Repositories (fun x -> x.Name) srcFilter
                            |> Set.map (fun x -> x.Projects)
                            |> Set.unionMany

        let dstFilter = dstName |> Set.singleton
        let dstRepos = PatternMatching.FilterMatch graph.Repositories (fun x -> x.Name) dstFilter
        for project in srcProjects do
            for refProject in project.References do
                if dstRepos |> Set.contains refProject.Repository then
                    printfn "%s -> %s" project.ProjectId refProject.ProjectId

    if queryInfo.Cycle then
        let repos = graph.Repositories |> Array.ofSeq
        for repo in repos do
            let seeds = repo |> Set.singleton
            match Algorithm.FindCycle seeds (fun x -> x.Name) (fun x -> x.References) (fun x -> x.ReferencedBy) with
            | Some path -> printfn "%s: %s" repo.Name path
            | None -> ()
      
