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

module ViewTests

open System.IO
open NUnit.Framework
open FsUnit
open Anthology
open StringHelpers
open Solution
open TestHelpers

[<Test>]
let CheckSelectSubProject () =
    // 
    //      G
    //     / \
    //    E   F
    //     \ /
    //      C   D
    //     / \ /
    //    A   B
    // 
    let projectDefs = [ "A", "4c116d6d-22ff-4b9c-80fd-de0e6d0a96b6", [] 
                        "B", "386c73d8-95dc-4684-ba6c-20f4cd63e42a", []
                        "C", "2904bc7b-8b30-41f1-8160-02b5281704b4", ["4c116d6d-22ff-4b9c-80fd-de0e6d0a96b6"; "386c73d8-95dc-4684-ba6c-20f4cd63e42a"]
                        "D", "209eab33-f903-4195-bc2d-03d086129168", ["386c73d8-95dc-4684-ba6c-20f4cd63e42a"]
                        "E", "d7b81c18-45df-44dc-853d-8cab07e1ad97", ["2904bc7b-8b30-41f1-8160-02b5281704b4"]
                        "F", "78c2e0d4-b410-4702-af93-71db7db228d0", ["2904bc7b-8b30-41f1-8160-02b5281704b4"] 
                        "G", "eb5c2f2b-d117-47b0-8067-305b4bae9aa2", ["d7b81c18-45df-44dc-853d-8cab07e1ad97"; "78c2e0d4-b410-4702-af93-71db7db228d0"] ]

    let createProject (name, id, refs) = 
        let refIds = refs |> Seq.map ProjectId.from |> Set
        { Repository = RepositoryId.from name
          ProjectId = ProjectId.from id
          RelativeProjectFile = ProjectRelativeFile (sprintf "%s.csproj" name)
          UniqueProjectId = ProjectUniqueId.from (ParseGuid id)
          Output = AssemblyId.from name
          OutputType = OutputType.Dll
          FxVersion = FxInfo.from "v4.5"
          FxProfile = FxInfo.from null
          FxIdentifier = FxInfo.from null
          HasTests = false
          AssemblyReferences = Set.empty
          PackageReferences = Set.empty
          ProjectReferences = refIds }

    let projects = projectDefs |> Seq.map createProject |> Set
    // goal is A & G
    let goal = ["4c116d6d-22ff-4b9c-80fd-de0e6d0a96b6" |> ProjectId.from
                "eb5c2f2b-d117-47b0-8067-305b4bae9aa2" |> ProjectId.from ] |> Set
    let projects = AnthologyGraph.ComputeProjectSelectionClosure projects goal |> Set

    // expect A, C, E, F & G
    projects |> Set.count |> should equal 5
    projects |> should contain (ProjectId.from "4c116d6d-22ff-4b9c-80fd-de0e6d0a96b6") // A
    projects |> should contain (ProjectId.from "2904bc7b-8b30-41f1-8160-02b5281704b4") // C
    projects |> should contain (ProjectId.from "d7b81c18-45df-44dc-853d-8cab07e1ad97") // E
    projects |> should contain (ProjectId.from "78c2e0d4-b410-4702-af93-71db7db228d0") // F
    projects |> should contain (ProjectId.from "eb5c2f2b-d117-47b0-8067-305b4bae9aa2") // G
    

[<Test>]
let CheckSelectAllProject () =
    // 
    //      G
    //     / \
    //    E   F
    //     \ /
    //      C   D
    //     / \ /
    //    A   B
    // 
    let projectDefs = [ "A", "4c116d6d-22ff-4b9c-80fd-de0e6d0a96b6", [] 
                        "B", "386c73d8-95dc-4684-ba6c-20f4cd63e42a", []
                        "C", "2904bc7b-8b30-41f1-8160-02b5281704b4", ["4c116d6d-22ff-4b9c-80fd-de0e6d0a96b6"; "386c73d8-95dc-4684-ba6c-20f4cd63e42a"]
                        "D", "209eab33-f903-4195-bc2d-03d086129168", ["386c73d8-95dc-4684-ba6c-20f4cd63e42a"]
                        "E", "d7b81c18-45df-44dc-853d-8cab07e1ad97", ["2904bc7b-8b30-41f1-8160-02b5281704b4"]
                        "F", "78c2e0d4-b410-4702-af93-71db7db228d0", ["2904bc7b-8b30-41f1-8160-02b5281704b4"] 
                        "G", "eb5c2f2b-d117-47b0-8067-305b4bae9aa2", ["d7b81c18-45df-44dc-853d-8cab07e1ad97"; "78c2e0d4-b410-4702-af93-71db7db228d0"] ]

    let createProject (name, id, refs) = 
        let refIds = refs |> Seq.map ProjectId.from |> Set
        { Repository = RepositoryId.from name
          ProjectId = ProjectId.from id
          RelativeProjectFile = ProjectRelativeFile (sprintf "%s.csproj" name)
          UniqueProjectId = ProjectUniqueId.from (ParseGuid id)
          Output = AssemblyId.from name
          OutputType = OutputType.Dll
          FxVersion = FxInfo.from "v4.5"
          FxProfile = FxInfo.from null
          FxIdentifier = FxInfo.from null
          HasTests = false
          AssemblyReferences = Set.empty
          PackageReferences = Set.empty
          ProjectReferences = refIds }

    let projects = projectDefs |> Seq.map createProject |> Set
    // goal is A & G
    let goal = ["4c116d6d-22ff-4b9c-80fd-de0e6d0a96b6" |> ProjectId.from
                "386c73d8-95dc-4684-ba6c-20f4cd63e42a" |> ProjectId.from
                "2904bc7b-8b30-41f1-8160-02b5281704b4" |> ProjectId.from
                "209eab33-f903-4195-bc2d-03d086129168" |> ProjectId.from
                "d7b81c18-45df-44dc-853d-8cab07e1ad97" |> ProjectId.from
                "78c2e0d4-b410-4702-af93-71db7db228d0" |> ProjectId.from
                "eb5c2f2b-d117-47b0-8067-305b4bae9aa2" |> ProjectId.from ] |> Set
    let projects = AnthologyGraph.ComputeProjectSelectionClosure projects goal |> Set

    // expect all nodes
    projects |> Set.count |> should equal 7
    projects |> should contain (ProjectId.from "4c116d6d-22ff-4b9c-80fd-de0e6d0a96b6") // A
    projects |> should contain (ProjectId.from "386c73d8-95dc-4684-ba6c-20f4cd63e42a") // B
    projects |> should contain (ProjectId.from "2904bc7b-8b30-41f1-8160-02b5281704b4") // C
    projects |> should contain (ProjectId.from "209eab33-f903-4195-bc2d-03d086129168") // D
    projects |> should contain (ProjectId.from "d7b81c18-45df-44dc-853d-8cab07e1ad97") // E
    projects |> should contain (ProjectId.from "78c2e0d4-b410-4702-af93-71db7db228d0") // F
    projects |> should contain (ProjectId.from "eb5c2f2b-d117-47b0-8067-305b4bae9aa2") // G


[<Test>]
let CheckSelectSubProjectSourceOnly () =
    // 
    //      G
    //     / \
    //    E   F
    //     \ /
    //      C   D
    //     / \ /
    //    A   B
    // 
    let projectDefs = [ "A", "4c116d6d-22ff-4b9c-80fd-de0e6d0a96b6", [] 
                        "B", "386c73d8-95dc-4684-ba6c-20f4cd63e42a", []
                        "C", "2904bc7b-8b30-41f1-8160-02b5281704b4", ["4c116d6d-22ff-4b9c-80fd-de0e6d0a96b6"; "386c73d8-95dc-4684-ba6c-20f4cd63e42a"]
                        "D", "209eab33-f903-4195-bc2d-03d086129168", ["386c73d8-95dc-4684-ba6c-20f4cd63e42a"]
                        "E", "d7b81c18-45df-44dc-853d-8cab07e1ad97", ["2904bc7b-8b30-41f1-8160-02b5281704b4"]
                        "F", "78c2e0d4-b410-4702-af93-71db7db228d0", ["2904bc7b-8b30-41f1-8160-02b5281704b4"] 
                        "G", "eb5c2f2b-d117-47b0-8067-305b4bae9aa2", ["d7b81c18-45df-44dc-853d-8cab07e1ad97"; "78c2e0d4-b410-4702-af93-71db7db228d0"] ]

    let createProject (name, id, refs) = 
        let refIds = refs |> Seq.map ProjectId.from |> Set
        { Repository = RepositoryId.from name
          ProjectId = ProjectId.from id
          RelativeProjectFile = ProjectRelativeFile (sprintf "%s.csproj" name)
          UniqueProjectId = ProjectUniqueId.from (ParseGuid id)
          Output = AssemblyId.from name
          OutputType = OutputType.Dll
          FxVersion = FxInfo.from "v4.5"
          FxProfile = FxInfo.from null
          FxIdentifier = FxInfo.from null
          HasTests = false
          AssemblyReferences = Set.empty
          PackageReferences = Set.empty
          ProjectReferences = refIds }

    let projects = projectDefs |> Seq.map createProject |> Set
    // goal is A & G but D
    let goal = ["4c116d6d-22ff-4b9c-80fd-de0e6d0a96b6" |> ProjectId.from
                "eb5c2f2b-d117-47b0-8067-305b4bae9aa2" |> ProjectId.from ] |> Set
    let projects = AnthologyGraph.ComputeProjectSelectionClosureSourceOnly projects goal |> Set

    // expect all nodes but D
    projects |> Set.count |> should equal 6
    projects |> should contain (ProjectId.from "4c116d6d-22ff-4b9c-80fd-de0e6d0a96b6") // A
    projects |> should contain (ProjectId.from "386c73d8-95dc-4684-ba6c-20f4cd63e42a") // B
    projects |> should contain (ProjectId.from "2904bc7b-8b30-41f1-8160-02b5281704b4") // C
    projects |> should contain (ProjectId.from "d7b81c18-45df-44dc-853d-8cab07e1ad97") // E
    projects |> should contain (ProjectId.from "78c2e0d4-b410-4702-af93-71db7db228d0") // F
    projects |> should contain (ProjectId.from "eb5c2f2b-d117-47b0-8067-305b4bae9aa2") // G
    


[<Test>]
let CheckSelectAllProjectSourceOnly () =
    // 
    //      G
    //     / \
    //    E   F
    //     \ /
    //      C   D
    //     / \ /
    //    A   B
    // 
    let projectDefs = [ "A", "4c116d6d-22ff-4b9c-80fd-de0e6d0a96b6", [] 
                        "B", "386c73d8-95dc-4684-ba6c-20f4cd63e42a", []
                        "C", "2904bc7b-8b30-41f1-8160-02b5281704b4", ["4c116d6d-22ff-4b9c-80fd-de0e6d0a96b6"; "386c73d8-95dc-4684-ba6c-20f4cd63e42a"]
                        "D", "209eab33-f903-4195-bc2d-03d086129168", ["386c73d8-95dc-4684-ba6c-20f4cd63e42a"]
                        "E", "d7b81c18-45df-44dc-853d-8cab07e1ad97", ["2904bc7b-8b30-41f1-8160-02b5281704b4"]
                        "F", "78c2e0d4-b410-4702-af93-71db7db228d0", ["2904bc7b-8b30-41f1-8160-02b5281704b4"] 
                        "G", "eb5c2f2b-d117-47b0-8067-305b4bae9aa2", ["d7b81c18-45df-44dc-853d-8cab07e1ad97"; "78c2e0d4-b410-4702-af93-71db7db228d0"] ]

    let createProject (name, id, refs) = 
        let refIds = refs |> Seq.map ProjectId.from |> Set
        { Repository = RepositoryId.from name
          ProjectId = ProjectId.from id
          RelativeProjectFile = ProjectRelativeFile (sprintf "%s.csproj" name)
          UniqueProjectId = ProjectUniqueId.from (ParseGuid id)
          Output = AssemblyId.from name
          OutputType = OutputType.Dll
          FxVersion = FxInfo.from "v4.5"
          FxProfile = FxInfo.from null
          FxIdentifier = FxInfo.from null
          HasTests = false
          AssemblyReferences = Set.empty
          PackageReferences = Set.empty
          ProjectReferences = refIds }

    let projects = projectDefs |> Seq.map createProject |> Set
    // goal is A & G
    let goal = ["4c116d6d-22ff-4b9c-80fd-de0e6d0a96b6" |> ProjectId.from // A
                "386c73d8-95dc-4684-ba6c-20f4cd63e42a" |> ProjectId.from // B
                "2904bc7b-8b30-41f1-8160-02b5281704b4" |> ProjectId.from // C
                "209eab33-f903-4195-bc2d-03d086129168" |> ProjectId.from // D
                "d7b81c18-45df-44dc-853d-8cab07e1ad97" |> ProjectId.from // E
                "78c2e0d4-b410-4702-af93-71db7db228d0" |> ProjectId.from // F
                "eb5c2f2b-d117-47b0-8067-305b4bae9aa2" |> ProjectId.from ] |> Set // G
    let projects = AnthologyGraph.ComputeProjectSelectionClosureSourceOnly projects goal |> Set

    // expect all nodes
    projects |> Set.count |> should equal 7
    projects |> should contain (ProjectId.from "4c116d6d-22ff-4b9c-80fd-de0e6d0a96b6") // A
    projects |> should contain (ProjectId.from "386c73d8-95dc-4684-ba6c-20f4cd63e42a") // B
    projects |> should contain (ProjectId.from "2904bc7b-8b30-41f1-8160-02b5281704b4") // C
    projects |> should contain (ProjectId.from "209eab33-f903-4195-bc2d-03d086129168") // D
    projects |> should contain (ProjectId.from "d7b81c18-45df-44dc-853d-8cab07e1ad97") // E
    projects |> should contain (ProjectId.from "78c2e0d4-b410-4702-af93-71db7db228d0") // F
    projects |> should contain (ProjectId.from "eb5c2f2b-d117-47b0-8067-305b4bae9aa2") // G




[<Test>]
let CheckGenerateSolution () =
    let anthoFile = FileInfo(testFile "anthology-simplified.yaml")
    let antho = AnthologySerializer.Load anthoFile
    let graph = antho |> Graph.from 
    let content = graph.Projects |> set
                                 |> GenerateSolutionContent

    let expectedFile = testFile "anthology-solution.txt"
    let expectedLines = System.IO.File.ReadAllLines expectedFile
    content |> should equal expectedLines

[<Test>]
let CheckSelectSingleProjectWithoutParent () =
    // 
    //      G
    //     / \
    //    E   F
    //     \ /
    //      C   D
    //     / \ /
    //    A   B
    // 
    let projectDefs = [ "A", "4c116d6d-22ff-4b9c-80fd-de0e6d0a96b6", [] 
                        "B", "386c73d8-95dc-4684-ba6c-20f4cd63e42a", []
                        "C", "2904bc7b-8b30-41f1-8160-02b5281704b4", ["4c116d6d-22ff-4b9c-80fd-de0e6d0a96b6"; "386c73d8-95dc-4684-ba6c-20f4cd63e42a"]
                        "D", "209eab33-f903-4195-bc2d-03d086129168", ["386c73d8-95dc-4684-ba6c-20f4cd63e42a"]
                        "E", "d7b81c18-45df-44dc-853d-8cab07e1ad97", ["2904bc7b-8b30-41f1-8160-02b5281704b4"]
                        "F", "78c2e0d4-b410-4702-af93-71db7db228d0", ["2904bc7b-8b30-41f1-8160-02b5281704b4"] 
                        "G", "eb5c2f2b-d117-47b0-8067-305b4bae9aa2", ["d7b81c18-45df-44dc-853d-8cab07e1ad97"; "78c2e0d4-b410-4702-af93-71db7db228d0"] ]

    let createProject (name, id, refs) = 
        let refIds = refs |> Seq.map ProjectId.from |> Set
        { Repository = RepositoryId.from name
          ProjectId = ProjectId.from id
          RelativeProjectFile = ProjectRelativeFile (sprintf "%s.csproj" name)
          UniqueProjectId = ProjectUniqueId.from (ParseGuid id)
          Output = AssemblyId.from name
          OutputType = OutputType.Dll
          FxVersion = FxInfo.from "v4.5"
          FxProfile = FxInfo.from null
          FxIdentifier = FxInfo.from null
          HasTests = false
          AssemblyReferences = Set.empty
          PackageReferences = Set.empty
          ProjectReferences = refIds }

    let projects = projectDefs |> Seq.map createProject |> Set
    // goal is D
    let goal = ["209eab33-f903-4195-bc2d-03d086129168" |> ProjectId.from ] |> Set
    let projects = AnthologyGraph.ComputeProjectSelectionClosure projects goal |> Set

    // expect D
    projects |> Set.count |> should equal 1
    projects |> should contain (ProjectId.from "209eab33-f903-4195-bc2d-03d086129168") // D

[<Test>]
let CheckSelect2ProjectsWithoutParent () =
    // 
    //      G
    //     / \
    //    E   F
    //     \ /
    //      C   D
    //     / \ /
    //    A   B
    // 
    let projectDefs = [ "A", "4c116d6d-22ff-4b9c-80fd-de0e6d0a96b6", [] 
                        "B", "386c73d8-95dc-4684-ba6c-20f4cd63e42a", []
                        "C", "2904bc7b-8b30-41f1-8160-02b5281704b4", ["4c116d6d-22ff-4b9c-80fd-de0e6d0a96b6"; "386c73d8-95dc-4684-ba6c-20f4cd63e42a"]
                        "D", "209eab33-f903-4195-bc2d-03d086129168", ["386c73d8-95dc-4684-ba6c-20f4cd63e42a"]
                        "E", "d7b81c18-45df-44dc-853d-8cab07e1ad97", ["2904bc7b-8b30-41f1-8160-02b5281704b4"]
                        "F", "78c2e0d4-b410-4702-af93-71db7db228d0", ["2904bc7b-8b30-41f1-8160-02b5281704b4"] 
                        "G", "eb5c2f2b-d117-47b0-8067-305b4bae9aa2", ["d7b81c18-45df-44dc-853d-8cab07e1ad97"; "78c2e0d4-b410-4702-af93-71db7db228d0"] ]

    let createProject (name, id, refs) = 
        let refIds = refs |> Seq.map ProjectId.from |> Set
        { Repository = RepositoryId.from name
          ProjectId = ProjectId.from id
          RelativeProjectFile = ProjectRelativeFile (sprintf "%s.csproj" name)
          UniqueProjectId = ProjectUniqueId.from (ParseGuid id)
          Output = AssemblyId.from name
          OutputType = OutputType.Dll
          FxVersion = FxInfo.from "v4.5"
          FxProfile = FxInfo.from null
          FxIdentifier = FxInfo.from null
          HasTests = false
          AssemblyReferences = Set.empty
          PackageReferences = Set.empty
          ProjectReferences = refIds }

    let projects = projectDefs |> Seq.map createProject |> Set
    // goal is D & G
    let goal = ["209eab33-f903-4195-bc2d-03d086129168" |> ProjectId.from 
                "eb5c2f2b-d117-47b0-8067-305b4bae9aa2" |> ProjectId.from ] |> Set
    let projects = AnthologyGraph.ComputeProjectSelectionClosure projects goal |> Set

    // expect D & G
    projects |> Set.count |> should equal 2
    projects |> should contain (ProjectId.from "209eab33-f903-4195-bc2d-03d086129168") // D
    projects |> should contain (ProjectId.from "eb5c2f2b-d117-47b0-8067-305b4bae9aa2") // G

[<Test>]
let CheckSelect2ProjectsWithoutParentButWithCommonChildrenSourceOnly () =
    // 
    //      G
    //     / \
    //    E   F
    //     \ /
    //      C   D
    //     / \ /
    //    A   B
    // 
    let projectDefs = [ "A", "4c116d6d-22ff-4b9c-80fd-de0e6d0a96b6", [] 
                        "B", "386c73d8-95dc-4684-ba6c-20f4cd63e42a", []
                        "C", "2904bc7b-8b30-41f1-8160-02b5281704b4", ["4c116d6d-22ff-4b9c-80fd-de0e6d0a96b6"; "386c73d8-95dc-4684-ba6c-20f4cd63e42a"]
                        "D", "209eab33-f903-4195-bc2d-03d086129168", ["386c73d8-95dc-4684-ba6c-20f4cd63e42a"]
                        "E", "d7b81c18-45df-44dc-853d-8cab07e1ad97", ["2904bc7b-8b30-41f1-8160-02b5281704b4"]
                        "F", "78c2e0d4-b410-4702-af93-71db7db228d0", ["2904bc7b-8b30-41f1-8160-02b5281704b4"] 
                        "G", "eb5c2f2b-d117-47b0-8067-305b4bae9aa2", ["d7b81c18-45df-44dc-853d-8cab07e1ad97"; "78c2e0d4-b410-4702-af93-71db7db228d0"] ]

    let createProject (name, id, refs) = 
        let refIds = refs |> Seq.map ProjectId.from |> Set
        { Repository = RepositoryId.from name
          ProjectId = ProjectId.from id
          RelativeProjectFile = ProjectRelativeFile (sprintf "%s.csproj" name)
          UniqueProjectId = ProjectUniqueId.from (ParseGuid id)
          Output = AssemblyId.from name
          OutputType = OutputType.Dll
          FxVersion = FxInfo.from "v4.5"
          FxProfile = FxInfo.from null
          FxIdentifier = FxInfo.from null
          HasTests = false
          AssemblyReferences = Set.empty
          PackageReferences = Set.empty
          ProjectReferences = refIds }

    let projects = projectDefs |> Seq.map createProject |> Set
    // goal is D & G
    let goal = ["209eab33-f903-4195-bc2d-03d086129168" |> ProjectId.from 
                "eb5c2f2b-d117-47b0-8067-305b4bae9aa2" |> ProjectId.from ] |> Set
    let projects = AnthologyGraph.ComputeProjectSelectionClosureSourceOnly projects goal |> Set

    // expect all nodes
    projects |> Set.count |> should equal 7
    projects |> should contain (ProjectId.from "4c116d6d-22ff-4b9c-80fd-de0e6d0a96b6") // A
    projects |> should contain (ProjectId.from "386c73d8-95dc-4684-ba6c-20f4cd63e42a") // B
    projects |> should contain (ProjectId.from "2904bc7b-8b30-41f1-8160-02b5281704b4") // C
    projects |> should contain (ProjectId.from "209eab33-f903-4195-bc2d-03d086129168") // D
    projects |> should contain (ProjectId.from "d7b81c18-45df-44dc-853d-8cab07e1ad97") // E
    projects |> should contain (ProjectId.from "78c2e0d4-b410-4702-af93-71db7db228d0") // F
    projects |> should contain (ProjectId.from "eb5c2f2b-d117-47b0-8067-305b4bae9aa2") // G


[<Test>]
let CheckSelect2LeafProjectsSourceOnly () =
    // 
    //      G
    //     / \
    //    E   F
    //     \ /
    //      C   D
    //     / \ /
    //    A   B
    // 
    let projectDefs = [ "A", "4c116d6d-22ff-4b9c-80fd-de0e6d0a96b6", [] 
                        "B", "386c73d8-95dc-4684-ba6c-20f4cd63e42a", []
                        "C", "2904bc7b-8b30-41f1-8160-02b5281704b4", ["4c116d6d-22ff-4b9c-80fd-de0e6d0a96b6"; "386c73d8-95dc-4684-ba6c-20f4cd63e42a"]
                        "D", "209eab33-f903-4195-bc2d-03d086129168", ["386c73d8-95dc-4684-ba6c-20f4cd63e42a"]
                        "E", "d7b81c18-45df-44dc-853d-8cab07e1ad97", ["2904bc7b-8b30-41f1-8160-02b5281704b4"]
                        "F", "78c2e0d4-b410-4702-af93-71db7db228d0", ["2904bc7b-8b30-41f1-8160-02b5281704b4"] 
                        "G", "eb5c2f2b-d117-47b0-8067-305b4bae9aa2", ["d7b81c18-45df-44dc-853d-8cab07e1ad97"; "78c2e0d4-b410-4702-af93-71db7db228d0"] ]

    let createProject (name, id, refs) = 
        let refIds = refs |> Seq.map ProjectId.from |> Set
        { Repository = RepositoryId.from name
          ProjectId = ProjectId.from id
          RelativeProjectFile = ProjectRelativeFile (sprintf "%s.csproj" name)
          UniqueProjectId = ProjectUniqueId.from (ParseGuid id)
          Output = AssemblyId.from name
          OutputType = OutputType.Dll
          FxVersion = FxInfo.from "v4.5"
          FxProfile = FxInfo.from null
          FxIdentifier = FxInfo.from null
          HasTests = false
          AssemblyReferences = Set.empty
          PackageReferences = Set.empty
          ProjectReferences = refIds }

    let projects = projectDefs |> Seq.map createProject |> Set
    // goal is A & B
    let goal = ["4c116d6d-22ff-4b9c-80fd-de0e6d0a96b6" |> ProjectId.from 
                "386c73d8-95dc-4684-ba6c-20f4cd63e42a" |> ProjectId.from ] |> Set
    let projects = AnthologyGraph.ComputeProjectSelectionClosureSourceOnly projects goal |> Set

    // expect A & B
    projects |> Set.count |> should equal 2
    projects |> should contain (ProjectId.from "4c116d6d-22ff-4b9c-80fd-de0e6d0a96b6") // A
    projects |> should contain (ProjectId.from "386c73d8-95dc-4684-ba6c-20f4cd63e42a") // B


[<Test>]
let CheckSelectProjectsWithHoleSourceOnly () =
    // 
    //      G
    //     / \
    //    E   F
    //     \ /
    //      C   D
    //     / \ /
    //    A   B
    // 
    let projectDefs = [ "A", "4c116d6d-22ff-4b9c-80fd-de0e6d0a96b6", [] 
                        "B", "386c73d8-95dc-4684-ba6c-20f4cd63e42a", []
                        "C", "2904bc7b-8b30-41f1-8160-02b5281704b4", ["4c116d6d-22ff-4b9c-80fd-de0e6d0a96b6"; "386c73d8-95dc-4684-ba6c-20f4cd63e42a"]
                        "D", "209eab33-f903-4195-bc2d-03d086129168", ["386c73d8-95dc-4684-ba6c-20f4cd63e42a"]
                        "E", "d7b81c18-45df-44dc-853d-8cab07e1ad97", ["2904bc7b-8b30-41f1-8160-02b5281704b4"]
                        "F", "78c2e0d4-b410-4702-af93-71db7db228d0", ["2904bc7b-8b30-41f1-8160-02b5281704b4"] 
                        "G", "eb5c2f2b-d117-47b0-8067-305b4bae9aa2", ["d7b81c18-45df-44dc-853d-8cab07e1ad97"; "78c2e0d4-b410-4702-af93-71db7db228d0"] ]

    let createProject (name, id, refs) = 
        let refIds = refs |> Seq.map ProjectId.from |> Set
        { Repository = RepositoryId.from name
          ProjectId = ProjectId.from id
          RelativeProjectFile = ProjectRelativeFile (sprintf "%s.csproj" name)
          UniqueProjectId = ProjectUniqueId.from (ParseGuid id)
          Output = AssemblyId.from name
          OutputType = OutputType.Dll
          FxVersion = FxInfo.from "v4.5"
          FxProfile = FxInfo.from null
          FxIdentifier = FxInfo.from null
          HasTests = false
          AssemblyReferences = Set.empty
          PackageReferences = Set.empty
          ProjectReferences = refIds }

    let projects = projectDefs |> Seq.map createProject |> Set
    // goal is G & A
    let goal = ["eb5c2f2b-d117-47b0-8067-305b4bae9aa2" |> ProjectId.from 
                "4c116d6d-22ff-4b9c-80fd-de0e6d0a96b6" |> ProjectId.from ] |> Set
    let projects = AnthologyGraph.ComputeProjectSelectionClosureSourceOnly projects goal |> Set

    // expect all nodes but D
    projects |> Set.count |> should equal 6
    projects |> should contain (ProjectId.from "4c116d6d-22ff-4b9c-80fd-de0e6d0a96b6") // A
    projects |> should contain (ProjectId.from "386c73d8-95dc-4684-ba6c-20f4cd63e42a") // B
    projects |> should contain (ProjectId.from "2904bc7b-8b30-41f1-8160-02b5281704b4") // C
    projects |> should contain (ProjectId.from "d7b81c18-45df-44dc-853d-8cab07e1ad97") // E
    projects |> should contain (ProjectId.from "78c2e0d4-b410-4702-af93-71db7db228d0") // F
    projects |> should contain (ProjectId.from "eb5c2f2b-d117-47b0-8067-305b4bae9aa2") // G
