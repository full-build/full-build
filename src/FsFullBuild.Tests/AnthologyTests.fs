module AnthologyTests

open System
open System.IO
open NUnit.Framework
open FsUnit
open Anthology
open StringHelpers
open Configuration

[<Test>]
let CheckReferences () =
    // AssemblyRef
    AssemblyRef.From "toto42" 
        |> should equal 
        <| AssemblyRef.From "TOTO42" 

    AssemblyRef.From (GacAssembly { AssemblyName="toto42"}) 
        |> should equal 
        <| AssemblyRef.From (LocalAssembly { AssemblyName="TOTO42"; HintPath="c:/tralala" })

    // PackageRef
    PackageRef.From "TotO42"
             |> should equal 
             <| PackageRef.From { Id="TotO42"; Version="Version"; TargetFramework="TargetFramework" } 

    // AssemblyRef
    ProjectRef.From { AssemblyName = "cqlplus"
                      OutputType = OutputType.Exe
                      ProjectGuid = ParseGuid "0a06398e-69be-487b-a011-4c0be6619b59"
                      RelativeProjectFile = "cqlplus/cqlplus-net45.csproj"
                      FxTarget = "v4.5"
                      ProjectReferences = [ ParseGuid "6f6eb447-9569-406a-a23b-c09b6dbdbe10"; ParseGuid "c1d252b7-d766-4c28-9c46-0696f896846c" ]
                      AssemblyReferences = [ "System" ; "System.Data"; "System.Xml"]
                      PackageReferences = [ ]
                      Repository = "cassandra-sharp" }
        |> should equal 
        <| ProjectRef.From { AssemblyName = "cqlplus2"
                             OutputType = OutputType.Dll
                             ProjectGuid = ParseGuid "{0a06398e-69be-487b-a011-4c0be6619b59}"
                             RelativeProjectFile = "cqlplus2/cqlplus-net45.csproj"
                             FxTarget = "v4.0"
                             ProjectReferences = [ ParseGuid "c1d252b7-d766-4c28-9c46-0696f896846c" ]
                             AssemblyReferences = [ "System" ; "System.Xml"]
                             PackageReferences = [ "NUnit" ]
                             Repository = "cassandra-sharp2" }

//    // RepositoryRef
    RepositoryRef.From { Vcs = VcsType.Git
                         Name = "Cassandra-Sharp"
                         Url = "https://github.com/pchalamet/cassandra-sharp" }
        |> should equal 
        <| RepositoryRef.From { Vcs = VcsType.Hg
                                Name = "Cassandra-Sharp"
                                Url = "https://github.com/pchalamet/cassandra-sharp2" }

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
