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

module Commands.Unused
open Graph


let listUnusedProjects (graph : Graph) =
    let rootProjects = graph.Applications |> Set.map (fun x -> x.Projects)
                                          |> Set.unionMany

    let allUsedProjects = Project.TransitiveReferences rootProjects
    let projectsUnitTests = allUsedProjects |> Set.map (fun x -> x.ReferencedBy |> Set.filter (fun y -> y.HasTests))
                                            |> Set.unionMany
                                            |> Project.TransitiveReferences
    let unusedProjects = graph.Projects - (allUsedProjects + projectsUnitTests)

    if 0 < unusedProjects.Count then 
        IoHelpers.DisplayHighlight "Unused projects"
        let groupedProjects = unusedProjects |> Seq.groupBy (fun x -> x.Repository)
        for repo, projects in groupedProjects do
            printfn "%s" repo.Name
            for project in projects do            
                printfn "    %s" project.Output.Name
    else
        printfn "No unused projects found"

let List (unusedInfo : CLI.Commands.ListUnused) =
    let antho = Configuration.LoadAnthology()
    let graph = antho |> Graph.from

    if unusedInfo.Project then listUnusedProjects graph
