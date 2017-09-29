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

module MsBuildHelpersTests

open FsUnit
open NUnit.Framework
open XmlHelpers
open MSBuildHelpers
open System.Xml.Linq
open Anthology
open StringHelpers

[<Test>]
let CheckCast () =
    let xel = XElement (NsNone + "Toto", 42)
    let i = !> xel : int
    i |> should equal 42

[<Test>]
let CheckProjectPropertyName () =
    let project = { Output = AssemblyId.from "cqlplus"
                    ProjectId = ProjectId.from "CqlPlus"
                    OutputType = OutputType.Exe
                    RelativeProjectFile = ProjectRelativeFile "cqlplus/cqlplus-net45.csproj"
                    HasTests = false
                    Platform = "net452"
                    ProjectReferences = [ ProjectId.from "cassandrasharp.interfaces"; ProjectId.from "cassandrasharp" ] |> set
                    PackageReferences = Set.empty
                    Repository = RepositoryId.from "cassandra-sharp" }

    let propName = ProjectPropertyName project.ProjectId
    propName |> should equal "FullBuild_cqlplus"

[<Test>]
let CheckPackagePropertyName () =
    let package = "Rx-Core" |> PackageId.from
    let propName = PackagePropertyName package
    propName |> should equal "FullBuild_rx_core_Pkg"
