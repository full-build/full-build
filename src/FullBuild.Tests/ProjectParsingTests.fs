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
    | "packages.config" -> if loadPackagesConfig then Some (XDocument.Load (testFile "packages.xml"))
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
let CheckBasicParsingFSharp () =
    let file = FileInfo (testFile "FSharpProjectSample1.fsproj")
    let prjDescriptor = Parsers.MSBuild.parseProjectContent (XDocumentLoader true) file.Directory (RepositoryId.from "Test") false file
    ()

[<Test>]
let CheckParseInvalidProject () =
    let projectFile = FileInfo (testFile "ProjectWithInvalidRefs.csproj")
    let getPrjDescriptor = (fun () -> Parsers.MSBuild.parseProjectContent (XDocumentLoader true) projectFile.Directory (RepositoryId.from "Test") false projectFile |> ignore)
    getPrjDescriptor |> should throw typeof<System.Exception>
