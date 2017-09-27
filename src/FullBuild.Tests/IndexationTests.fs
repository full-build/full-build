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

module IndexationTests

open FsUnit
open NUnit.Framework
open Anthology
open StringHelpers


[<Test>]
let CheckNoReplace () = 
    let p1 = { Output = AssemblyId.from "cqlplus"
               ProjectId = ProjectId.from "cqlplus"
               OutputType = OutputType.Exe
               UniqueProjectId = ProjectUniqueId.from (ParseGuid "0a06398e-69be-487b-a011-4c0be6619b59")
               RelativeProjectFile = ProjectRelativeFile "cqlplus/cqlplus-net45.csproj"
               HasTests = false
               ProjectReferences = [ ProjectId.from "cassandrasharp"; ProjectId.from "cassandrasharp.interfaces" ] |> set
               PackageReferences = Set.empty
               Repository = RepositoryId.from "cassandra-sharp" } 

    let p2 = { Output = AssemblyId.from "cqlplus2"
               ProjectId = ProjectId.from "cqlplus2"
               OutputType = OutputType.Exe
               UniqueProjectId = ProjectUniqueId.from (ParseGuid "7e56adfe-612f-45ae-834f-4b8175d44513")
               RelativeProjectFile = ProjectRelativeFile "cqlplus/cqlplus.csproj"
               HasTests = false
               ProjectReferences = [ ProjectId.from "cassandrasharp"; ProjectId.from "cassandrasharp.interfaces" ] |> set
               PackageReferences = Set.empty
               Repository = RepositoryId.from "cassandra-sharp2" }

    let p3 = { Output = AssemblyId.from "cqlplus3"
               ProjectId = ProjectId.from "toto"
               OutputType = OutputType.Exe
               UniqueProjectId = ProjectUniqueId.from (ParseGuid "4be955ab-1db4-493a-b13f-69fbaf30c31f")
               RelativeProjectFile = ProjectRelativeFile "toto/toto-net45.csproj"
               HasTests = false
               ProjectReferences = [ ProjectId.from "cassandrasharp"; ProjectId.from "cassandrasharp.interfaces" ] |> set
               PackageReferences = Set.empty
               Repository = RepositoryId.from "cassandra-sharp-contrib" }

    let newProjects = [ p1 ] |> Set
    let existingProjects = [ p2; p3 ] |> Set
    let expected = [p1; p2; p3 ] |> Set
    let result = Core.Indexation.MergeProjects newProjects existingProjects
    result |> should equal expected



[<Test>]
let CheckReplace () = 
    let p1 = { Output = AssemblyId.from "cqlplus"
               ProjectId = ProjectId.from "cqlplus"
               OutputType = OutputType.Exe
               UniqueProjectId = ProjectUniqueId.from (ParseGuid "0a06398e-69be-487b-a011-4c0be6619b59")
               RelativeProjectFile = ProjectRelativeFile "cqlplus/cqlplus-net45.csproj"
               HasTests = false
               ProjectReferences = [ ProjectId.from "cassandrasharp"; ProjectId.from "cassandrasharp.interfaces" ] |> set
               PackageReferences = Set.empty
               Repository = RepositoryId.from "cassandra-sharp" } 

    let p2 = { Output = AssemblyId.from "cqlplus2"
               ProjectId = ProjectId.from "cqlplus"
               OutputType = OutputType.Exe
               UniqueProjectId = ProjectUniqueId.from (ParseGuid "0a06398e-69be-487b-a011-4c0be6619b59")
               RelativeProjectFile = ProjectRelativeFile "cqlplus/cqlplus-net45.csproj"
               HasTests = false
               ProjectReferences = [ ProjectId.from "cassandrasharp"; ProjectId.from "cassandrasharp.interfaces" ] |> set
               PackageReferences = Set.empty
               Repository = RepositoryId.from "cassandra-sharp" }

    let p3 = { Output = AssemblyId.from "cqlplus3"
               ProjectId = ProjectId.from "toto"
               OutputType = OutputType.Exe
               UniqueProjectId = ProjectUniqueId.from (ParseGuid "4be955ab-1db4-493a-b13f-69fbaf30c31f")
               RelativeProjectFile = ProjectRelativeFile "toto/toto-net45.csproj"
               HasTests = false
               ProjectReferences = [ ProjectId.from "cassandrasharp"; ProjectId.from "cassandrasharp.interfaces" ] |> set
               PackageReferences = Set.empty
               Repository = RepositoryId.from "cassandra-sharp-contrib" }

    let newProjects = [ p1 ] |> Set
    let existingProjects = [ p2; p3 ] |> Set
    let expected = [p1; p3 ] |> Set
    let result = Core.Indexation.MergeProjects newProjects existingProjects
    result |> should equal expected


[<Test>]
let CheckStillReferenced () = 
    let p1 = { Output = AssemblyId.from "a"
               ProjectId = ProjectId.from "a"
               OutputType = OutputType.Exe
               UniqueProjectId = ProjectUniqueId.from (ParseGuid "0a06398e-69be-487b-a011-4c0be6619b59")
               RelativeProjectFile = ProjectRelativeFile "cqlplus/cqlplus-net45.csproj"
               HasTests = false
               ProjectReferences = [ ] |> set
               PackageReferences = Set.empty
               Repository = RepositoryId.from "repo-a" } 

    let p2 = { Output = AssemblyId.from "b"
               ProjectId = ProjectId.from "b"
               OutputType = OutputType.Exe
               UniqueProjectId = ProjectUniqueId.from (ParseGuid "0a06398e-69be-487b-a011-4c0be6619b59")
               RelativeProjectFile = ProjectRelativeFile "cqlplus/cqlplus-net45.csproj"
               HasTests = false
               ProjectReferences = [ ] |> set
               PackageReferences = Set.empty
               Repository = RepositoryId.from "repo-a" }

    let p3 = { Output = AssemblyId.from "cqlplus3"
               ProjectId = ProjectId.from "toto"
               OutputType = OutputType.Exe
               UniqueProjectId = ProjectUniqueId.from (ParseGuid "4be955ab-1db4-493a-b13f-69fbaf30c31f")
               RelativeProjectFile = ProjectRelativeFile "toto/toto-net45.csproj"
               HasTests = false
               ProjectReferences = [ ProjectId.from "a" ] |> set
               PackageReferences = Set.empty
               Repository = RepositoryId.from "repo-b" }

    let newProjects = [ p2 ] |> Set
    let existingProjects = [ p1; p3 ] |> Set
    (fun () -> Core.Indexation.MergeProjects newProjects existingProjects |> ignore) |> should throw typeof<System.Exception>

