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
        Applications = [ ]
        Bookmarks = [ { Name = "cassandra-sharp"; Version = "b62e33a6ba39f987c91fdde11472f42b2a4acd94" }; { Name = "cassandra-sharp-contrib"; Version = "e0089100b3c5ca520e831c5443ad9dc8ab176052" } ]
        Repositories = [ { Vcs = VcsType.Git; Name = "cassandra-sharp"; Url = "https://github.com/pchalamet/cassandra-sharp" }
                         { Vcs = VcsType.Git; Name = "cassandra-sharp-contrib"; Url = "https://github.com/pchalamet/cassandra-sharp-contrib" } ]
        Packages = [ { Id="FSharp.Data"; Version="2.2.5"; TargetFramework="net45" }
                     { Id="FsUnit"; Version="1.3.0.1"; TargetFramework="net45" }
                     { Id="Mini"; Version="0.4.2.0"; TargetFramework="net45" }
                     { Id="Newtonsoft.Json"; Version="7.0.1"; TargetFramework="net45" }
                     { Id="NLog"; Version="4.0.1"; TargetFramework="net45" }
                     { Id="NUnit"; Version="2.6.3"; TargetFramework="net45" }
                     { Id="xunit"; Version="1.9.1"; TargetFramework="net45" } ]
        Projects = [ { AssemblyName = "cqlplus"
                       OutputType = OutputType.Exe
                       ProjectGuid = ParseGuid "0a06398e-69be-487b-a011-4c0be6619b59"
                       RelativeProjectFile = "cqlplus/cqlplus-net45.csproj"
                       FxTarget = "v4.5"
                       ProjectReferences = [ ProjectRef.Bind (ParseGuid "6f6eb447-9569-406a-a23b-c09b6dbdbe10"); ProjectRef.Bind(ParseGuid "c1d252b7-d766-4c28-9c46-0696f896846c") ]
                       AssemblyReferences = [ AssemblyRef.Bind "System" ; AssemblyRef.Bind "System.Data"; AssemblyRef.Bind "System.Xml"]
                       PackageReferences = [ ]
                       Repository = RepositoryRef.Bind "cassandra-sharp" } ] }

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
                     Repository = { Vcs=VcsType.Git; Name=".full-build"; Url="https://github.com/pchalamet/full-build"}
                     PackageGlobalCache = "c:\PackageGlobalCache"
                     NuGets = ["https://www.nuget.org/api/v2/"; "https://www.nuget.org/api/v3/"] }

    config |> should equal expected
