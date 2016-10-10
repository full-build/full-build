module RepoTests

open NUnit.Framework
open FsUnit
open Anthology

[<Test>]
let CheckFilter () =
    let repos = Set [ { Builder = BuilderType.MSBuild    
                        Repository = { Name = RepositoryId.from "cassandra-sharp"; Url = RepositoryUrl.from "https://github.com/pchalamet/cassandra-sharp"; Branch = Some (BranchId.from "fullbuild") } }
                      { Builder = BuilderType.MSBuild
                        Repository = { Name = RepositoryId.from "cassandra-sharp-contrib"; Url = RepositoryUrl.from "https://github.com/pchalamet/cassandra-sharp-contrib"; Branch = None } } ]
    let filters = set ["cassandra*"]
    let filteredRepos = PatternMatching.FilterMatch repos (fun x -> x.Repository.Name.toString) filters

    filteredRepos |> should equal repos
