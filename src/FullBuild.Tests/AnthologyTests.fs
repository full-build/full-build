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
                       FxTarget = FrameworkVersion "v4.5"
                       ProjectReferences = [ ProjectId.from "cassandrasharp.interfaces"; ProjectId.from "cassandrasharp" ] |> set
                       AssemblyReferences = [ AssemblyId.from "System" ; AssemblyId.from "System.Data"; AssemblyId.from "System.Xml"] |> set
                       PackageReferences = Set.empty
                       Repository = RepositoryId.from "cassandra-sharp" } ] |> set
        Applications = Set.empty 
        Tester = TestRunnerType.NUnit 
        Vcs = VcsType.Git }

    let antho2 = {
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
                       FxTarget = FrameworkVersion "v4.5"
                       ProjectReferences = [ ProjectId.from "cassandrasharp.interfaces"; ProjectId.from "cassandrasharp" ] |> set
                       AssemblyReferences = [ AssemblyId.from "System" ; AssemblyId.from "System.Xml"; AssemblyId.from "System.Data" ] |> set
                       PackageReferences = Set.empty
                       Repository = RepositoryId.from "cassandra-sharp" } ] |> set 
        Applications = Set.empty 
        Tester = TestRunnerType.NUnit 
        Vcs = VcsType.Git }
        
    antho1 |> should equal antho2
