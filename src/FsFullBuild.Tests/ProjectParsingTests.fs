module ProjectParserTests

open System
open System.IO
open ProjectParser
open NUnit.Framework
open FsUnit

[<Test>]
let CheckBasicParsingCSharp () =
    let file = new FileInfo ("./CSharpProjectSample1.xml")
    let prjDescriptor = ProjectParser.ParseProject file.Directory file
    prjDescriptor.Project.ProjectGuid |> should equal (ProjectParser.ParseGuid "3AF55CC8-9998-4039-BC31-54ECBFC91396")

[<Test>]
let CheckBasicParsingFSharp () =
    let file = new FileInfo ("./FSharpProjectSample1.xml")
    let sources = ProjectParser.ParseProject file.Directory file
    let prjDescriptor = ProjectParser.ParseProject file.Directory file
    prjDescriptor.Project.ProjectGuid |> should equal (ProjectParser.ParseGuid "5fde3939-c144-4287-bc57-a96ec2d1a9da")

[<Test>]
let CheckParseVirginProject () =
    let file = new FileInfo ("./VirginProject.xml")
    let project = ProjectParser.ParseProject file.Directory file
    let sources = ProjectParser.ParseProject file.Directory file
    let prjDescriptor = ProjectParser.ParseProject file.Directory file
    prjDescriptor.Project.ProjectReferences |> should equal [ProjectParser.ParseGuid "c1d252b7-d766-4c28-9c46-0696f896846d"]

[<Test>]
let CheckParseConvertedProject () =
    let file = new FileInfo ("./ConvertedProject.xml")
    let project = ProjectParser.ParseProject file.Directory file
    let sources = ProjectParser.ParseProject file.Directory file
    let prjDescriptor = ProjectParser.ParseProject file.Directory file
    prjDescriptor.Project.ProjectReferences |> should equal [ProjectParser.ParseGuid "6f6eb447-9569-406a-a23b-c09b6dbdbe10"]
