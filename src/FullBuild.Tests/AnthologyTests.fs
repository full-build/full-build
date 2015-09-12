module AnthologyTests

open NUnit.Framework
open FsUnit
open Anthology
open StringHelpers

[<Test>]
let CheckReferences () =
    AssemblyId.from "badaboum" |> should equal <| AssemblyId.from "BADABOUM"

    PackageId.from "badaboum" |> should equal <| PackageId.from "BADABOUM"

    RepositoryId.from "badaboum" |> should equal <| RepositoryId.from "BADABOUM"

[<Test>]
let CheckToRepository () =
    let (ToRepository repoGit) = ("git", "cassandra-sharp", "https://github.com/pchalamet/cassandra-sharp")
    repoGit |> should equal { Vcs = VcsType.Git
                              Name = RepositoryId.from "cassandra-sharp"
                              Url = RepositoryUrl "https://github.com/pchalamet/cassandra-sharp" } 

    let (ToRepository repoHg) = ("hg", "cassandra-sharp", "https://github.com/pchalamet/cassandra-sharp")
    repoHg |> should equal { Vcs = VcsType.Hg
                             Name = RepositoryId.from "cassandra-sharp"
                             Url = RepositoryUrl "https://github.com/pchalamet/cassandra-sharp" } 

    (fun () -> let (ToRepository repo) = ("pouet", "cassandra-sharp", "https://github.com/pchalamet/cassandra-sharp")
               ())
        |> should throw typeof<System.Exception>

[<Test>]
let CheckEqualityWithPermutation () =
    let antho1 = {
        Applications = Set.empty
        Repositories = [ { Vcs = VcsType.Git; Name = RepositoryId.from "cassandra-sharp"; Url = RepositoryUrl "https://github.com/pchalamet/cassandra-sharp" }
                         { Vcs = VcsType.Git; Name = RepositoryId.from "cassandra-sharp-contrib"; Url = RepositoryUrl "https://github.com/pchalamet/cassandra-sharp-contrib" } ] |> set
        Projects = [ { Output = AssemblyId.from "cqlplus"
                       OutputType = OutputType.Exe
                       ProjectGuid = ProjectId.from (ParseGuid "0a06398e-69be-487b-a011-4c0be6619b59")
                       ProjectType = ProjectType.from (ParseGuid "FAE04EC0-301F-11D3-BF4B-00C04F79EFBC")
                       RelativeProjectFile = ProjectRelativeFile "cqlplus/cqlplus-net45.csproj"
                       FxTarget = FrameworkVersion "v4.5"
                       ProjectReferences = [ ProjectId.from (ParseGuid "6f6eb447-9569-406a-a23b-c09b6dbdbe10"); ProjectId.from(ParseGuid "c1d252b7-d766-4c28-9c46-0696f896846c") ] |> set
                       AssemblyReferences = [ AssemblyId.from "System" ; AssemblyId.from "System.Data"; AssemblyId.from "System.Xml"] |> set
                       PackageReferences = Set.empty
                       Repository = RepositoryId.from "cassandra-sharp" } ] |> set }

    let antho2 = {
        Applications = Set.empty
        Repositories = [ { Vcs = VcsType.Git; Name = RepositoryId.from "cassandra-sharp-contrib"; Url = RepositoryUrl "https://github.com/pchalamet/cassandra-sharp-contrib" } 
                         { Vcs = VcsType.Git; Name = RepositoryId.from "cassandra-sharp"; Url = RepositoryUrl "https://github.com/pchalamet/cassandra-sharp" } ] |> set
        Projects = [ { Output = AssemblyId.from "cqlplus"
                       OutputType = OutputType.Exe
                       ProjectGuid = ProjectId.from (ParseGuid "0a06398e-69be-487b-a011-4c0be6619b59")
                       ProjectType = ProjectType.from (ParseGuid "FAE04EC0-301F-11D3-BF4B-00C04F79EFBC")
                       RelativeProjectFile = ProjectRelativeFile "cqlplus/cqlplus-net45.csproj"
                       FxTarget = FrameworkVersion "v4.5"
                       ProjectReferences = [ ProjectId.from(ParseGuid "c1d252b7-d766-4c28-9c46-0696f896846c"); ProjectId.from (ParseGuid "6f6eb447-9569-406a-a23b-c09b6dbdbe10") ] |> set
                       AssemblyReferences = [ AssemblyId.from "System" ; AssemblyId.from "System.Xml"; AssemblyId.from "System.Data" ] |> set
                       PackageReferences = Set.empty
                       Repository = RepositoryId.from "cassandra-sharp" } ] |> set }
        
    antho1 |> should equal antho2
