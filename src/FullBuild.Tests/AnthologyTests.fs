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

module AnthologyTests

open NUnit.Framework
open FsUnit
open Anthology
open StringHelpers

[<Test>]
let CheckReferences () =
    AssemblyId.from "badaboum" |> should equal <| AssemblyId.from "BADABOUM"

    PackageId.from "badaboum" |> should not' (equal <| PackageId.from "BADABOUM")
    PackageId.from "badaboum" |> should equal <| PackageId.from "badaboum"

    RepositoryId.from "badaboum" |> should equal <| RepositoryId.from "BADABOUM"

let CheckToRepository () =
    let repoGit = { Name = RepositoryId.from "cassandra-sharp"; Url = RepositoryUrl.from "https://github.com/pchalamet/cassandra-sharp"; Branch = None}
    repoGit |> should equal { Name = RepositoryId.from "cassandra-sharp"
                              Branch = None
                              Url = RepositoryUrl.from "https://github.com/pchalamet/cassandra-sharp" } 

    let repoHg = { Name = RepositoryId.from "cassandra-sharp"; Url = RepositoryUrl.from "https://github.com/pchalamet/cassandra-sharp"; Branch = None }
    repoHg |> should equal { Name = RepositoryId.from "cassandra-sharp"
                             Branch = None
                             Url = RepositoryUrl.from "https://github.com/pchalamet/cassandra-sharp" } 


[<Test>]
let CheckEqualityWithPermutation () =
    let antho1 = {
        MinVersion = "1.2.3.4"
        Artifacts = @"c:\toto"
        NuGets = []
        MasterRepository = { Name = RepositoryId.from ".full-build"; Url = RepositoryUrl.from "https://github.com/pchalamet/cassandra-sharp-full-build" ; Branch = None}
        Repositories = [ { Builder = BuilderType.MSBuild
                           Repository = { Name = RepositoryId.from "cassandra-sharp"; Url = RepositoryUrl.from "https://github.com/pchalamet/cassandra-sharp" ; Branch = None} }
                         { Builder = BuilderType.MSBuild
                           Repository = { Name = RepositoryId.from "cassandra-sharp-contrib"; Url = RepositoryUrl.from "https://github.com/pchalamet/cassandra-sharp-contrib" ; Branch = None} } ] |> set
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
                       PackageReferences = Set.empty
                       Repository = RepositoryId.from "cassandra-sharp" } ] |> set
        Applications = Set.empty 
        Tester = TestRunnerType.NUnit 
        Vcs = VcsType.Git }

    let antho2 = {
        MinVersion = "1.2.3.4"
        Artifacts = @"c:\toto"
        NuGets = []
        MasterRepository = { Name = RepositoryId.from ".full-build"; Url = RepositoryUrl.from "https://github.com/pchalamet/cassandra-sharp-full-build" ; Branch = None}
        Repositories = [ { Builder = BuilderType.MSBuild
                           Repository = { Name = RepositoryId.from "cassandra-sharp"; Url = RepositoryUrl.from "https://github.com/pchalamet/cassandra-sharp" ; Branch = None} }
                         { Builder = BuilderType.MSBuild
                           Repository = { Name = RepositoryId.from "cassandra-sharp-contrib"; Url = RepositoryUrl.from "https://github.com/pchalamet/cassandra-sharp-contrib" ; Branch = None} } ] |> set
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
                       AssemblyReferences = [ AssemblyId.from "System" ; AssemblyId.from "System.Xml"; AssemblyId.from "System.Data" ] |> set
                       PackageReferences = Set.empty
                       Repository = RepositoryId.from "cassandra-sharp" } ] |> set 
        Applications = Set.empty 
        Tester = TestRunnerType.NUnit 
        Vcs = VcsType.Git }
        
    antho1 |> should equal antho2
