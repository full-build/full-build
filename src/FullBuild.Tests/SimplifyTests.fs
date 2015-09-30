module SimplifyTests

open FsUnit
open NUnit.Framework
open Anthology
open StringHelpers
open System.IO


[<Test>]
let CheckSimplifyAssemblies () =
    let file = FileInfo("anthology-indexed.yaml")
    let anthology = AnthologySerializer.Load file

    let package2Files = Map.empty

    let lognetunittestsRef = ProjectId.from (ParseGuid "9e8648a4-d25a-4cfa-aaee-20d9d63ff571")
    let cassandraSharpAssName = AssemblyId.from "cassandrasharp"
    let cassandraSharpItfAssName = AssemblyId.from "cassandrasharp.interfaces"
    let cassandraSharpPrjRef = ProjectId.from (ParseGuid "6f6eb447-9569-406a-a23b-c09b6dbdbe10")
    let cassandraSharpItfPrjRef = ProjectId.from (ParseGuid "c1d252b7-d766-4c28-9c46-0696f896846c")

    let lognetunittests = anthology.Projects |> Seq.find (fun x -> x.ProjectGuid = lognetunittestsRef)
    lognetunittests.AssemblyReferences |> should contain cassandraSharpAssName
    lognetunittests.AssemblyReferences |> should contain cassandraSharpItfAssName
    lognetunittests.ProjectReferences |> should not' (contain cassandraSharpPrjRef)
    lognetunittests.ProjectReferences |> should not' (contain cassandraSharpItfPrjRef)

    let simplifedProjects = Simplify.TransformSingleAssemblyToProjectOrPackage package2Files anthology.Projects
    let simplifiedlognetunittests = simplifedProjects |> Seq.find (fun x -> x.ProjectGuid = lognetunittestsRef)

    simplifiedlognetunittests.AssemblyReferences |> should not' (contain cassandraSharpAssName)
    simplifiedlognetunittests.AssemblyReferences |> should not' (contain cassandraSharpItfAssName)
    simplifiedlognetunittests.ProjectReferences |> should contain cassandraSharpPrjRef
    simplifiedlognetunittests.ProjectReferences |> should contain cassandraSharpItfPrjRef

[<Test>]
let CheckSimplifyAnthology () =
    let fileIndexed = FileInfo("anthology-indexed.yaml")
    let anthology = AnthologySerializer.Load fileIndexed

    let fileSimplified = FileInfo("anthology-simplified.yaml")
    let expectedAnthology = AnthologySerializer.Load fileSimplified

    let package2files = Map [ (PackageId.from "log4net", Set [AssemblyId.from "log4net"])
                              (PackageId.from "Moq", Set [AssemblyId.from "moq"; AssemblyId.from "Moq.Silverlight" ])
                              (PackageId.from "Nunit", Set [AssemblyId.from "nunit.framework"])
                              (PackageId.from "Rx-Core", Set [AssemblyId.from "System.Reactive.Core"])
                              (PackageId.from "Rx-Interfaces", Set [AssemblyId.from "System.Reactive.Interfaces"])
                              (PackageId.from "Rx-Linq", Set [AssemblyId.from "System.Reactive.Linq"])
                              (PackageId.from "Rx-Main", Set.empty)
                              (PackageId.from "Rx-PlatformServices", Set [AssemblyId.from "System.Reactive.PlatformServices"])
                              (PackageId.from "cassandra-sharp", Set.empty)
                              (PackageId.from "cassandra-sharp-core", Set [AssemblyId.from "CassandraSharp"])
                              (PackageId.from "cassandra-sharp-interfaces", Set [AssemblyId.from "CassandraSharp.Interfaces"]) ]

    let package2packages = Map [ (PackageId.from "log4net", Set.empty)
                                 (PackageId.from "Moq", Set.empty)
                                 (PackageId.from "Nunit", Set.empty)
                                 (PackageId.from "Rx-Core", Set [PackageId.from "Rx-Interfaces"])
                                 (PackageId.from "Rx-Interfaces", Set.empty)
                                 (PackageId.from "Rx-Linq", Set [PackageId.from "Rx-Core"; PackageId.from "Rx-Interfaces"])
                                 (PackageId.from "Rx-Main", Set [PackageId.from "Rx-Core"; PackageId.from "Rx-Interfaces"; 
                                                                 PackageId.from "Rx-Linq"; PackageId.from "Rx-PlatformServices"])
                                 (PackageId.from "Rx-PlatformServices", Set [PackageId.from "Rx-Core"; PackageId.from "Rx-Interfaces"])
                                 (PackageId.from "cassandra-sharp", Set [PackageId.from "cassandra-sharp-core"; PackageId.from "cassandra-sharp-interfaces"])
                                 (PackageId.from "cassandra-sharp-core", Set [PackageId.from "Rx-Main"])
                                 (PackageId.from "cassandra-sharp-interfaces", Set.empty) ]

    let newAnthology = Simplify.SimplifyAnthology anthology package2files package2packages
    let file = FileInfo (Path.GetRandomFileName())
    printfn "Temporary file is %A" file.FullName

    AnthologySerializer.Save file newAnthology

    newAnthology |> should equal expectedAnthology

[<Test>]
let CheckConflictsWithSameGuid () =
    let p1 = { Output = AssemblyId.from "cqlplus"
               OutputType = OutputType.Exe
               ProjectGuid = ProjectId.from (ParseGuid "0a06398e-69be-487b-a011-4c0be6619b59")
               RelativeProjectFile = ProjectRelativeFile "cqlplus/cqlplus-net45.csproj"
               FxTarget = FrameworkVersion "v4.5"
               ProjectReferences = [ ProjectId.from(ParseGuid "c1d252b7-d766-4c28-9c46-0696f896846c"); ProjectId.from (ParseGuid "6f6eb447-9569-406a-a23b-c09b6dbdbe10") ] |> set
               AssemblyReferences = [ AssemblyId.from "System" ; AssemblyId.from "System.Xml"; AssemblyId.from "System.Data" ] |> set
               PackageReferences = Set.empty
               Repository = RepositoryId.from "cassandra-sharp" } 

    let p2 = { Output = AssemblyId.from "cqlplus2"
               OutputType = OutputType.Exe
               ProjectGuid = ProjectId.from (ParseGuid "0a06398e-69be-487b-a011-4c0be6619b59")
               RelativeProjectFile = ProjectRelativeFile "cqlplus/cqlplus-net45.csproj"
               FxTarget = FrameworkVersion "v4.5"
               ProjectReferences = [ ProjectId.from(ParseGuid "c1d252b7-d766-4c28-9c46-0696f896846c"); ProjectId.from (ParseGuid "6f6eb447-9569-406a-a23b-c09b6dbdbe10") ] |> set
               AssemblyReferences = [ AssemblyId.from "System" ; AssemblyId.from "System.Xml"; AssemblyId.from "System.Data" ] |> set
               PackageReferences = Set.empty
               Repository = RepositoryId.from "cassandra-sharp2" }

    let conflictsSameGuid = Indexation.FindConflicts [p1; p2] |> List.ofSeq
    conflictsSameGuid |> should equal [Indexation.SameGuid (p1, p2)]

[<Test>]
let CheckConflictsWithSameOutput () =
    let p1 = { Output = AssemblyId.from "cqlplus"
               OutputType = OutputType.Exe
               ProjectGuid = ProjectId.from (ParseGuid "0a06398e-69be-487b-a011-4c0be6619b59")
               RelativeProjectFile = ProjectRelativeFile "cqlplus/cqlplus-net45.csproj"
               FxTarget = FrameworkVersion "v4.5"
               ProjectReferences = [ ProjectId.from(ParseGuid "c1d252b7-d766-4c28-9c46-0696f896846c"); ProjectId.from (ParseGuid "6f6eb447-9569-406a-a23b-c09b6dbdbe10") ] |> set
               AssemblyReferences = [ AssemblyId.from "System" ; AssemblyId.from "System.Xml"; AssemblyId.from "System.Data" ] |> set
               PackageReferences = Set.empty
               Repository = RepositoryId.from "cassandra-sharp" } 

    let p2 = { Output = AssemblyId.from "cqlplus"
               OutputType = OutputType.Exe
               ProjectGuid = ProjectId.from (ParseGuid "39787692-f8f8-408d-9557-0c40547c1563")
               RelativeProjectFile = ProjectRelativeFile "cqlplus/cqlplus-net45.csproj"
               FxTarget = FrameworkVersion "v4.5"
               ProjectReferences = [ ProjectId.from(ParseGuid "c1d252b7-d766-4c28-9c46-0696f896846c"); ProjectId.from (ParseGuid "6f6eb447-9569-406a-a23b-c09b6dbdbe10") ] |> set
               AssemblyReferences = [ AssemblyId.from "System" ; AssemblyId.from "System.Xml"; AssemblyId.from "System.Data" ] |> set
               PackageReferences = Set.empty
               Repository = RepositoryId.from "cassandra-sharp2" }

    let conflictsSameGuid = Indexation.FindConflicts [p1; p2] |> List.ofSeq
    conflictsSameGuid |> should equal [Indexation.SameOutput (p1, p2)]

[<Test>]
let CheckNoConflictsSameProjectName () =
    let p1 = { Output = AssemblyId.from "cqlplus"
               OutputType = OutputType.Exe
               ProjectGuid = ProjectId.from (ParseGuid "0a06398e-69be-487b-a011-4c0be6619b59")
               RelativeProjectFile = ProjectRelativeFile "cqlplus/cqlplus-net45.csproj"
               FxTarget = FrameworkVersion "v4.5"
               ProjectReferences = [ ProjectId.from(ParseGuid "c1d252b7-d766-4c28-9c46-0696f896846c"); ProjectId.from (ParseGuid "6f6eb447-9569-406a-a23b-c09b6dbdbe10") ] |> set
               AssemblyReferences = [ AssemblyId.from "System" ; AssemblyId.from "System.Xml"; AssemblyId.from "System.Data" ] |> set
               PackageReferences = Set.empty
               Repository = RepositoryId.from "cassandra-sharp" } 

    let p2 = { Output = AssemblyId.from "cqlplus2"
               OutputType = OutputType.Exe
               ProjectGuid = ProjectId.from (ParseGuid "39787692-f8f8-408d-9557-0c40547c1563")
               RelativeProjectFile = ProjectRelativeFile "cqlplus/cqlplus-net45.csproj"
               FxTarget = FrameworkVersion "v4.5"
               ProjectReferences = [ ProjectId.from(ParseGuid "c1d252b7-d766-4c28-9c46-0696f896846c"); ProjectId.from (ParseGuid "6f6eb447-9569-406a-a23b-c09b6dbdbe10") ] |> set
               AssemblyReferences = [ AssemblyId.from "System" ; AssemblyId.from "System.Xml"; AssemblyId.from "System.Data" ] |> set
               PackageReferences = Set.empty
               Repository = RepositoryId.from "cassandra-sharp2" }

    let conflictsSameGuid = Indexation.FindConflicts [p1; p2] |> List.ofSeq
    conflictsSameGuid |> should equal []
