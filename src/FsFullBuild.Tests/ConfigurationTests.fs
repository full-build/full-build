module ConfigurationTests

open NUnit.Framework
open FsUnit
open Configuration
open System.IO
open Anthology
open StringHelpers

[<Test>]
let CheckRoundtripAnthology () =
    let expected = {
        Applications = Set.empty
        Bookmarks = [ { Name = BookmarkName "cassandra-sharp"; Version = BookmarkVersion "b62e33a6ba39f987c91fdde11472f42b2a4acd94" }; { Name = BookmarkName "cassandra-sharp-contrib"; Version = BookmarkVersion "e0089100b3c5ca520e831c5443ad9dc8ab176052" } ] |> set
        Repositories = [ { Vcs = VcsType.Git; Name = RepositoryName "cassandra-sharp"; Url = RepositoryUrl "https://github.com/pchalamet/cassandra-sharp" }
                         { Vcs = VcsType.Git; Name = RepositoryName "cassandra-sharp-contrib"; Url = RepositoryUrl "https://github.com/pchalamet/cassandra-sharp-contrib" } ] |> set
        Projects = [ { Output = AssemblyRef.Bind "cqlplus"
                       OutputType = OutputType.Exe
                       ProjectGuid = ParseGuid "0a06398e-69be-487b-a011-4c0be6619b59"
                       RelativeProjectFile = "cqlplus/cqlplus-net45.csproj"
                       FxTarget = "v4.5"
                       ProjectReferences = [ ProjectRef.Bind (ParseGuid "6f6eb447-9569-406a-a23b-c09b6dbdbe10"); ProjectRef.Bind(ParseGuid "c1d252b7-d766-4c28-9c46-0696f896846c") ] |> set
                       AssemblyReferences = [ AssemblyRef.Bind "System" ; AssemblyRef.Bind "System.Data"; AssemblyRef.Bind "System.Xml"] |> set
                       PackageReferences = Set.empty
                       Repository = RepositoryRef.Bind(RepositoryName "cassandra-sharp") } ] |> set }

    let file = FileInfo (Path.GetRandomFileName())
    printfn "Temporary file is %A" file.FullName

    SaveAnthologyToFile file expected

    let antho = LoadAnthologyFromFile file
    antho |> should equal expected

[<Test>]
let CheckGlobalIniFilename () =
    let file = FileInfo("GlobalConfig.ini")
    file.Exists |> should equal true
    let config = GlobalConfigurationFromFile file

    let expected = { BinRepo = "c:\BinRepo"
                     Repository = { Vcs=VcsType.Git; Name=RepositoryName ".full-build"; Url=RepositoryUrl "https://github.com/pchalamet/full-build"}
                     PackageGlobalCache = "c:\PackageGlobalCache"
                     NuGets = ["https://www.nuget.org/api/v2/"; "https://www.nuget.org/api/v3/"] }

    config |> should equal expected
