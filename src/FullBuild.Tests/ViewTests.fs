module ViewTests

open System.IO
open NUnit.Framework
open FsUnit
open Anthology
open View
open StringHelpers
open Configuration

[<Test>]
let CheckSelectProject () =
    let file = FileInfo ("anthology-indexed.json")
    let antho = LoadAnthologyFromFile file
    
    let projects = ComputeProjectSelectionClosure antho.Projects [RepositoryId.Bind "cassandra-sharp-contrib"] |> Seq.toList
    projects |> should equal [ ProjectId.Bind (ParseGuid "925833ed-8653-4e90-9c37-b5b6cb693cf4")
                               ProjectId.Bind (ParseGuid "9e8648a4-d25a-4cfa-aaee-20d9d63ff571") ]

[<Test>]
let CheckGenerateSolution () =
    let projects = [ { Repository = RepositoryId.Bind "cassandra-sharp-contrib"
                       RelativeProjectFile = ProjectRelativeFile "CassandraSharp.Contrib.log4net/CassandraSharp.Contrib.log4net-net45.csproj"
                       ProjectGuid = ProjectId (ParseGuid "925833ed-8653-4e90-9c37-b5b6cb693cf4")
                       ProjectType = ProjectType (ParseGuid "FAE04EC0-301F-11D3-BF4B-00C04F79EFBC")
                       Output = AssemblyId.Bind "CassandraSharp.Contrib.log4net"
                       OutputType = OutputType.Dll
                       FxTarget = FrameworkVersion "v4.5"   
                       AssemblyReferences = [ AssemblyId.Bind "System" ] |> set
                       PackageReferences = [ PackageId.Bind "log4net"
                                             PackageId.Bind "Rx-Core"; PackageId.Bind "Rx-Interfaces"; PackageId.Bind "Rx-Linq"; PackageId.Bind "Rx-Main"; PackageId.Bind "Rx-PlatformServices" ] |> set
                       ProjectReferences = [ ProjectId.Bind (ParseGuid "6f6eb447-9569-406a-a23b-c09b6dbdbe10") ] |> set}
                     { Repository = RepositoryId.Bind "cassandra-sharp-contrib"
                       RelativeProjectFile = ProjectRelativeFile "CassandraSharp.Contrib.log4netUnitTests/CassandraSharp.Contrib.log4netUnitTests-net45.csproj"
                       ProjectGuid = ProjectId (ParseGuid "9e8648a4-d25a-4cfa-aaee-20d9d63ff571")
                       ProjectType = ProjectType (ParseGuid "FAE04EC0-301F-11D3-BF4B-00C04F79EFBC")
                       Output = AssemblyId.Bind "CassandraSharp.Contrib.log4netUnitTests"
                       OutputType = OutputType.Dll
                       FxTarget = FrameworkVersion "v4.5"
                       AssemblyReferences = [ AssemblyId.Bind "System"; AssemblyId.Bind "System.Core" ] |> set
                       PackageReferences = [ PackageId.Bind "log4net" 
                                             PackageId.Bind "NUnit"; PackageId.Bind "Rx-Core"; PackageId.Bind "Rx-Interfaces"; PackageId.Bind "Rx-Linq"; PackageId.Bind "Rx-Main"; PackageId.Bind "Rx-PlatformServices" ] |> set
                       ProjectReferences = [ ProjectId.Bind (ParseGuid "6f6eb447-9569-406a-a23b-c09b6dbdbe10"); ProjectId.Bind (ParseGuid "c1d252b7-d766-4c28-9c46-0696f896846c"); ProjectId.Bind (ParseGuid "925833ed-8653-4e90-9c37-b5b6cb693cf4") ] |> set } ]

    let content = GenerateSolutionContent projects

    // NOTE: CassandraSharp.Contrib.log4netUnitTests must depend on CassandraSharp.Contrib.log4net
    //       other dependencies must not be set as outside solution scope (ie: no build order to be specified)
    content |> should equal [| ""
                               "Microsoft Visual Studio Solution File, Format Version 12.00"
                               "# Visual Studio 2013"
                               @"Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""CassandraSharp.Contrib.log4net-net45"", ""cassandra-sharp-contrib/CassandraSharp.Contrib.log4net/CassandraSharp.Contrib.log4net-net45.csproj"", ""{925833ed-8653-4e90-9c37-b5b6cb693cf4}"""
                               "\tProjectSection(ProjectDependencies) = postProject"
                               "\tEndProjectSection"
                               "EndProject"
                               @"Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""CassandraSharp.Contrib.log4netUnitTests-net45"", ""cassandra-sharp-contrib/CassandraSharp.Contrib.log4netUnitTests/CassandraSharp.Contrib.log4netUnitTests-net45.csproj"", ""{9e8648a4-d25a-4cfa-aaee-20d9d63ff571}"""
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
