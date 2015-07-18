module ProjectParsingTests

open System
open System.IO
open ProjectParsing
open NUnit.Framework
open FsUnit

[<Test>]
let CheckBasicParsing () =
    let file = new FileInfo ("./CSharpProjectSample1.xml")
    let project = ProjectParsing.ParseProject file.Directory file
    project.ProjectGuid |> should equal (ProjectParsing.ParseGuid "3AF55CC8-9998-4039-BC31-54ECBFC91396")

[<Test>]
let CheckParseVirginProject () =
    let file = new FileInfo ("./VirginProject.xml")
    let project = ProjectParsing.ParseProject file.Directory file
    project.ProjectGuid |> should equal (ProjectParsing.ParseGuid "C1D252B7-D766-4C28-9C46-0696F896846C")
    project.ProjectReferences |> should equal [ProjectParsing.ParseGuid "c1d252b7-d766-4c28-9c46-0696f896846d"]

[<Test>]
let CheckParseConvertedProject () =
    let file = new FileInfo ("./ConvertedProject.xml")
    let project = ProjectParsing.ParseProject file.Directory file
    project.ProjectGuid |> should equal (ProjectParsing.ParseGuid "c1d252b7-d766-4c28-9c46-0696f896846d")
    project.ProjectReferences |> should equal [ProjectParsing.ParseGuid "6f6eb447-9569-406a-a23b-c09b6dbdbe10"]
