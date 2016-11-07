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

module ArtifactsSerializerTests

open FsUnit
open NUnit.Framework
open Anthology
open ArtifactsSerializer
open StringHelpers

[<Test>]
let CheckSaveLoadArtifacts () =
    let artifacts1 = {
        MinVersion = "1.2.3.4"
        Binaries = @"c:\toto"
        NuGets = [ RepositoryUrl.from "https://www.nuget.org/api/v2/"; RepositoryUrl.from "file:///C:/src/full-build-packages/"]
        MasterRepository = { Name = RepositoryId.from ".full-build"; Url = RepositoryUrl.from "https://github.com/pchalamet/cassandra-sharp-full-build"; Branch = None }
        Repositories = [ { Builder = BuilderType.MSBuild
                           Repository = { Name = RepositoryId.from "cassandra-sharp"; Url = RepositoryUrl.from "https://github.com/pchalamet/cassandra-sharp"; Branch = Some (BranchId.from "fullbuild") } }
                         { Builder = BuilderType.MSBuild
                           Repository = { Name = RepositoryId.from "cassandra-sharp-contrib"; Url = RepositoryUrl.from "https://github.com/pchalamet/cassandra-sharp-contrib"; Branch = None } } ] |> set
        Applications = [ { Name = ApplicationId.from "toto"
                           Publisher = PublisherType.Copy
                           Projects = [ProjectId.from "cassandrasharp"] |> set } ] |> Set 
        Tester = TestRunnerType.NUnit 
        Vcs = VcsType.Gerrit }

    let res = ArtifactsSerializer.Serialize artifacts1
    printfn "%s" res

    let artifacts2 = ArtifactsSerializer.Deserialize res
    artifacts2 |> should equal artifacts1

