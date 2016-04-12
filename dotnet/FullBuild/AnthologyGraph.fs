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

module AnthologyGraph
open Anthology
open Collections

// find all referencing projects of a project
let private referencingProjects (projects : Project set) (current : ProjectId) =
    projects |> Set.filter (fun x -> x.ProjectReferences |> Set.contains current)

// compute all nodes to reach a goal
// current : node we are starting from
// path : all nodes explored yet
// goal : goal to reach
let rec private explorePathUntilGoal (findParents : ProjectId -> Project set) (goal : ProjectId set) (path : ProjectId set) (current : Project) =
    let currentId = current.ProjectId
    let nextPath = path |> Set.add currentId
    if goal |> Set.contains currentId then
        nextPath
    else
        currentId |> findParents
                  |> Set.map (explorePathUntilGoal findParents goal nextPath)
                  |> Set.unionMany

let private computePath (findParents : ProjectId -> Project set) (goal : ProjectId set) (startNode : Project) =
    let startNodeId = startNode.ProjectId
    let goalWithoutStartNode = goal |> Set.remove startNodeId
    explorePathUntilGoal findParents goalWithoutStartNode Set.empty startNode
        |> Set.union goal

let ComputeProjectSelectionClosure (allProjects : Project set) (goal : ProjectId set) =
    let findParents = referencingProjects allProjects

    let seeds = allProjects |> Set.filter (fun x -> goal |> Set.contains x.ProjectId)
    let transitiveClosure = seeds |> Set.map (computePath findParents goal)
                                  |> Set.unionMany
    transitiveClosure


let rec public ComputeProjectSelectionClosureSourceOnly (allProjects : Project set) (goal : ProjectId set) =
    let findParents = referencingProjects allProjects

    let transitiveClosure = allProjects |> Set.map (computePath findParents goal)
                                        |> Set.unionMany
    transitiveClosure


let ComputeRepositoriesDependencies (allProjects : Project set) (selectedRepos : RepositoryId set) =
    let selectedProjects = allProjects |> Set.filter (fun x -> selectedRepos |> Set.contains x.Repository)
                                       |> Set.map (fun x -> x.ProjectId)
    let transitiveProjects = ComputeProjectSelectionClosureSourceOnly allProjects selectedProjects
    let repositories = allProjects |> Set.filter (fun x -> transitiveProjects |> Set.contains x.ProjectId)
                                   |> Set.map (fun x -> x.Repository)
    repositories


