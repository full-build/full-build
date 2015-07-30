module AnthologyTests

open System
open System.IO
open NUnit.Framework
open FsUnit
open Anthology
open StringHelpers

[<Test>]
let CheckRoundtripAnthology () =
    let expected = {
        Applications = [ ]
        Bookmarks = [ { Name = "cassandra-sharp"; Version = "b62e33a6ba39f987c91fdde11472f42b2a4acd94" }; { Name = "cassandra-sharp-contrib"; Version = "e0089100b3c5ca520e831c5443ad9dc8ab176052" } ]
        Repositories = [ { Vcs = VcsType.Git; Name = "cassandra-sharp"; Url = "https://github.com/pchalamet/cassandra-sharp" }
                         { Vcs = VcsType.Git; Name = "cassandra-sharp-contrib"; Url = "https://github.com/pchalamet/cassandra-sharp-contrib" } ]
        Binaries = [ GacAssembly { AssemblyName = "System" }; GacAssembly { AssemblyName = "System.Configuration" } ]
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
                       ProjectReferences = [ ParseGuid "6f6eb447-9569-406a-a23b-c09b6dbdbe10"; ParseGuid "c1d252b7-d766-4c28-9c46-0696f896846c" ]
                       AssemblyReferences = [ "System" ; "System.Data"; "System.Xml"]
                       PackageReferences = [ ]
                       Repository = "cassandra-sharp" } ] }

    let file = new FileInfo (Path.GetRandomFileName())
    printfn "Temporary file is %A" file.FullName

    SaveAnthologyToFile file expected

    let antho = LoadAnthologyFromFile file
    antho |> should equal expected

[<Test>]
let CheckReferences () =
    // AssemblyRef
    "TotO42" |> AssemblyRef.From 
             |> should equal {AssemblyRef.Target = "toto42"}
    GacAssembly { AssemblyName="TotO42"} |> AssemblyRef.From  
                                         |> should equal {AssemblyRef.Target = "toto42"}
    LocalAssembly { AssemblyName="TotO42"; HintPath="c:/tralala" } |> AssemblyRef.From  
                                                                   |> should equal {AssemblyRef.Target = "toto42"}

    // PackageRef
    "TotO42" |> PackageRef.From 
             |> should equal {PackageRef.Target = "toto42"}
    { Id="TotO42"; Version="Version"; TargetFramework="TargetFramework" } |> PackageRef.From 
                                                                          |> should equal {PackageRef.Target = "toto42"}
    
    "TotO42" |> PackageRef.From 
             |> should equal {PackageRef.Target = "toto42"}
    { Id="TotO42"; Version="Version"; TargetFramework="TargetFramework" } |> PackageRef.From 
                                                                          |> should equal {PackageRef.Target = "toto42"}

    // AssemblyRef
    { AssemblyName = "cqlplus"
      OutputType = OutputType.Exe
      ProjectGuid = ParseGuid "0a06398e-69be-487b-a011-4c0be6619b59"
      RelativeProjectFile = "cqlplus/cqlplus-net45.csproj"
      FxTarget = "v4.5"
      ProjectReferences = [ ParseGuid "6f6eb447-9569-406a-a23b-c09b6dbdbe10"; ParseGuid "c1d252b7-d766-4c28-9c46-0696f896846c" ]
      AssemblyReferences = [ "System" ; "System.Data"; "System.Xml"]
      PackageReferences = [ ]
      Repository = "cassandra-sharp" } |> ProjectRef.From 
                                       |> should equal {ProjectRef.Target = ParseGuid "0a06398e-69be-487b-a011-4c0be6619b59" }

    // RepositoryRef
    { Vcs = VcsType.Git
      Name = "Cassandra-Sharp"
      Url = "https://github.com/pchalamet/cassandra-sharp" } |> RepositoryRef.From 
                                                             |> should equal { RepositoryRef.Target = "cassandra-sharp" }

[<Test>]
let CheckToRepository () =
    let (ToRepository repoGit) = ("git", "https://github.com/pchalamet/cassandra-sharp", "cassandra-sharp")
    repoGit |> should equal { Vcs = VcsType.Git
                              Name = "cassandra-sharp"
                              Url = "https://github.com/pchalamet/cassandra-sharp" } 

    let (ToRepository repoHg) = ("hg", "https://github.com/pchalamet/cassandra-sharp", "cassandra-sharp")
    repoHg |> should equal { Vcs = VcsType.Hg
                             Name = "cassandra-sharp"
                             Url = "https://github.com/pchalamet/cassandra-sharp" } 

    (fun () -> let (ToRepository repo) = ("pouet", "https://github.com/pchalamet/cassandra-sharp", "cassandra-sharp")
               ())
        |> should throw typeof<System.Exception>
