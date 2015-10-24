module ViewTests

open System.IO
open NUnit.Framework
open FsUnit
open Anthology
open View
open StringHelpers
open Solution

[<Test>]
let CheckSelectProject () =
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

    let createProject (name,id,refs) = 
        let refIds = refs |> Seq.map (ProjectId.from << ParseGuid) |> Set
        { Repository = RepositoryId.from name
          RelativeProjectFile = ProjectRelativeFile (sprintf "%s.csproj" name)
          ProjectGuid = ProjectId.from (ParseGuid id)
          Output = AssemblyId.from name
          OutputType = OutputType.Dll
          FxTarget = FrameworkVersion "v4.5"
          AssemblyReferences = Set.empty
          PackageReferences = Set.empty
          ProjectReferences = refIds }

    let projects = projectDefs |> Seq.map createProject |> Set
    let goal = ["4c116d6d-22ff-4b9c-80fd-de0e6d0a96b6" |> ParseGuid |> ProjectId.from
                "eb5c2f2b-d117-47b0-8067-305b4bae9aa2" |> ParseGuid |> ProjectId.from ] |> Set
    let projects = ComputeProjectSelectionClosure projects goal |> Set

    projects |> Set.count |> should equal 5
    projects |> should contain (ProjectId.from (ParseGuid "4c116d6d-22ff-4b9c-80fd-de0e6d0a96b6"))
    projects |> should contain (ProjectId.from (ParseGuid "2904bc7b-8b30-41f1-8160-02b5281704b4"))
    projects |> should contain (ProjectId.from (ParseGuid "d7b81c18-45df-44dc-853d-8cab07e1ad97"))
    projects |> should contain (ProjectId.from (ParseGuid "78c2e0d4-b410-4702-af93-71db7db228d0"))
    projects |> should contain (ProjectId.from (ParseGuid "eb5c2f2b-d117-47b0-8067-305b4bae9aa2"))
    

[<Test>]
let CheckGenerateSolution () =
    let projects = [ { Repository = RepositoryId.from "cassandra-sharp-contrib"
                       RelativeProjectFile = ProjectRelativeFile "CassandraSharp.Contrib.log4net/CassandraSharp.Contrib.log4net-net45.csproj"
                       ProjectGuid = ProjectId.from (ParseGuid "925833ed-8653-4e90-9c37-b5b6cb693cf4")
                       Output = AssemblyId.from "CassandraSharp.Contrib.log4net"
                       OutputType = OutputType.Dll
                       FxTarget = FrameworkVersion "v4.5"   
                       AssemblyReferences = [ AssemblyId.from "System" ] |> set
                       PackageReferences = [ PackageId.from "log4net"
                                             PackageId.from "Rx-Core"; PackageId.from "Rx-Interfaces"; PackageId.from "Rx-Linq"; PackageId.from "Rx-Main"; PackageId.from "Rx-PlatformServices" ] |> set
                       ProjectReferences = [ ProjectId.from (ParseGuid "6f6eb447-9569-406a-a23b-c09b6dbdbe10") ] |> set}
                     { Repository = RepositoryId.from "cassandra-sharp-contrib"
                       RelativeProjectFile = ProjectRelativeFile "CassandraSharp.Contrib.log4netUnitTests/CassandraSharp.Contrib.log4netUnitTests-net45.csproj"
                       ProjectGuid = ProjectId.from (ParseGuid "9e8648a4-d25a-4cfa-aaee-20d9d63ff571")
                       Output = AssemblyId.from "CassandraSharp.Contrib.log4netUnitTests"
                       OutputType = OutputType.Dll
                       FxTarget = FrameworkVersion "v4.5"
                       AssemblyReferences = [ AssemblyId.from "System"; AssemblyId.from "System.Core" ] |> set
                       PackageReferences = [ PackageId.from "log4net" 
                                             PackageId.from "NUnit"; PackageId.from "Rx-Core"; PackageId.from "Rx-Interfaces"; PackageId.from "Rx-Linq"; PackageId.from "Rx-Main"; PackageId.from "Rx-PlatformServices" ] |> set
                       ProjectReferences = [ ProjectId.from (ParseGuid "6f6eb447-9569-406a-a23b-c09b6dbdbe10"); ProjectId.from (ParseGuid "c1d252b7-d766-4c28-9c46-0696f896846c"); ProjectId.from (ParseGuid "925833ed-8653-4e90-9c37-b5b6cb693cf4") ] |> set } ]

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
                               "\tEndProjectSection"
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
                               "EndGlobal" |]
