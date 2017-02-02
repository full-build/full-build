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

module ViewTests

open System.IO
open NUnit.Framework
open FsUnit
open StringHelpers
open Generators.Solution
open TestHelpers
open Collections


let loadGraph artifactsFile projectsFile =
    let artifactsFile = FileInfo(testFile artifactsFile)
    let projectsFile = FileInfo(testFile projectsFile)
    let artifacts = ArtifactsSerializer.Load artifactsFile
    let projects = ProjectsSerializer.Load projectsFile
    let anthology = AnthologySerializer.Deserialize artifacts projects
    let graph = Graph.from anthology
    graph



[<Test>]
let CheckGenerateSolution () =
    let graph = loadGraph "graph-artifacts.yaml" "graph-projects.yaml"
    let content = graph.Projects |> set
                                 |> GenerateSolutionContent

    let expectedFile = testFile "anthology-solution.txt"
    let expectedLines = System.IO.File.ReadAllLines expectedFile

    content |> should equal expectedLines





[<Test>]
let CheckSingleProjectSelection () =
    // Dependencies D, G => A, B, C, D, E, F, G
    // 
    //      G
    //     / \
    //    E   F
    //     \ /
    //      C   D
    //     / \ /
    //    A   B
    // 
    let graph = loadGraph "view-artifacts.yaml" "view-projects.yaml"

    let viewRepository = Views.from graph
    let projects = graph.Projects
    let goal = projects |> selectProjects ["g"]

    let view = viewRepository.CreateView "test" (set ["*/g"]) false false false None false

    let projects = view.Projects
    projects |> should equal goal


[<Test>]
let CheckClosureSelection () =
    // A,G => A, C, E, F, G
    // 
    //      G
    //     / \
    //    E   F
    //     \ /
    //      C   D
    //     / \ /
    //    A   B
    // 
    let graph = loadGraph "view-artifacts.yaml" "view-projects.yaml"

    let viewRepository = Views.from graph
    let projects = graph.Projects
    let goal = projects |> selectProjects ["a"; "c"; "e"; "f"; "g"]

    let view = viewRepository.CreateView "test" (set ["*/a"; "*/g"]) false false false None false

    let projects = view.Projects
    projects |> should equal goal


    
[<Test>]
let checkSelectAllDependencies () =
    // G => A, B, C, E, F, G
    // 
    //      G
    //     / \
    //    E   F
    //     \ /
    //      C   D
    //     / \ /
    //    A   B
    // 
    let graph = loadGraph "view-artifacts.yaml" "view-projects.yaml"

    let viewRepository = Views.from graph
    let projects = graph.Projects
    let goal = projects |> selectProjects ["a"; "b"; "c"; "e"; "f"; "g"]

    let view = viewRepository.CreateView "test" (set ["*/g"]) true false false None false

    let projects = view.Projects
    projects |> should equal goal

    
[<Test>]
let CheckAllReferencedBy () =
    // B => C, D, E, F, G
    // 
    //      G
    //     / \
    //    E   F
    //     \ /
    //      C   D
    //     / \ /
    //    A   B
    // 
    let graph = loadGraph "view-artifacts.yaml" "view-projects.yaml"

    let viewRepository = Views.from graph
    let projects = graph.Projects
    let goal = projects |> selectProjects ["b"; "c"; "d"; "e"; "f"; "g"]

    let view = viewRepository.CreateView "test" (set ["*/b"]) false true false None false

    let projects = view.Projects
    projects |> should equal goal



[<Test>]
let CheckSelect2ProjectsWithoutParentButWithCommonChildrenSourceOnly () =
    // Dependencies D, G => A, B, C, D, E, F, G
    // 
    //      G
    //     / \
    //    E   F
    //     \ /
    //      C   D
    //     / \ /
    //    A   B
    // 
    let graph = loadGraph "view-artifacts.yaml" "view-projects.yaml"

    let viewRepository = Views.from graph
    let projects = graph.Projects
    let goal = projects |> selectProjects ["a"; "b"; "c"; "d"; "e"; "f"; "g"]

    let view = viewRepository.CreateView "test" (set ["*/d"; "*/g"]) true false false None false

    let projects = view.Projects
    projects |> should equal goal


[<Test>]
let CheckSelect2LeafProjectsSourceOnly () =
    // Dependencies A, B => A, B
    // 
    //      G
    //     / \
    //    E   F
    //     \ /
    //      C   D
    //     / \ /
    //    A   B
    // 
    let graph = loadGraph "view-artifacts.yaml" "view-projects.yaml"

    let viewRepository = Views.from graph
    let projects = graph.Projects
    let goal = projects |> selectProjects ["a"; "b"]

    let view = viewRepository.CreateView "test" (set ["*/a"; "*/b"]) true false false None false

    let projects = view.Projects
    projects |> should equal goal


[<Test>]
let CheckSelectProjectsWithHoleSourceOnly () =
    // Dependencies A, B => A, B
    // 
    //      G
    //     / \
    //    E   F
    //     \ /
    //      C   D
    //     / \ /
    //    A   B
    // 
    let graph = loadGraph "view-artifacts.yaml" "view-projects.yaml"

    let viewRepository = Views.from graph
    let projects = graph.Projects
    let goal = projects |> selectProjects ["a"; "b"; "c"; "e"; "f"; "g"]

    let view = viewRepository.CreateView "test" (set ["*/a"; "*/g"]) true false false None false

    let projects = view.Projects
    projects |> should equal goal


[<Test>]
let CheckSelectReferencedBy () =
    // ReferencedBy A => A, C, E, F, G
    // 
    //      G
    //     / \
    //    E   F
    //     \ /
    //      C   D
    //     / \ /
    //    A   B
    // 
    let graph = loadGraph "view-artifacts.yaml" "view-projects.yaml"

    let viewRepository = Views.from graph
    let projects = graph.Projects
    let goal = projects |> selectProjects ["a"; "c"; "e"; "f"; "g"]

    let view = viewRepository.CreateView "test" (set ["*/a"]) false true false None false

    let projects = view.Projects
    projects |> should equal goal


[<Test>]
let CheckSelectReferencesAndReferencedBy () =
    // ReferencedBy C => A, B, C, E, F, G
    // 
    //      G
    //     / \
    //    E   F
    //     \ /
    //      C   D
    //     / \ /
    //    A   B
    // 
    let graph = loadGraph "view-artifacts.yaml" "view-projects.yaml"

    let viewRepository = Views.from graph
    let projects = graph.Projects
    let goal = projects |> selectProjects ["a"; "b"; "c"; "e"; "f"; "g"]

    let view = viewRepository.CreateView "test" (set ["*/c"]) true true false None false

    let projects = view.Projects
    projects |> should equal goal

[<Test>]
let CheckSelectFromAppDown () =
    // ReferencedBy E, D => A, B, C, D, E
    // 
    //      G
    //     / \
    //    E   F
    //     \ /
    //      C   D
    //     / \ /
    //    A   B
    // 
    let graph = loadGraph "view-artifacts.yaml" "view-projects.yaml"

    let viewRepository = Views.from graph
    let projects = graph.Projects
    let goal = projects |> selectProjects ["a"; "b"; "c"; "d"; "e"]

    let view = viewRepository.CreateView "test" Set.empty true false false (Some "ed-app*") false

    let projects = view.Projects
    projects |> should equal goal
