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
    let file = FileInfo ("Anthology.json")
    let antho = LoadAnthologyFromFile file
    
    let projects = ComputeProjectSelectionClosure antho.Projects [RepositoryName.Bind "cassandra-sharp-contrib"] |> Seq.toList
    projects |> should equal [ ProjectRef.Bind (ParseGuid "925833ed-8653-4e90-9c37-b5b6cb693cf4")
                               ProjectRef.Bind (ParseGuid "9e8648a4-d25a-4cfa-aaee-20d9d63ff571") ]

[<Test>]
let CheckGenerateSolution () =
    let projects = [ { Repository = RepositoryName.Bind "cassandra-sharp-contrib"
                       RelativeProjectFile = "CassandraSharp.Contrib.log4net/CassandraSharp.Contrib.log4net-net45.csproj"
                       ProjectGuid = ParseGuid "925833ed-8653-4e90-9c37-b5b6cb693cf4"
                       Output = AssemblyRef.Bind "CassandraSharp.Contrib.log4net"
                       OutputType=OutputType.Dll
                       FxTarget="v4.5"   
                       AssemblyReferences = [ AssemblyRef.Bind "System" ] |> set
                       PackageReferences = [ PackageId "log4net"
                                             PackageId "Rx-Core"; PackageId "Rx-Interfaces"; PackageId "Rx-Linq"; PackageId "Rx-Main"; PackageId "Rx-PlatformServices" ] |> set
                       ProjectReferences = [ProjectRef.Bind (ParseGuid "6f6eb447-9569-406a-a23b-c09b6dbdbe10")] |> set}
                     { Repository = RepositoryName.Bind "cassandra-sharp-contrib"
                       RelativeProjectFile = "CassandraSharp.Contrib.log4netUnitTests/CassandraSharp.Contrib.log4netUnitTests-net45.csproj"
                       ProjectGuid = ParseGuid "9e8648a4-d25a-4cfa-aaee-20d9d63ff571"
                       Output = AssemblyRef.Bind "CassandraSharp.Contrib.log4netUnitTests"
                       OutputType = OutputType.Dll
                       FxTarget = "v4.5"
                       AssemblyReferences = [AssemblyRef.Bind "System"; AssemblyRef.Bind "System.Core"] |> set
                       PackageReferences = [ PackageId "log4net" 
                                             PackageId "NUnit"; PackageId "Rx-Core"; PackageId "Rx-Interfaces"; PackageId "Rx-Linq"; PackageId "Rx-Main"; PackageId "Rx-PlatformServices" ] |> set
                       ProjectReferences = [ ProjectRef.Bind (ParseGuid "6f6eb447-9569-406a-a23b-c09b6dbdbe10"); ProjectRef.Bind (ParseGuid "c1d252b7-d766-4c28-9c46-0696f896846c"); ProjectRef.Bind (ParseGuid "925833ed-8653-4e90-9c37-b5b6cb693cf4") ] |> set } ]

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
