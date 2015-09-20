module AnthologySerializerTests

open AnthologySerializer
open FsUnit
open NUnit.Framework
open Anthology
open StringHelpers

[<Test>]
let CheckSaveLoadAnthology () =
    let antho1 = {
        Repositories = [ { Vcs = VcsType.Git; Name = RepositoryId.from "cassandra-sharp"; Url = RepositoryUrl.from "https://github.com/pchalamet/cassandra-sharp" }
                         { Vcs = VcsType.Git; Name = RepositoryId.from "cassandra-sharp-contrib"; Url = RepositoryUrl.from "https://github.com/pchalamet/cassandra-sharp-contrib" } ] |> set
        Projects = [ { Output = AssemblyId.from "cqlplus"
                       OutputType = OutputType.Exe
                       ProjectGuid = ProjectId.from (ParseGuid "0a06398e-69be-487b-a011-4c0be6619b59")
                       RelativeProjectFile = ProjectRelativeFile "cqlplus/cqlplus-net45.csproj"
                       FxTarget = FrameworkVersion "v4.5"
                       ProjectReferences = [ ProjectId.from (ParseGuid "6f6eb447-9569-406a-a23b-c09b6dbdbe10"); ProjectId.from(ParseGuid "c1d252b7-d766-4c28-9c46-0696f896846c") ] |> set
                       AssemblyReferences = [ AssemblyId.from "System" ; AssemblyId.from "System.Data"; AssemblyId.from "System.Xml"] |> set
                       PackageReferences = Set.empty
                       Repository = RepositoryId.from "cassandra-sharp" } ] |> set }

    let res = AnthologySerializer.Serialize antho1
    printfn "%s" res

    let antho2 = AnthologySerializer.Deserialize res
    antho2 |> should equal antho1

