module RepoTests

open NUnit.Framework
open FsUnit
open Anthology
open Repo

[<Test>]
let CheckFilter () =
    let repos = [ { Vcs = VcsType.Git; Name = RepositoryId.Bind "cassandra-sharp"; Url = RepositoryUrl "https://github.com/pchalamet/cassandra-sharp" }
                  { Vcs = VcsType.Git; Name = RepositoryId.Bind "cassandra-sharp-contrib"; Url = RepositoryUrl "https://github.com/pchalamet/cassandra-sharp-contrib" } ]
  
    MatchRepo repos (RepositoryId.Bind "cassandra*") |> should equal repos
