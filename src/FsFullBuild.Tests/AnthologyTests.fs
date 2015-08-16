module AnthologyTests

open NUnit.Framework
open FsUnit
open Anthology
open StringHelpers

[<Test>]
let CheckReferences () =
    // AssemblyRef
    AssemblyRef.Bind ({ Assembly.AssemblyName="badaboum" })
        |> should equal 
        <| AssemblyRef.Bind ({ Assembly.AssemblyName="BADABOUM" })

    // PackageRef
    PackageRef.Bind "bAdAboum"
             |> should equal 
             <| PackageRef.Bind { Id="BADABOUM"; Version="Version"; TargetFramework="TargetFramework" } 

    // AssemblyRef
    ProjectRef.Bind { AssemblyName = "cqlplus"
                      OutputType = OutputType.Exe
                      ProjectGuid = ParseGuid "0a06398e-69be-487b-a011-4c0be6619b59"
                      RelativeProjectFile = "cqlplus/cqlplus-net45.csproj"
                      FxTarget = "v4.5"
                      ProjectReferences = [ ProjectRef.Bind(ParseGuid "6f6eb447-9569-406a-a23b-c09b6dbdbe10"); ProjectRef.Bind(ParseGuid "c1d252b7-d766-4c28-9c46-0696f896846c") ] |> set
                      AssemblyReferences = [ AssemblyRef.Bind("System") ; AssemblyRef.Bind("System.Data"); AssemblyRef.Bind("System.Xml")] |> set
                      PackageReferences = Set.empty
                      Repository = RepositoryRef.Bind("cassandra-sharp") }
        |> should equal 
        <| ProjectRef.Bind { AssemblyName = "cqlplus2"
                             OutputType = OutputType.Dll
                             ProjectGuid = ParseGuid "{0a06398e-69be-487b-a011-4c0be6619b59}"
                             RelativeProjectFile = "cqlplus2/cqlplus-net45.csproj"
                             FxTarget = "v4.0"
                             ProjectReferences = [ ProjectRef.Bind(ParseGuid "c1d252b7-d766-4c28-9c46-0696f896846c") ] |> set
                             AssemblyReferences = [ AssemblyRef.Bind("System") ; AssemblyRef.Bind("System.Xml")] |> set
                             PackageReferences = [ PackageRef.Bind("NUnit") ] |> set
                             Repository = RepositoryRef.Bind("cassandra-sharp2") }

//    // RepositoryRef
    RepositoryRef.Bind { Vcs = VcsType.Git
                         Name = "Cassandra-Sharp"
                         Url = "https://github.com/pchalamet/cassandra-sharp" }
        |> should equal 
        <| RepositoryRef.Bind { Vcs = VcsType.Hg
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

[<Test>]
let CheckEqualityWithPermutation () =
    let antho1 = {
        Applications = [ ]
        Bookmarks = [ { Name = "cassandra-sharp"; Version = "b62e33a6ba39f987c91fdde11472f42b2a4acd94" }; { Name = "cassandra-sharp-contrib"; Version = "e0089100b3c5ca520e831c5443ad9dc8ab176052" } ]
        Repositories = [ { Vcs = VcsType.Git; Name = "cassandra-sharp"; Url = "https://github.com/pchalamet/cassandra-sharp" }
                         { Vcs = VcsType.Git; Name = "cassandra-sharp-contrib"; Url = "https://github.com/pchalamet/cassandra-sharp-contrib" } ] |> set
        Packages = [ { Id="FSharp.Data"; Version="2.2.5"; TargetFramework="net45" }
                     { Id="FsUnit"; Version="1.3.0.1"; TargetFramework="net45" }
                     { Id="Mini"; Version="0.4.2.0"; TargetFramework="net45" }
                     { Id="Newtonsoft.Json"; Version="7.0.1"; TargetFramework="net45" }
                     { Id="NLog"; Version="4.0.1"; TargetFramework="net45" }
                     { Id="NUnit"; Version="2.6.3"; TargetFramework="net45" }
                     { Id="xunit"; Version="1.9.1"; TargetFramework="net45" } ] |> set
        Projects = [ { AssemblyName = "cqlplus"
                       OutputType = OutputType.Exe
                       ProjectGuid = ParseGuid "0a06398e-69be-487b-a011-4c0be6619b59"
                       RelativeProjectFile = "cqlplus/cqlplus-net45.csproj"
                       FxTarget = "v4.5"
                       ProjectReferences = [ ProjectRef.Bind (ParseGuid "6f6eb447-9569-406a-a23b-c09b6dbdbe10"); ProjectRef.Bind(ParseGuid "c1d252b7-d766-4c28-9c46-0696f896846c") ] |> set
                       AssemblyReferences = [ AssemblyRef.Bind "System" ; AssemblyRef.Bind "System.Data"; AssemblyRef.Bind "System.Xml"] |> set
                       PackageReferences = Set.empty
                       Repository = RepositoryRef.Bind "cassandra-sharp" } ] |> set }

    let antho2 = {
        Applications = [ ]
        Bookmarks = [ { Name = "cassandra-sharp"; Version = "b62e33a6ba39f987c91fdde11472f42b2a4acd94" }; { Name = "cassandra-sharp-contrib"; Version = "e0089100b3c5ca520e831c5443ad9dc8ab176052" } ]
        Repositories = [ { Vcs = VcsType.Git; Name = "cassandra-sharp-contrib"; Url = "https://github.com/pchalamet/cassandra-sharp-contrib" } 
                         { Vcs = VcsType.Git; Name = "cassandra-sharp"; Url = "https://github.com/pchalamet/cassandra-sharp" } ] |> set
        Packages = [ { Id="FSharp.Data"; Version="2.2.5"; TargetFramework="net45" }
                     { Id="Mini"; Version="0.4.2.0"; TargetFramework="net45" }
                     { Id="Newtonsoft.Json"; Version="7.0.1"; TargetFramework="net45" }
                     { Id="NLog"; Version="4.0.1"; TargetFramework="net45" }
                     { Id="NUnit"; Version="2.6.3"; TargetFramework="net45" }
                     { Id="FsUnit"; Version="1.3.0.1"; TargetFramework="net45" }
                     { Id="xunit"; Version="1.9.1"; TargetFramework="net45" } ] |> set
        Projects = [ { AssemblyName = "cqlplus"
                       OutputType = OutputType.Exe
                       ProjectGuid = ParseGuid "0a06398e-69be-487b-a011-4c0be6619b59"
                       RelativeProjectFile = "cqlplus/cqlplus-net45.csproj"
                       FxTarget = "v4.5"
                       ProjectReferences = [ ProjectRef.Bind(ParseGuid "c1d252b7-d766-4c28-9c46-0696f896846c"); ProjectRef.Bind (ParseGuid "6f6eb447-9569-406a-a23b-c09b6dbdbe10") ] |> set
                       AssemblyReferences = [ AssemblyRef.Bind "System" ; AssemblyRef.Bind "System.Xml"; AssemblyRef.Bind "System.Data" ] |> set
                       PackageReferences = Set.empty
                       Repository = RepositoryRef.Bind "cassandra-sharp" } ] |> set }
        
    antho1 |> should equal antho2
