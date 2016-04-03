module ViewTests

open System.IO
open NUnit.Framework
open FsUnit
open Anthology
open View
open StringHelpers
open Solution

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
          FxTarget = FrameworkVersion "v4.5"
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
          FxTarget = FrameworkVersion "v4.5"
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
          FxTarget = FrameworkVersion "v4.5"
          AssemblyReferences = Set.empty
          PackageReferences = Set.empty
          ProjectReferences = refIds }

    let projects = projectDefs |> Seq.map createProject |> Set
    // goal is A & G but D
    let goal = ["4c116d6d-22ff-4b9c-80fd-de0e6d0a96b6" |> ProjectId.from
                "eb5c2f2b-d117-47b0-8067-305b4bae9aa2" |> ProjectId.from ] |> Set
    let projects = AnthologyGraph.ComputeProjectSelectionClosureSourceOnly projects goal |> Set

    // expect all nodes
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
          FxTarget = FrameworkVersion "v4.5"
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
    let projects = [ { Repository = RepositoryId.from "cassandra-sharp-contrib"
                       ProjectId = ProjectId.from "CassandraSharp.Contrib.log4net"
                       RelativeProjectFile = ProjectRelativeFile "CassandraSharp.Contrib.log4net/CassandraSharp.Contrib.log4net-net45.csproj"
                       UniqueProjectId = ProjectUniqueId.from (ParseGuid "925833ed-8653-4e90-9c37-b5b6cb693cf4")
                       Output = AssemblyId.from "CassandraSharp.Contrib.log4net"
                       OutputType = OutputType.Dll
                       FxTarget = FrameworkVersion "v4.5"   
                       AssemblyReferences = [ AssemblyId.from "System" ] |> set
                       PackageReferences = [ PackageId.from "log4net"
                                             PackageId.from "Rx-Core"; PackageId.from "Rx-Interfaces"; PackageId.from "Rx-Linq"; PackageId.from "Rx-Main"; PackageId.from "Rx-PlatformServices" ] |> set
                       ProjectReferences = [ ProjectId.from "cassandrasharp.interfaces" ] |> set}
                     { Repository = RepositoryId.from "cassandra-sharp-contrib"
                       ProjectId = ProjectId.from "CassandraSharp.Contrib.log4netUnitTests"
                       RelativeProjectFile = ProjectRelativeFile "CassandraSharp.Contrib.log4netUnitTests/CassandraSharp.Contrib.log4netUnitTests-net45.csproj"
                       UniqueProjectId = ProjectUniqueId.from (ParseGuid "9e8648a4-d25a-4cfa-aaee-20d9d63ff571")
                       Output = AssemblyId.from "CassandraSharp.Contrib.log4netUnitTests"
                       OutputType = OutputType.Dll
                       FxTarget = FrameworkVersion "v4.5"
                       AssemblyReferences = [ AssemblyId.from "System"; AssemblyId.from "System.Core" ] |> set
                       PackageReferences = [ PackageId.from "log4net" 
                                             PackageId.from "NUnit"; PackageId.from "Rx-Core"; PackageId.from "Rx-Interfaces"; PackageId.from "Rx-Linq"; PackageId.from "Rx-Main"; PackageId.from "Rx-PlatformServices" ] |> set
                       ProjectReferences = [ ProjectId.from "cassandrasharp.interfaces"; ProjectId.from "cassandrasharp"; ProjectId.from "cassandrasharp.contrib.log4net" ] |> set } ]

    let content = GenerateSolutionContent projects

    // NOTE: CassandraSharp.Contrib.log4netUnitTests must depend on CassandraSharp.Contrib.log4net
    //       other dependencies must not be set as outside solution scope (ie: no build order to be specified)
    content |> should equal [| ""
                               "Microsoft Visual Studio Solution File, Format Version 12.00"
                               "# Visual Studio 2013"
                               @"Project(""{fae04ec0-301f-11d3-bf4b-00c04f79efbc}"") = ""CassandraSharp.Contrib.log4net-net45"", ""cassandra-sharp-contrib/CassandraSharp.Contrib.log4net/CassandraSharp.Contrib.log4net-net45.csproj"", ""{925833ed-8653-4e90-9c37-b5b6cb693cf4}"""
                               "\tProjectSection(ProjectDependencies) = postProject"
                               "\tEndProjectSection"
                               "EndProject"
                               @"Project(""{fae04ec0-301f-11d3-bf4b-00c04f79efbc}"") = ""CassandraSharp.Contrib.log4netUnitTests-net45"", ""cassandra-sharp-contrib/CassandraSharp.Contrib.log4netUnitTests/CassandraSharp.Contrib.log4netUnitTests-net45.csproj"", ""{9e8648a4-d25a-4cfa-aaee-20d9d63ff571}"""
                               "\tProjectSection(ProjectDependencies) = postProject"
                               "\t\t{925833ed-8653-4e90-9c37-b5b6cb693cf4} = {925833ed-8653-4e90-9c37-b5b6cb693cf4}"
                               "\tEndProjectSection"
                               "EndProject"
                               @"Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = ""cassandra-sharp-contrib"", ""cassandra-sharp-contrib"", ""{930836cc-992f-e356-ccc9-96f1adb7ff88}"""
                               "EndProject"
                               "Global"
                               "\tGlobalSection(SolutionConfigurationPlatforms) = preSolution"
                               "\t\tDebug|Any CPU = Debug|Any CPU"
                               "\t\tRelease|Any CPU = Release|Any CPU"
                               "\tEndGlobalSection"
                               "\tGlobalSection(ProjectConfigurationPlatforms) = postSolution"
                               "\t\t{925833ed-8653-4e90-9c37-b5b6cb693cf4}.Debug|Any CPU.ActiveCfg = Debug|Any CPU"
                               "\t\t{925833ed-8653-4e90-9c37-b5b6cb693cf4}.Debug|Any CPU.Build.0 = Debug|Any CPU"
                               "\t\t{925833ed-8653-4e90-9c37-b5b6cb693cf4}.Release|Any CPU.ActiveCfg = Release|Any CPU"
                               "\t\t{925833ed-8653-4e90-9c37-b5b6cb693cf4}.Release|Any CPU.Build.0 = Release|Any CPU"
                               "\t\t{9e8648a4-d25a-4cfa-aaee-20d9d63ff571}.Debug|Any CPU.ActiveCfg = Debug|Any CPU"
                               "\t\t{9e8648a4-d25a-4cfa-aaee-20d9d63ff571}.Debug|Any CPU.Build.0 = Debug|Any CPU"
                               "\t\t{9e8648a4-d25a-4cfa-aaee-20d9d63ff571}.Release|Any CPU.ActiveCfg = Release|Any CPU"
                               "\t\t{9e8648a4-d25a-4cfa-aaee-20d9d63ff571}.Release|Any CPU.Build.0 = Release|Any CPU"
                               "\tEndGlobalSection"
                               "\tGlobalSection(NestedProjects) = preSolution"
                               "\t\t{925833ed-8653-4e90-9c37-b5b6cb693cf4} = {930836cc-992f-e356-ccc9-96f1adb7ff88}"
                               "\t\t{9e8648a4-d25a-4cfa-aaee-20d9d63ff571} = {930836cc-992f-e356-ccc9-96f1adb7ff88}"
                               "\tEndGlobalSection"
                               "EndGlobal" |]
