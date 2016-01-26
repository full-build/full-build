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

module Env

open System.IO
open IoHelpers
open System.Reflection

let private VIEW_FOLDER = "views"
let private PROJECT_FOLDER = "projects"
let private PACKAGE_FOLDER = "packages"
let BIN_FOLDER = "bin"
let OBJ_FOLDER = "obj"
let ANTHOLOGY_FILENAME = "anthology"
let BASELINE_FILENAME = "baseline"
let FULLBUILD_TARGETS = "full-build.targets"
let MASTER_REPO = ".full-build"
let MSBUILD_SOLUTION_DIR = "$(SolutionDir)"
let MSBUILD_TARGETFX_DIR = "$(TargetFrameworkVersion)"
let MSBUILD_APP_OUTPUT = "apps"
let MSBUILD_PROJECT_FOLDER = sprintf "%s/%s/%s/" MSBUILD_SOLUTION_DIR MASTER_REPO PROJECT_FOLDER
let MSBUILD_PACKAGE_FOLDER = sprintf "%s/%s/%s/" MSBUILD_SOLUTION_DIR MASTER_REPO PACKAGE_FOLDER
let MSBUILD_BIN_FOLDER = sprintf "%s/%s/%s" MSBUILD_SOLUTION_DIR MASTER_REPO BIN_FOLDER
let MSBUILD_NUGET_FOLDER = sprintf "../%s/" PACKAGE_FOLDER
let MSBUILD_FULLBUILD_TARGETS = sprintf "%s/%s/%s" MSBUILD_SOLUTION_DIR MASTER_REPO FULLBUILD_TARGETS
let PUBLISH_BIN_FOLDER = BIN_FOLDER
let PUBLISH_APPS_FOLDER = MSBUILD_APP_OUTPUT

let IsWorkspaceFolder(wsDir : DirectoryInfo) = 
    let subDir = wsDir |> GetSubDirectory MASTER_REPO
    subDir.Exists

let rec private WorkspaceFolderSearch(dir : DirectoryInfo) = 
    if dir = null || not dir.Exists then failwith "Can't find workspace root directory. Check you are in a workspace."
    if IsWorkspaceFolder dir then dir
    else WorkspaceFolderSearch dir.Parent

type DummyType () = class end

let GetFullBuildAssembly () =
    let fbAssembly = typeof<DummyType>.GetTypeInfo().Assembly
    fbAssembly

let private getInstallationFolder () =
    let fbAssembly = GetFullBuildAssembly ()
    let fbAssFI = fbAssembly.Location |> FileInfo
    fbAssFI.Directory

type Folder = 
       | Workspace
       | AppOutput
       | Config
       | View
       | Project
       | Package
       | Bin
       | Installation


let rec GetFolder folder =
    match folder with
    | Workspace -> CurrentFolder() |> WorkspaceFolderSearch 
    | AppOutput -> GetFolder Workspace |> CreateSubDirectory MSBUILD_APP_OUTPUT
    | Config -> GetFolder Workspace |> CreateSubDirectory MASTER_REPO
    | View -> GetFolder Config |> CreateSubDirectory VIEW_FOLDER
    | Project -> GetFolder Config |> CreateSubDirectory PROJECT_FOLDER
    | Package -> GetFolder Config |> CreateSubDirectory PACKAGE_FOLDER
    | Bin -> GetFolder Config |> CreateSubDirectory BIN_FOLDER
    | Installation -> getInstallationFolder()

let GetAnthologyFileName() = 
    GetFolder Config |> GetFile ANTHOLOGY_FILENAME

let GetBaselineFileName() = 
    GetFolder Config  |> GetFile BASELINE_FILENAME


let IsMono () =
    let monoRuntime = System.Type.GetType ("Mono.Runtime") 
    monoRuntime <> null

let CheckLicense () =
    let fbInstallDir = GetFolder Installation
    let licFile = fbInstallDir |> GetFile "LICENSE.txt"
    if not (licFile.Exists) then failwithf "Please ensure original LICENSE.txt is available."

    let licContent = File.ReadAllText (licFile.FullName)
    let guid = StringHelpers.GenerateGuidFromString licContent
    let licGuid = StringHelpers.ParseGuid "21a734e7-1308-06de-3905-7708ed4c4dbc"
    if guid <> licGuid then failwithf "Please ensure original LICENSE.txt is available."
