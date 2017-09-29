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

module SimplifyTests

open FsUnit
open NUnit.Framework
open Anthology
open StringHelpers
open System.IO
open TestHelpers

[<Test>]
let CheckConflictsWithSameOutput () =
    let p1 = { Output = AssemblyId.from "cqlplus"
               ProjectId = ProjectId.from "cqlplus"
               OutputType = OutputType.Exe
               RelativeProjectFile = ProjectRelativeFile "cqlplus/cqlplus-net45.csproj"
               HasTests = false
               Platform = "net452"
               ProjectReferences = [ ProjectId.from "cassandrasharp"; ProjectId.from "cassandrasharp.interfaces" ] |> set
               PackageReferences = Set.empty
               Repository = RepositoryId.from "cassandra-sharp" } 

    let p2 = { Output = AssemblyId.from "cqlplus"
               ProjectId = ProjectId.from "cqlplus"
               OutputType = OutputType.Exe
               RelativeProjectFile = ProjectRelativeFile "cqlplus/cqlplus-net45.csproj"
               HasTests = false
               Platform = "net452"
               ProjectReferences = [ ProjectId.from "cassandrasharp"; ProjectId.from "cassandrasharp.interfaces" ] |> set
               PackageReferences = Set.empty
               Repository = RepositoryId.from "cassandra-sharp2" }

    let conflictsSameGuid = Core.Indexation.findConflicts [p1; p2] |> List.ofSeq
    conflictsSameGuid |> should equal [Core.Indexation.SameOutput (p1, p2)]

[<Test>]
let CheckNoConflictsSameProjectName () =
    let p1 = { Output = AssemblyId.from "cqlplus"
               ProjectId = ProjectId.from "cqlplus"
               OutputType = OutputType.Exe
               RelativeProjectFile = ProjectRelativeFile "cqlplus/cqlplus-net45.csproj"
               HasTests = false
               Platform = "net452"
               ProjectReferences = [ ProjectId.from "cassandrasharp"; ProjectId.from "cassandrasharp.interfaces" ] |> set
               PackageReferences = Set.empty
               Repository = RepositoryId.from "cassandra-sharp" } 

    let p2 = { Output = AssemblyId.from "cqlplus2"
               ProjectId = ProjectId.from "cqlplus"
               OutputType = OutputType.Exe
               RelativeProjectFile = ProjectRelativeFile "cqlplus/cqlplus-net45.csproj"
               HasTests = false
               Platform = "net452"
               ProjectReferences = [ ProjectId.from "cassandrasharp"; ProjectId.from "cassandrasharp.interfaces" ] |> set
               PackageReferences = Set.empty
               Repository = RepositoryId.from "cassandra-sharp2" }

    let conflictsSameGuid = Core.Indexation.findConflicts [p1; p2] |> List.ofSeq
    conflictsSameGuid |> should equal []
