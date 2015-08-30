module SimplifyTests

open FsUnit
open NUnit.Framework
open Anthology
open StringHelpers
open System.IO


[<Test>]
let CheckSimplifyAssemblies () =
    let anthology = Configuration.LoadAnthologyFromFile (FileInfo("anthology-indexed.json"))

    let package2Files = Map.empty

    let lognetunittestsRef = ProjectId (ParseGuid "9e8648a4-d25a-4cfa-aaee-20d9d63ff571")
    let cassandraSharpAssName = AssemblyId.Bind "cassandrasharp"
    let cassandraSharpItfAssName = AssemblyId.Bind "cassandrasharp.interfaces"
    let cassandraSharpPrjRef = ProjectId.Bind (ParseGuid "6f6eb447-9569-406a-a23b-c09b6dbdbe10")
    let cassandraSharpItfPrjRef = ProjectId.Bind (ParseGuid "c1d252b7-d766-4c28-9c46-0696f896846c")

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
    let anthology = Configuration.LoadAnthologyFromFile (FileInfo("anthology-indexed.json"))
    let expectedAnthology = Configuration.LoadAnthologyFromFile (FileInfo("anthology-simplified.json"))

    let package2files = Map [ (PackageId.Bind "log4net", Set [AssemblyId.Bind "log4net"])
                              (PackageId.Bind "Moq", Set [AssemblyId.Bind "moq"; AssemblyId.Bind "Moq.Silverlight" ])
                              (PackageId.Bind "Nunit", Set [AssemblyId.Bind "nunit.framework"])
                              (PackageId.Bind "Rx-Core", Set [AssemblyId.Bind "System.Reactive.Core"])
                              (PackageId.Bind "Rx-Interfaces", Set [AssemblyId.Bind "System.Reactive.Interfaces"])
                              (PackageId.Bind "Rx-Linq", Set [AssemblyId.Bind "System.Reactive.Linq"])
                              (PackageId.Bind "Rx-Main", Set.empty)
                              (PackageId.Bind "Rx-PlatformServices", Set [AssemblyId.Bind "System.Reactive.PlatformServices"])
                              (PackageId.Bind "cassandra-sharp", Set.empty)
                              (PackageId.Bind "cassandra-sharp-core", Set [AssemblyId.Bind "CassandraSharp"])
                              (PackageId.Bind "cassandra-sharp-interfaces", Set [AssemblyId.Bind "CassandraSharp.Interfaces"]) ]

    let package2packages = Map [ (PackageId.Bind "log4net", Set.empty)
                                 (PackageId.Bind "Moq", Set.empty)
                                 (PackageId.Bind "Nunit", Set.empty)
                                 (PackageId.Bind "Rx-Core", Set [PackageId.Bind "Rx-Interfaces"])
                                 (PackageId.Bind "Rx-Interfaces", Set.empty)
                                 (PackageId.Bind "Rx-Linq", Set [PackageId.Bind "Rx-Core"; PackageId.Bind "Rx-Interfaces"])
                                 (PackageId.Bind "Rx-Main", Set [PackageId.Bind "Rx-Core"; PackageId.Bind "Rx-Interfaces"; 
                                                                 PackageId.Bind "Rx-Linq"; PackageId.Bind "Rx-PlatformServices"])
                                 (PackageId.Bind "Rx-PlatformServices", Set [PackageId.Bind "Rx-Core"; PackageId.Bind "Rx-Interfaces"])
                                 (PackageId.Bind "cassandra-sharp", Set [PackageId.Bind "cassandra-sharp-core"; PackageId.Bind "cassandra-sharp-interfaces"])
                                 (PackageId.Bind "cassandra-sharp-core", Set [PackageId.Bind "Rx-Main"])
                                 (PackageId.Bind "cassandra-sharp-interfaces", Set.empty) ]

    let newAnthology = Simplify.SimplifyAnthology anthology package2files package2packages
    let file = FileInfo (Path.GetRandomFileName())
    printfn "Temporary file is %A" file.FullName

    Configuration.SaveAnthologyToFile file newAnthology


    newAnthology |> should equal expectedAnthology
