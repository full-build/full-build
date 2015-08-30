module MsBuildHelpersTests

open FsUnit
open NUnit.Framework
open MsBuildHelpers
open System.Xml.Linq
open Anthology
open StringHelpers

[<Test>]
let CheckCast () =
    let xel = XElement (NsNone + "Toto", 42)
    let i = !> xel : int
    i |> should equal 42

[<Test>]
let CheckProjectPropertyName () =
    let project = { Output = AssemblyId.Bind "cqlplus"
                    OutputType = OutputType.Exe
                    ProjectGuid = ProjectId (ParseGuid "0a06398e-69be-487b-a011-4c0be6619b59")
                    ProjectType = ProjectType (ParseGuid "FAE04EC0-301F-11D3-BF4B-00C04F79EFBC")
                    RelativeProjectFile = ProjectRelativeFile "cqlplus/cqlplus-net45.csproj"
                    FxTarget = FrameworkVersion "v4.5"
                    ProjectReferences = [ ProjectId.Bind(ParseGuid "6f6eb447-9569-406a-a23b-c09b6dbdbe10"); ProjectId(ParseGuid "c1d252b7-d766-4c28-9c46-0696f896846c") ] |> set
                    PackageReferences = [ ] |> set
                    AssemblyReferences = [ AssemblyId.Bind("System") ; AssemblyId.Bind("System.Xml") ] |> set
                    Repository = RepositoryId.Bind "cassandra-sharp" }

    let propName = ProjectPropertyName project
    propName |> should equal "Prj_0a06398e_69be_487b_a011_4c0be6619b59"

[<Test>]
let CheckPackagePropertyName () =
    let package = "Rx-Core"
    let propName = PackagePropertyName package
    propName |> should equal "FullBuild_Rx_Core_Pkg"
