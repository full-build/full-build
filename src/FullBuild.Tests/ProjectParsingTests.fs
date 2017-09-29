//   Copyright 2014-2017 Pierre Chalamet
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.

module ProjectParsingTests

open System
open System.IO
open System.Linq
open System.Xml.Linq
open Parsers.MSBuild
open NUnit.Framework
open FsUnit
open Anthology
open StringHelpers
open XmlHelpers
open TestHelpers

let XDocumentLoader (loadPackagesConfig : bool) (fi : FileInfo) : XDocument option =
    match fi.Name with
    | "packages.config" -> if loadPackagesConfig then Some (XDocument.Load (testFile "./TestCases/packages.xml"))
                           else None
    | x -> if fi.Exists then Some (XDocument.Load (testFile x))
           else None

[<Test>]
let CheckCastString () =
    let x = XElement (XNamespace.None + "Test", "42")
    let xs : string = !> x
    let xi : int = !> x
    xs |> should equal "42"
    xi |> should equal 42

[<Test>]
let CheckBasicParsingCSharp () =
    let expectedPackages = Set [ { Package.Id=PackageId.from "FSharp.Data"; Package.Version=PackageVersion.Constraint "2.2.5" }
                                 { Package.Id=PackageId.from "FsUnit"; Package.Version=PackageVersion.Constraint "1.3.0.1" }
                                 { Package.Id=PackageId.from "Mini"; Package.Version=PackageVersion.Constraint "0.4.2.0" }
                                 { Package.Id=PackageId.from "Newtonsoft.Json"; Package.Version=PackageVersion.Constraint "7.0.1" }
                                 { Package.Id=PackageId.from "NLog"; Package.Version=PackageVersion.Constraint "4.0.1" }
                                 { Package.Id=PackageId.from "NUnit"; Package.Version=PackageVersion.Constraint "2.6.3" }
                                 { Package.Id=PackageId.from "xunit"; Package.Version=PackageVersion.Constraint "1.9.1" } 
                                 { Package.Id=PackageId.from "Microsoft.NETCore.App"; Package.Version=PackageVersion.Constraint "1.0.0" } 
                                 { Package.Id=PackageId.from "Microsoft.NET.SDK"; Package.Version=PackageVersion.Free } ]

    let file = FileInfo (testFile "./TestCases/CSharpProjectSample1.csproj")
    let prjDescriptor = Parsers.MSBuild.parseProjectContent (XDocumentLoader true) file.Directory (RepositoryId.from "Test") false file
    prjDescriptor.Packages |> should equal expectedPackages
    prjDescriptor.Project.HasTests |> should equal false

[<Test>]
let CheckTestsProject () =
    let file = FileInfo (testFile "./TestCases/CSharpProjectSample1.Tests.csproj")
    let prjDescriptor = Parsers.MSBuild.parseProjectContent (XDocumentLoader true) file.Directory (RepositoryId.from "Test") false file
    prjDescriptor.Project.HasTests |> should equal true

[<Test>]
let CheckBasicParsingFSharp () =
    let file = FileInfo (testFile "./TestCases/FSharpProjectSample1.fsproj")
    let prjDescriptor = Parsers.MSBuild.parseProjectContent (XDocumentLoader true) file.Directory (RepositoryId.from "Test") false file
    ()

[<Test>]
let CheckParseVirginProject () =
    let file = FileInfo (testFile "./TestCases/VirginProject.csproj")
    let prjDescriptor = Parsers.MSBuild.parseProjectContent (XDocumentLoader true) file.Directory (RepositoryId.from "Test") false file
    prjDescriptor.Project.ProjectReferences |> should equal [ProjectId.from "CassandraSharp"]

[<Test>]
let CheckParsePaketizedProject () =
    let file = FileInfo (testFile "./TestCases/Paket.fsproj")
    let prjDescriptor = Parsers.MSBuild.parseProjectContent (XDocumentLoader false) file.Directory (RepositoryId.from "Test") false file
    prjDescriptor.Project.ProjectReferences |> should equal [ProjectId.from "CassandraSharp"]
    prjDescriptor.Project.PackageReferences |> should equal (Set [ PackageId.from "FSharp.Core"; PackageId.from "UnionArgParser" ])

[<Test>]
let CheckParseConvertedProject () =
    let expectedPackages = Set [ { Package.Id=PackageId.from "Rx-Core"; Package.Version=PackageVersion.Free }
                                 { Package.Id=PackageId.from "Rx-Interfaces"; Package.Version=PackageVersion.Free }
                                 { Package.Id=PackageId.from "Rx-Linq"; Package.Version=PackageVersion.Free }
                                 { Package.Id=PackageId.from "Rx-PlatformServices"; Package.Version=PackageVersion.Free }
                                 { Package.Id=PackageId.from "FSharp.Data"; Package.Version=PackageVersion.Constraint "2.2.5" }
                                 { Package.Id=PackageId.from "FsUnit"; Package.Version=PackageVersion.Constraint "1.3.0.1" }
                                 { Package.Id=PackageId.from "Mini"; Package.Version=PackageVersion.Constraint "0.4.2.0" }
                                 { Package.Id=PackageId.from "Newtonsoft.Json"; Package.Version=PackageVersion.Constraint "7.0.1" }
                                 { Package.Id=PackageId.from "NLog"; Package.Version=PackageVersion.Constraint "4.0.1" }
                                 { Package.Id=PackageId.from "NUnit"; Package.Version=PackageVersion.Constraint "2.6.3" }
                                 { Package.Id=PackageId.from "xunit"; Package.Version=PackageVersion.Constraint "1.9.1" } ]

    let expectedProject = { Repository = RepositoryId.from "Test"
                            ProjectId = ProjectId.from "CassandraSharp"
                            RelativeProjectFile = ProjectRelativeFile "ConvertedProject.csproj"
                            Output = AssemblyId.from "CassandraSharp"
                            OutputType = OutputType.Dll
                            HasTests = false
                            PackageReferences = Set [ { Package.Id=PackageId.from "Rx-Core"; Package.Version=PackageVersion.Free }
                                                      { Package.Id=PackageId.from "Rx-Interfaces"; Package.Version=PackageVersion.Free }
                                                      { Package.Id=PackageId.from "Rx-Linq"; Package.Version=PackageVersion.Free }
                                                      { Package.Id=PackageId.from "Rx-PlatformServices"; Package.Version=PackageVersion.Free }
                                                      { Package.Id=PackageId.from "FSharp.Data"; Package.Version=PackageVersion.Constraint "2.2.5" }
                                                      { Package.Id=PackageId.from "FsUnit"; Package.Version=PackageVersion.Constraint "1.3.0.1" }
                                                      { Package.Id=PackageId.from "Mini"; Package.Version=PackageVersion.Constraint "0.4.2.0" }
                                                      { Package.Id=PackageId.from "Newtonsoft.Json"; Package.Version=PackageVersion.Constraint "7.0.1" }
                                                      { Package.Id=PackageId.from "NLog"; Package.Version=PackageVersion.Constraint "4.0.1" }
                                                      { Package.Id=PackageId.from "NUnit"; Package.Version=PackageVersion.Constraint "2.6.3" }
                                                      { Package.Id=PackageId.from "xunit"; Package.Version=PackageVersion.Constraint "1.9.1" } ]
                            ProjectReferences = Set [ ProjectId.from "cassandrasharp.interfaces" ] }

    let projectFile = FileInfo (testFile "./TestCases/ConvertedProject.csproj")
    let prjDescriptor = Parsers.MSBuild.parseProjectContent (XDocumentLoader true) projectFile.Directory (RepositoryId.from "Test") false projectFile

    prjDescriptor.Project.ProjectReferences |> should equal [ProjectId.from "cassandrasharp.interfaces"]
    prjDescriptor.Packages |> should equal expectedPackages
    prjDescriptor.Project |> should equal expectedProject

[<Test>]
let CheckParseConvertedProjectWithoutPackagesConfig () =
    let expectedPackages = Set [ { Package.Id=PackageId.from "Rx-Core"; Package.Version=PackageVersion.Free }
                                 { Package.Id=PackageId.from "Rx-Interfaces"; Package.Version=PackageVersion.Free }
                                 { Package.Id=PackageId.from "Rx-Linq"; Package.Version=PackageVersion.Free }
                                 { Package.Id=PackageId.from "Rx-PlatformServices"; Package.Version=PackageVersion.Free } ]

    let expectedProject = { Repository = RepositoryId.from "Test"
                            ProjectId = ProjectId.from "CassandraSharp"
                            RelativeProjectFile = ProjectRelativeFile "ConvertedProject.csproj"
                            Output = AssemblyId.from "CassandraSharp"
                            OutputType = OutputType.Dll
                            HasTests = false
                            PackageReferences = Set [ { Package.Id=PackageId.from "Rx-Core"; Package.Version=PackageVersion.Free }
                                                      { Package.Id=PackageId.from "Rx-Interfaces"; Package.Version=PackageVersion.Free }
                                                      { Package.Id=PackageId.from "Rx-Linq"; Package.Version=PackageVersion.Free }
                                                      { Package.Id=PackageId.from "Rx-PlatformServices"; Package.Version=PackageVersion.Free } ]
                            ProjectReferences = Set [ ProjectId.from "cassandrasharp.interfaces" ] }

    let projectFile = FileInfo (testFile "./TestCases/ConvertedProject.csproj")
    let prjDescriptor = Parsers.MSBuild.parseProjectContent (XDocumentLoader false) projectFile.Directory (RepositoryId.from "Test") false projectFile
    prjDescriptor.Project.ProjectReferences |> should equal [ProjectId.from "cassandrasharp.interfaces"]

    prjDescriptor.Packages |> should equal expectedPackages
    prjDescriptor.Project |> should equal expectedProject

[<Test>]
let CheckParseInvalidProject () =
    let projectFile = FileInfo (testFile "./TestCases/ProjectWithInvalidRefs.csproj")
    let getPrjDescriptor = (fun () -> Parsers.MSBuild.parseProjectContent (XDocumentLoader true) projectFile.Directory (RepositoryId.from "Test") false projectFile |> ignore)
    getPrjDescriptor |> should throw typeof<System.Exception>
