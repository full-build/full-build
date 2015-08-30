module SimplifyTests

open FsUnit
open NUnit.Framework
open Anthology
open StringHelpers
open System.IO


[<Test>]
let CheckSimplifyAssemblies () =
    let anthology = Configuration.LoadAnthologyFromFile (FileInfo("anthology.json"))

    let package2Files = Map.empty

    let lognetunittestsRef = ProjectRef (ParseGuid "9e8648a4-d25a-4cfa-aaee-20d9d63ff571")
    let cassandraSharpAssName = AssemblyRef.Bind "cassandrasharp"
    let cassandraSharpItfAssName = AssemblyRef.Bind "cassandrasharp.interfaces"
    let cassandraSharpPrjRef = ProjectRef.Bind (ParseGuid "6f6eb447-9569-406a-a23b-c09b6dbdbe10")
    let cassandraSharpItfPrjRef = ProjectRef.Bind (ParseGuid "c1d252b7-d766-4c28-9c46-0696f896846c")

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
