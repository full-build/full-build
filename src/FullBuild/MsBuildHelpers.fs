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

module MSBuildHelpers
open Graph

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
