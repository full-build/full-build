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

module RepoTests

open NUnit.Framework
open FsUnit
open Anthology

[<Test>]
let CheckFilter () =
    let repos = Set [ { Builder = BuilderType.MSBuild
                        Tester = TestRunnerType.NUnit
                        Repository = { Name = RepositoryId.from "cassandra-sharp"
                                       Url = RepositoryUrl.from "https://github.com/pchalamet/cassandra-sharp"
                                       Branch = Some (BranchId.from "fullbuild")
                                       Vcs = VcsType.Git } }
                      { Builder = BuilderType.MSBuild
                        Tester = TestRunnerType.NUnit
                        Repository = { Name = RepositoryId.from "cassandra-sharp-contrib"
                                       Url = RepositoryUrl.from "https://github.com/pchalamet/cassandra-sharp-contrib"
                                       Branch = None
                                       Vcs = VcsType.Git } } ]
    let filters = set ["cassandra*"]
    let filteredRepos = PatternMatching.FilterMatch repos (fun x -> x.Repository.Name.toString) filters

    filteredRepos |> should equal repos
