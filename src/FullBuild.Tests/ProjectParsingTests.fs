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
    let expectedPackages = Set [ { Id=PackageId.from "FSharp.Data"; Version=PackageVersion "2.2.5" }
                                 { Id=PackageId.from "FsUnit"; Version=PackageVersion "1.3.0.1" }
                                 { Id=PackageId.from "Mini"; Version=PackageVersion "0.4.2.0" }
                                 { Id=PackageId.from "Newtonsoft.Json"; Version=PackageVersion "7.0.1" }
                                 { Id=PackageId.from "NLog"; Version=PackageVersion "4.0.1" }
                                 { Id=PackageId.from "NUnit"; Version=PackageVersion "2.6.3" }
                                 { Id=PackageId.from "xunit"; Version=PackageVersion "1.9.1" } ]
    
    let file = FileInfo ("./CSharpProjectSample1.csproj")
    let prjDescriptor = ProjectParsing.ParseProjectContent (XDocumentLoader true) file.Directory (RepositoryId.from "Test") file
    prjDescriptor.Project.UniqueProjectId |> should equal (ProjectUniqueId.from (ParseGuid "3AF55CC8-9998-4039-BC31-54ECBFC91396"))
    prjDescriptor.Packages |> should equal expectedPackages

[<Test>]
let CheckBasicParsingFSharp () =
    let file = FileInfo ("./FSharpProjectSample1.fsproj")
    let prjDescriptor = ProjectParsing.ParseProjectContent (XDocumentLoader true) file.Directory (RepositoryId.from "Test") file
    prjDescriptor.Project.UniqueProjectId |> should equal (ProjectUniqueId.from (ParseGuid "5fde3939-c144-4287-bc57-a96ec2d1a9da"))

[<Test>]
let CheckParseVirginProject () =
    let file = FileInfo ("./VirginProject.csproj")
    let prjDescriptor = ProjectParsing.ParseProjectContent (XDocumentLoader true) file.Directory (RepositoryId.from "Test") file
    prjDescriptor.Project.ProjectReferences |> should equal [ProjectId.from "CassandraSharp"]

[<Test>]
let CheckParsePaketizedProject () =
    let file = FileInfo ("./Paket.fsproj")
    let prjDescriptor = ProjectParsing.ParseProjectContent (XDocumentLoader false) file.Directory (RepositoryId.from "Test") file
    prjDescriptor.Project.ProjectReferences |> should equal [ProjectId.from "CassandraSharp"]
    prjDescriptor.Project.PackageReferences |> should equal (Set [ PackageId.from "FSharp.Core"; PackageId.from "UnionArgParser" ])

[<Test>]
let CheckParseConvertedProject () =
    let expectedPackages = Set [ { Id=PackageId.from "Rx-Core"; Version=Unspecified }
                                 { Id=PackageId.from "Rx-Interfaces"; Version=Unspecified }
                                 { Id=PackageId.from "Rx-Linq"; Version=Unspecified }
                                 { Id=PackageId.from "Rx-PlatformServices"; Version=Unspecified }
                                 { Id=PackageId.from "FSharp.Data"; Version=PackageVersion "2.2.5" }
                                 { Id=PackageId.from "FsUnit"; Version=PackageVersion "1.3.0.1" }
                                 { Id=PackageId.from "Mini"; Version=PackageVersion "0.4.2.0" }
                                 { Id=PackageId.from "Newtonsoft.Json"; Version=PackageVersion "7.0.1" }
                                 { Id=PackageId.from "NLog"; Version=PackageVersion "4.0.1" }
                                 { Id=PackageId.from "NUnit"; Version=PackageVersion "2.6.3" }
                                 { Id=PackageId.from "xunit"; Version=PackageVersion "1.9.1" } ]

    let expectedProject = { Repository = RepositoryId.from "Test"
                            ProjectId = ProjectId.from "CassandraSharp"
                            RelativeProjectFile = ProjectRelativeFile "ConvertedProject.csproj"
                            UniqueProjectId = ProjectUniqueId.from (ParseGuid "c1d252b7-d766-4c28-9c46-0696f896846d") 
                            Output = AssemblyId.from "CassandraSharp"
                            OutputType = OutputType.Dll
                            FxTarget = FrameworkVersion "v4.5"
                            AssemblyReferences = Set [ AssemblyId.from "System"
                                                       AssemblyId.from "System.Numerics"
                                                       AssemblyId.from "System.Xml"
                                                       AssemblyId.from "System.Configuration" ]
                            PackageReferences = Set [ PackageId.from "Rx-Core"
                                                      PackageId.from "Rx-Interfaces"
                                                      PackageId.from "Rx-Linq"
                                                      PackageId.from "Rx-PlatformServices"
                                                      PackageId.from "FSharp.Data"
                                                      PackageId.from "FsUnit"
                                                      PackageId.from "Mini"
                                                      PackageId.from "Newtonsoft.Json"
                                                      PackageId.from "NLog"
                                                      PackageId.from "NUnit"
                                                      PackageId.from "xunit" ]
                            ProjectReferences = Set [ ProjectId.from "cassandrasharp.interfaces" ] }

    let projectFile = FileInfo ("./ConvertedProject.csproj")
    let prjDescriptor = ProjectParsing.ParseProjectContent (XDocumentLoader true) projectFile.Directory (RepositoryId.from "Test") projectFile
    prjDescriptor.Project.ProjectReferences |> should equal [ProjectId.from "cassandrasharp.interfaces"]

    prjDescriptor.Packages |> Seq.iter (fun x -> printfn "%A" x.Id.toString)

    prjDescriptor.Packages |> should equal expectedPackages
    prjDescriptor.Project |> should equal expectedProject

[<Test>]
let CheckParseConvertedProjectWithoutPackagesConfig () =
    let expectedPackages = Set [ { Id=PackageId.from "Rx-Core"; Version=Unspecified }
                                 { Id=PackageId.from "Rx-Interfaces"; Version=Unspecified }
                                 { Id=PackageId.from "Rx-Linq"; Version=Unspecified }
                                 { Id=PackageId.from "Rx-PlatformServices"; Version=Unspecified } ]

    let expectedProject = { Repository = RepositoryId.from "Test"
                            ProjectId = ProjectId.from "CassandraSharp"
                            RelativeProjectFile = ProjectRelativeFile "ConvertedProject.csproj"
                            UniqueProjectId = ProjectUniqueId.from (ParseGuid "c1d252b7-d766-4c28-9c46-0696f896846d") 
                            Output = AssemblyId.from "CassandraSharp"
                            OutputType = OutputType.Dll
                            FxTarget = FrameworkVersion "v4.5"
                            AssemblyReferences = Set [ AssemblyId.from "System"
                                                       AssemblyId.from "System.Numerics"
                                                       AssemblyId.from "System.Xml"
                                                       AssemblyId.from "System.Configuration" ]
                            PackageReferences = Set [ PackageId.from "Rx-Core"
                                                      PackageId.from "Rx-Interfaces"
                                                      PackageId.from "Rx-Linq"
                                                      PackageId.from "Rx-PlatformServices" ]
                            ProjectReferences = Set [ ProjectId.from "cassandrasharp.interfaces" ] }

    let projectFile = FileInfo ("./ConvertedProject.csproj")
    let prjDescriptor = ProjectParsing.ParseProjectContent (XDocumentLoader false) projectFile.Directory (RepositoryId.from "Test") projectFile
    prjDescriptor.Project.ProjectReferences |> should equal [ProjectId.from "cassandrasharp.interfaces"]

    prjDescriptor.Packages |> Seq.iter (fun x -> printfn "%A" x.Id.toString)

    prjDescriptor.Packages |> should equal expectedPackages
    prjDescriptor.Project |> should equal expectedProject

[<Test>]
let CheckParseInvalidProject () =
    let projectFile = FileInfo ("./ProjectWithInvalidRefs.csproj")
    let getPrjDescriptor = (fun () -> ProjectParsing.ParseProjectContent (XDocumentLoader true) projectFile.Directory (RepositoryId.from "Test") projectFile |> ignore)
    getPrjDescriptor |> should throw typeof<System.Exception>
