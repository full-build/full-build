module AnthologyTests

open System
open System.IO
open NUnit.Framework
open FsUnit
open Anthology



[<Test>]
let CheckRoundtripAnthology () =
    let expected = {
        Applications = [ ]
        Binaries = [ { AssemblyName = "System"; HintPath = None }; { AssemblyName = "System.Configuration"; HintPath = None } ]
        Bookmarks = [ { Name = "cassandra-sharp"; Version = "b62e33a6ba39f987c91fdde11472f42b2a4acd94" }; { Name = "cassandra-sharp-contrib"; Version = "e0089100b3c5ca520e831c5443ad9dc8ab176052" } ]
        Packages = [ { Name = "log4net"; Version = "2.0.3"}; {Name="moq"; Version="4.2.1502.0911"}]
        Projects = [ { AssemblyName = "cqlplus"
                       OutputType = OutputType.Exe
                       ProjectGuid = Guid.Parse ("0a06398e-69be-487b-a011-4c0be6619b59")
                       RelativeProjectFile = "cqlplus/cqlplus-net45.csproj"
                       FxTarget = "v4.5"
                       ProjectReferences = [ Guid.Parse ("6f6eb447-9569-406a-a23b-c09b6dbdbe10"); Guid.Parse ("c1d252b7-d766-4c28-9c46-0696f896846c") ]
                       BinaryReferences = [ "System" ; "System.Data"; "System.Xml"]
                       PackageReferences = [ ]
                       Repository = "cassandra-sharp" } ]
        Repositories = [ { Vcs = VcsType.Git; Name = "cassandra-sharp"; Url = "https://github.com/pchalamet/cassandra-sharp" }
                         { Vcs = VcsType.Git; Name = "cassandra-sharp-contrib"; Url = "https://github.com/pchalamet/cassandra-sharp-contrib" } ]
        }

    let file = new FileInfo (Path.GetRandomFileName())
    printfn "Temporary file is %A" file.FullName

    SaveAnthologyToFile file expected

    let antho = LoadAnthologyFromFile file
    antho |> should equal expected





