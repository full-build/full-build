module ProjectParsingTests

open System
open System.IO
open System.Linq
open System.Xml.Linq
open ProjectParsing
open NUnit.Framework
open FsUnit
open Anthology
open StringHelpers
open MsBuildHelpers

let XDocumentLoader (loadPackagesConfig : bool) (fi : FileInfo) : XDocument option =
    match fi.Name with
    | "packages.config" -> if loadPackagesConfig then Some (XDocument.Load ("packages.xml"))
                           else None
    | x -> if fi.Exists then Some (XDocument.Load (x))
           else None

[<Test>]
let CheckMatchPaketReference () =
    let ref = @"..\..\packages\FSharp.Core\lib\portable-net45+netcore45+wpa81+wp8\FSharp.Core.dll"
    match ref with
    | MatchPackage x -> x |> should equal "FSharp.Core"
    | _ -> failwith "Parsing error"

[<Test>]
let CheckCastString () =
    let x = XElement (XNamespace.None + "Test", "42")
    let xs : string = !> x
    let xi : int = !> x
    xs |> should equal "42"
    xi |> should equal 42

[<Test>]
let CheckBasicParsingCSharp () =
    let expectedPackages = Set [ { Id=PackageId.Bind "FSharp.Data"; Version=PackageVersion "2.2.5" }
                                 { Id=PackageId.Bind "FsUnit"; Version=PackageVersion "1.3.0.1" }
                                 { Id=PackageId.Bind "Mini"; Version=PackageVersion "0.4.2.0" }
                                 { Id=PackageId.Bind "Newtonsoft.Json"; Version=PackageVersion "7.0.1" }
                                 { Id=PackageId.Bind "NLog"; Version=PackageVersion "4.0.1" }
                                 { Id=PackageId.Bind "NUnit"; Version=PackageVersion "2.6.3" }
                                 { Id=PackageId.Bind "xunit"; Version=PackageVersion "1.9.1" } ]
    
    let file = FileInfo ("./CSharpProjectSample1.csproj")
    let prjDescriptor = ProjectParsing.ParseProjectContent (XDocumentLoader true) file.Directory (RepositoryId.Bind "Test") file
    prjDescriptor.Project.ProjectGuid |> should equal (ProjectId.Bind (ParseGuid "3AF55CC8-9998-4039-BC31-54ECBFC91396"))
    prjDescriptor.Packages |> should equal expectedPackages

[<Test>]
let CheckBasicParsingFSharp () =
    let file = FileInfo ("./FSharpProjectSample1.fsproj")
    let prjDescriptor = ProjectParsing.ParseProjectContent (XDocumentLoader true) file.Directory (RepositoryId.Bind "Test") file
    prjDescriptor.Project.ProjectGuid |> should equal (ProjectId.Bind (ParseGuid "5fde3939-c144-4287-bc57-a96ec2d1a9da"))

[<Test>]
let CheckParseVirginProject () =
    let file = FileInfo ("./VirginProject.csproj")
    let prjDescriptor = ProjectParsing.ParseProjectContent (XDocumentLoader true) file.Directory (RepositoryId.Bind "Test") file
    prjDescriptor.Project.ProjectReferences |> should equal [ProjectId.Bind (ParseGuid "c1d252b7-d766-4c28-9c46-0696f896846d")]

[<Test>]
let CheckParsePaketizedProject () =
    let file = FileInfo ("./Paket.fsproj")
    let prjDescriptor = ProjectParsing.ParseProjectContent (XDocumentLoader false) file.Directory (RepositoryId.Bind "Test") file
    prjDescriptor.Project.ProjectReferences |> should equal [ProjectId.Bind (ParseGuid "c1d252b7-d766-4c28-9c46-0696f896846c")]
    prjDescriptor.Project.PackageReferences |> should equal (Set [ PackageId.Bind "FSharp.Core"; PackageId.Bind "UnionArgParser" ])

[<Test>]
let CheckParseConvertedProject () =
    let expectedAssemblies = Set [ AssemblyId.Bind "System"
                                   AssemblyId.Bind "System.Numerics"
                                   AssemblyId.Bind "System.Xml"
                                   AssemblyId.Bind "System.Configuration" ]

    let expectedPackages = Set [ { Id=PackageId.Bind "Rx-Core"; Version=Unspecified }
                                 { Id=PackageId.Bind "Rx-Interfaces"; Version=Unspecified }
                                 { Id=PackageId.Bind "Rx-Linq"; Version=Unspecified }
                                 { Id=PackageId.Bind "Rx-PlatformServices"; Version=Unspecified }
                                 { Id=PackageId.Bind "FSharp.Data"; Version=PackageVersion "2.2.5" }
                                 { Id=PackageId.Bind "FsUnit"; Version=PackageVersion "1.3.0.1" }
                                 { Id=PackageId.Bind "Mini"; Version=PackageVersion "0.4.2.0" }
                                 { Id=PackageId.Bind "Newtonsoft.Json"; Version=PackageVersion "7.0.1" }
                                 { Id=PackageId.Bind "NLog"; Version=PackageVersion "4.0.1" }
                                 { Id=PackageId.Bind "NUnit"; Version=PackageVersion "2.6.3" }
                                 { Id=PackageId.Bind "xunit"; Version=PackageVersion "1.9.1" } ]

    let expectedProject = { Repository = RepositoryId.Bind "Test"
                            RelativeProjectFile = ProjectRelativeFile "ConvertedProject.csproj"
                            ProjectGuid = ProjectId.Bind (ParseGuid "c1d252b7-d766-4c28-9c46-0696f896846d") 
                            ProjectType = ProjectType.Bind (ParseGuid "FAE04EC0-301F-11D3-BF4B-00C04F79EFBC")
                            Output = AssemblyId.Bind "CassandraSharp"
                            OutputType = OutputType.Dll
                            FxTarget = FrameworkVersion "v4.5"
                            AssemblyReferences = Set [ AssemblyId.Bind "System"
                                                       AssemblyId.Bind "System.Numerics"
                                                       AssemblyId.Bind "System.Xml"
                                                       AssemblyId.Bind "System.Configuration" ]
                            PackageReferences = Set [ PackageId.Bind "Rx-Core"
                                                      PackageId.Bind "Rx-Interfaces"
                                                      PackageId.Bind "Rx-Linq"
                                                      PackageId.Bind "Rx-PlatformServices"
                                                      PackageId.Bind "FSharp.Data"
                                                      PackageId.Bind "FsUnit"
                                                      PackageId.Bind "Mini"
                                                      PackageId.Bind "Newtonsoft.Json"
                                                      PackageId.Bind "NLog"
                                                      PackageId.Bind "NUnit"
                                                      PackageId.Bind "xunit" ]
                            ProjectReferences = Set [ ProjectId.Bind (ParseGuid "6f6eb447-9569-406a-a23b-c09b6dbdbe10") ] }

    let expectedProjects = Set [ ProjectId.Bind (ParseGuid "6f6eb447-9569-406a-a23b-c09b6dbdbe10") ]

    let projectFile = FileInfo ("./ConvertedProject.csproj")
    let prjDescriptor = ProjectParsing.ParseProjectContent (XDocumentLoader true) projectFile.Directory (RepositoryId.Bind "Test") projectFile
    prjDescriptor.Project.ProjectReferences |> should equal [ProjectId.Bind (ParseGuid "6f6eb447-9569-406a-a23b-c09b6dbdbe10")]

    prjDescriptor.Packages |> Seq.iter (fun x -> printfn "%A" x.Id.Value)

    prjDescriptor.Packages |> should equal expectedPackages
    prjDescriptor.Project |> should equal expectedProject

[<Test>]
let CheckParseConvertedProjectWithoutPackagesConfig () =
    let expectedAssemblies = Set [ AssemblyId.Bind "System"
                                   AssemblyId.Bind "System.Numerics"
                                   AssemblyId.Bind "System.Xml"
                                   AssemblyId.Bind "System.Configuration" ]

    let expectedPackages = Set [ { Id=PackageId.Bind "Rx-Core"; Version=Unspecified }
                                 { Id=PackageId.Bind "Rx-Interfaces"; Version=Unspecified }
                                 { Id=PackageId.Bind "Rx-Linq"; Version=Unspecified }
                                 { Id=PackageId.Bind "Rx-PlatformServices"; Version=Unspecified } ]

    let expectedProject = { Repository = RepositoryId.Bind "Test"
                            RelativeProjectFile = ProjectRelativeFile "ConvertedProject.csproj"
                            ProjectGuid = ProjectId.Bind (ParseGuid "c1d252b7-d766-4c28-9c46-0696f896846d") 
                            ProjectType = ProjectType.Bind (ParseGuid "FAE04EC0-301F-11D3-BF4B-00C04F79EFBC")
                            Output = AssemblyId.Bind "CassandraSharp"
                            OutputType = OutputType.Dll
                            FxTarget = FrameworkVersion "v4.5"
                            AssemblyReferences = Set [ AssemblyId.Bind "System"
                                                       AssemblyId.Bind "System.Numerics"
                                                       AssemblyId.Bind "System.Xml"
                                                       AssemblyId.Bind "System.Configuration" ]
                            PackageReferences = Set [ PackageId.Bind "Rx-Core"
                                                      PackageId.Bind "Rx-Interfaces"
                                                      PackageId.Bind "Rx-Linq"
                                                      PackageId.Bind "Rx-PlatformServices" ]
                            ProjectReferences = Set [ ProjectId.Bind (ParseGuid "6f6eb447-9569-406a-a23b-c09b6dbdbe10") ] }

    let expectedProjects = Set [ ProjectId.Bind (ParseGuid "6f6eb447-9569-406a-a23b-c09b6dbdbe10") ]

    let projectFile = FileInfo ("./ConvertedProject.csproj")
    let prjDescriptor = ProjectParsing.ParseProjectContent (XDocumentLoader false) projectFile.Directory (RepositoryId.Bind "Test") projectFile
    prjDescriptor.Project.ProjectReferences |> should equal [ProjectId.Bind (ParseGuid "6f6eb447-9569-406a-a23b-c09b6dbdbe10")]

    prjDescriptor.Packages |> Seq.iter (fun x -> printfn "%A" x.Id.Value)

    prjDescriptor.Packages |> should equal expectedPackages
    prjDescriptor.Project |> should equal expectedProject

[<Test>]
let CheckParseInvalidProject () =
    let projectFile = FileInfo ("./ProjectWithInvalidRefs.csproj")
    let getPrjDescriptor = (fun () -> ProjectParsing.ParseProjectContent (XDocumentLoader true) projectFile.Directory (RepositoryId.Bind "Test") projectFile |> ignore)
    getPrjDescriptor |> should throw typeof<System.Exception>
