//   Copyright 2014-2016 Pierre Chalamet
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

module Conversion
open Anthology
open System.IO
open System.Xml.Linq


let XDocumentLoader (fileName : FileInfo) =
    XDocument.Load fileName.FullName

let XDocumentSaver (fileName : FileInfo) (xdoc : XDocument) =
    xdoc.Save (fileName.FullName)


let convertMsBuild repos =
    let antho = Configuration.LoadAnthology ()
    let projects = antho.Projects |> Set.filter (fun x -> repos |> Set.contains x.Repository)

    MsBuildConversion.GenerateProjects projects XDocumentSaver
    MsBuildConversion.ConvertProjects projects XDocumentLoader XDocumentSaver
    MsBuildConversion.RemoveUselessStuff projects



let Convert builder repos =
    match builder with
    | BuilderType.MSBuild -> convertMsBuild repos
