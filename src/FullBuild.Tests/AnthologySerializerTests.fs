module AnthologySerializerTests

open AnthologySerializer
open FsUnit
open NUnit.Framework
open Anthology
open StringHelpers

[<Test>]
let CheckSaveLoadAnthology () =
    let antho1 = {
        Artifacts = "c:\toto"
        NuGets = [ RepositoryUrl.from "https://www.nuget.org/api/v2/"; RepositoryUrl.from "file:///C:/src/full-build-packages/"]
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
                       PackageReferences = [ PackageId.from "NLog" ; PackageId.from "Rx-Main" ] |> Set
                       Repository = RepositoryId.from "cassandra-sharp" } ] |> set 
        Applications = [ { Name = ApplicationId.from "toto"
                           Projects = [ ProjectRef.from "cassandrasharp.interfaces"; ProjectRef.from "cassandrasharp" ] |> set } ] |> Set }

    let res = AnthologySerializer.Serialize antho1
    printfn "%s" res

    let antho2 = AnthologySerializer.Deserialize res
    antho2 |> should equal antho1

