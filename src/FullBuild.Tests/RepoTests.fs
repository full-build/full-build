module RepoTests

open NUnit.Framework
open FsUnit
open Anthology
open Repo

[<Test>]
let CheckFilter () =
    let repos = Set [ { Builder = BuilderType.MSBuild    
                        Sticky = false
                        Repository = { Name = RepositoryId.from "cassandra-sharp"; Url = RepositoryUrl.from "https://github.com/pchalamet/cassandra-sharp"; Branch = Some (BranchId.from "fullbuild") } }
                      { Builder = BuilderType.MSBuild
                        Sticky = true
                        Repository = { Name = RepositoryId.from "cassandra-sharp-contrib"; Url = RepositoryUrl.from "https://github.com/pchalamet/cassandra-sharp-contrib"; Branch = None } } ]
  
    MatchRepo repos (RepositoryId.from "cassandra*") |> should equal repos
