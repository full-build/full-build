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

module MsBuildHelpers
open Graph
open System.Xml.Linq

#nowarn "0077" // op_Explicit


let NsMsBuild = XNamespace.Get("http://schemas.microsoft.com/developer/msbuild/2003")

let NsDgml = XNamespace.Get("http://schemas.microsoft.com/vs/2009/dgml")

let NsRuntime = XNamespace.Get("urn:schemas-microsoft-com:asm.v1")

let NsNone = XNamespace.None

let inline (!>) (x : ^a) : ^b = (((^a or ^b) : (static member op_Explicit : ^a -> ^b) x))


let replaceInvalidChars (s : string) =
    s.Replace('-', '_').Replace('.', '_').Replace("{", "").Replace("}", "")


let ProjectPropertyName (projectId : Anthology.ProjectId) =
    let prjId = projectId.toString |> replaceInvalidChars
    let prjProp = sprintf "FullBuild_%s" prjId
    prjProp

let PackagePropertyName (packageId : Anthology.PackageId) =
    let pkgId = packageId.toString |> replaceInvalidChars
    let pkgProp = sprintf "FullBuild_%s_Pkg" pkgId
    pkgProp



let MsBuildProjectPropertyName (project : Project) =
    let prjId = project.ProjectId |> replaceInvalidChars
    let prjProp = sprintf "FullBuild_%s" prjId
    prjProp

let MsBuildPackagePropertyName (package : Package) =
    let pkgId = package.Name |> replaceInvalidChars
    let pkgProp = sprintf "FullBuild_%s_Pkg" pkgId
    pkgProp
