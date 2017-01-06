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

module ProjectsSerializerTests

open FsUnit
open NUnit.Framework
open Anthology
open ProjectsSerializer
open StringHelpers

[<Test>]
let CheckSaveLoadProjects () =
    let projects1 = {
        Projects = [ { Output = AssemblyId.from "cqlplus"
                       ProjectId = ProjectId.from "cqlplus"
                       OutputType = OutputType.Exe
                       UniqueProjectId = ProjectUniqueId.from (ParseGuid "0a06398e-69be-487b-a011-4c0be6619b59")
                       RelativeProjectFile = ProjectRelativeFile "cqlplus/cqlplus-net45.csproj"
                       FxVersion = FxInfo.from "v4.5"
                       FxProfile = FxInfo.from null
                       FxIdentifier = FxInfo.from null
                       HasTests = false
                       ProjectReferences = [ ProjectId.from "cassandrasharp.interfaces"; ProjectId.from "cassandrasharp" ] |> set
                       AssemblyReferences = [ AssemblyId.from "System" ; AssemblyId.from "System.Data"; AssemblyId.from "System.Xml"] |> set
                       PackageReferences = [ PackageId.from "NLog" ; PackageId.from "Rx-Main" ] |> Set
                       Repository = RepositoryId.from "cassandra-sharp" } ] |> set }

    let res = ProjectsSerializer.Serialize projects1
    printfn "%s" res

    let projects2 = ProjectsSerializer.Deserialize res
    projects2 |> should equal projects1

