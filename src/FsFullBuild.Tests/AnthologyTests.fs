module AnthologyTests

open NUnit.Framework
open FsUnit
open Anthology
open StringHelpers

[<Test>]
let CheckReferences () =
    // AssemblyRef
//    AssemblyRef.Bind ({ Assembly.AssemblyName="badaboum" })
//        |> should equal 
//        <| AssemblyRef.Bind ({ Assembly.AssemblyName="BADABOUM" })

    // PackageRef
//    PackageRef.Bind "bAdAboum"
//             |> should equal 
//             <| PackageRef.Bind { Id=PackageRef.Bind "BADABOUM"; Version="Version"; TargetFramework="TargetFramework" } 

    // AssemblyRef
    ProjectRef.Bind { Output = AssemblyRef.Bind "cqlplus"
                      OutputType = OutputType.Exe
                      ProjectGuid = ParseGuid "0a06398e-69be-487b-a011-4c0be6619b59"
                      RelativeProjectFile = "cqlplus/cqlplus-net45.csproj"
                      FxTarget = "v4.5"
                      ProjectReferences = [ ProjectRef.Bind(ParseGuid "6f6eb447-9569-406a-a23b-c09b6dbdbe10"); ProjectRef.Bind(ParseGuid "c1d252b7-d766-4c28-9c46-0696f896846c") ] |> set
                      AssemblyReferences = [ AssemblyRef.Bind("System") ; AssemblyRef.Bind("System.Data"); AssemblyRef.Bind("System.Xml")] |> set
                      PackageReferences = Set.empty
                      Repository = RepositoryName.Bind "cassandra-sharp" }
        |> should equal 
        <| ProjectRef.Bind { Output = AssemblyRef.Bind "cqlplus2"
                             OutputType = OutputType.Dll
                             ProjectGuid = ParseGuid "{0a06398e-69be-487b-a011-4c0be6619b59}"
                             RelativeProjectFile = "cqlplus2/cqlplus-net45.csproj"
                             FxTarget = "v4.0"
                             ProjectReferences = [ ProjectRef.Bind(ParseGuid "c1d252b7-d766-4c28-9c46-0696f896846c") ] |> set
                             AssemblyReferences = [ AssemblyRef.Bind("System") ; AssemblyRef.Bind("System.Xml")] |> set
                             PackageReferences = [ PackageId "NUnit" ] |> set
                             Repository = RepositoryName.Bind "cassandra-sharp2" }

//    // RepositoryRef
// FIXME
//    RepositoryRef.Bind { Vcs = VcsType.Git
//                         Name = RepositoryName "Cassandra-Sharp"
//                         Url = RepositoryUrl "https://github.com/pchalamet/cassandra-sharp" }
//        |> should equal 
//        <| RepositoryRef.Bind { Vcs = VcsType.Hg
//                                Name = RepositoryName "Cassandra-Sharp"
//                                Url = RepositoryUrl "https://github.com/pchalamet/cassandra-sharp2" }

[<Test>]
let CheckToRepository () =
    let (ToRepository repoGit) = ("git", "https://github.com/pchalamet/cassandra-sharp", "cassandra-sharp")
    repoGit |> should equal { Vcs = VcsType.Git
                              Name = RepositoryName "cassandra-sharp"
                              Url = RepositoryUrl "https://github.com/pchalamet/cassandra-sharp" } 

    let (ToRepository repoHg) = ("hg", "https://github.com/pchalamet/cassandra-sharp", "cassandra-sharp")
    repoHg |> should equal { Vcs = VcsType.Hg
                             Name = RepositoryName "cassandra-sharp"
                             Url = RepositoryUrl "https://github.com/pchalamet/cassandra-sharp" } 

    (fun () -> let (ToRepository repo) = ("pouet", "https://github.com/pchalamet/cassandra-sharp", "cassandra-sharp")
               ())
        |> should throw typeof<System.Exception>

[<Test>]
let CheckEqualityWithPermutation () =
    let antho1 = {
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
                       Repository = RepositoryName.Bind "cassandra-sharp" } ] |> set }

    let antho2 = {
        Applications = Set.empty
        Bookmarks = [ { Name = BookmarkName "cassandra-sharp-contrib"; Version = BookmarkVersion "e0089100b3c5ca520e831c5443ad9dc8ab176052" }; { Name = BookmarkName "cassandra-sharp"; Version = BookmarkVersion "b62e33a6ba39f987c91fdde11472f42b2a4acd94" } ] |> set
        Repositories = [ { Vcs = VcsType.Git; Name = RepositoryName "cassandra-sharp-contrib"; Url = RepositoryUrl "https://github.com/pchalamet/cassandra-sharp-contrib" } 
                         { Vcs = VcsType.Git; Name = RepositoryName "cassandra-sharp"; Url = RepositoryUrl "https://github.com/pchalamet/cassandra-sharp" } ] |> set
        Projects = [ { Output = AssemblyRef.Bind "cqlplus"
                       OutputType = OutputType.Exe
                       ProjectGuid = ParseGuid "0a06398e-69be-487b-a011-4c0be6619b59"
                       RelativeProjectFile = "cqlplus/cqlplus-net45.csproj"
                       FxTarget = "v4.5"
                       ProjectReferences = [ ProjectRef.Bind(ParseGuid "c1d252b7-d766-4c28-9c46-0696f896846c"); ProjectRef.Bind (ParseGuid "6f6eb447-9569-406a-a23b-c09b6dbdbe10") ] |> set
                       AssemblyReferences = [ AssemblyRef.Bind "System" ; AssemblyRef.Bind "System.Xml"; AssemblyRef.Bind "System.Data" ] |> set
                       PackageReferences = Set.empty
                       Repository = RepositoryName.Bind "cassandra-sharp" } ] |> set }
        
    antho1 |> should equal antho2
