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
    let repoGit = { Vcs = VcsType.Git; Name = RepositoryId.from "cassandra-sharp"; Url = RepositoryUrl.from "https://github.com/pchalamet/cassandra-sharp" }
    repoGit |> should equal { Vcs = VcsType.Git
                              Name = RepositoryId.from "cassandra-sharp"
                              Url = RepositoryUrl.from "https://github.com/pchalamet/cassandra-sharp" } 

    let repoHg = { Vcs = VcsType.Hg; Name = RepositoryId.from "cassandra-sharp"; Url = RepositoryUrl.from "https://github.com/pchalamet/cassandra-sharp" }
    repoHg |> should equal { Vcs = VcsType.Hg
                             Name = RepositoryId.from "cassandra-sharp"
                             Url = RepositoryUrl.from "https://github.com/pchalamet/cassandra-sharp" } 


[<Test>]
let CheckEqualityWithPermutation () =
    let antho1 = {
        Artifacts = "c:\toto"
        NuGets = []
        MasterRepository = { Vcs = VcsType.Git; Name = RepositoryId.from ".full-build"; Url = RepositoryUrl.from "https://github.com/pchalamet/cassandra-sharp-full-build" }
        Repositories = [ { Vcs = VcsType.Git; Name = RepositoryId.from "cassandra-sharp"; Url = RepositoryUrl.from "https://github.com/pchalamet/cassandra-sharp" }
                         { Vcs = VcsType.Git; Name = RepositoryId.from "cassandra-sharp-contrib"; Url = RepositoryUrl.from "https://github.com/pchalamet/cassandra-sharp-contrib" } ] |> set
        Projects = [ { Output = AssemblyId.from "cqlplus"
                       ProjectId = ProjectRef.from "cqlplus"
                       OutputType = OutputType.Exe
                       UniqueProjectId = ProjectId.from (ParseGuid "0a06398e-69be-487b-a011-4c0be6619b59")
                       RelativeProjectFile = ProjectRelativeFile "cqlplus/cqlplus-net45.csproj"
                       FxTarget = FrameworkVersion "v4.5"
                       ProjectReferences = [ ProjectRef.from "cassandrasharp.interfaces"; ProjectRef.from "cassandrasharp" ] |> set
                       AssemblyReferences = [ AssemblyId.from "System" ; AssemblyId.from "System.Data"; AssemblyId.from "System.Xml"] |> set
                       PackageReferences = Set.empty
                       Repository = RepositoryId.from "cassandra-sharp" } ] |> set
        Applications = Set.empty }

    let antho2 = {
        Artifacts = "c:\toto"
        NuGets = []
        MasterRepository = { Vcs = VcsType.Git; Name = RepositoryId.from ".full-build"; Url = RepositoryUrl.from "https://github.com/pchalamet/cassandra-sharp-full-build" }
        Repositories = [ { Vcs = VcsType.Git; Name = RepositoryId.from "cassandra-sharp-contrib"; Url = RepositoryUrl.from "https://github.com/pchalamet/cassandra-sharp-contrib" } 
                         { Vcs = VcsType.Git; Name = RepositoryId.from "cassandra-sharp"; Url = RepositoryUrl.from "https://github.com/pchalamet/cassandra-sharp" } ] |> set
        Projects = [ { Output = AssemblyId.from "cqlplus"
                       ProjectId = ProjectRef.from "cqlplus"
                       OutputType = OutputType.Exe
                       UniqueProjectId = ProjectId.from (ParseGuid "0a06398e-69be-487b-a011-4c0be6619b59")
                       RelativeProjectFile = ProjectRelativeFile "cqlplus/cqlplus-net45.csproj"
                       FxTarget = FrameworkVersion "v4.5"
                       ProjectReferences = [ ProjectRef.from "cassandrasharp.interfaces"; ProjectRef.from "cassandrasharp" ] |> set
                       AssemblyReferences = [ AssemblyId.from "System" ; AssemblyId.from "System.Xml"; AssemblyId.from "System.Data" ] |> set
                       PackageReferences = Set.empty
                       Repository = RepositoryId.from "cassandra-sharp" } ] |> set 
        Applications = Set.empty }
        
    antho1 |> should equal antho2
