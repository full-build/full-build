//   Copyright 2014-2015 Pierre Chalamet
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

open System
open System.IO
open IoHelpers

let private BIN_FOLDER = "bin"
let private VIEW_FOLDER = "views"
let private PROJECT_FOLDER = "projects"
let private APP_FOLDER = "apps"
let private PACKAGE_FOLDER = "packages"
let ANTHOLOGY_FILENAME = "anthology"
let BASELINE_FILENAME = "baseline"
let MASTER_REPO = ".full-build"
let MSBUILD_SOLUTION_DIR = "$(SolutionDir)"
let MSBUILD_TARGETFX_DIR = "$(TargetFrameworkVersion)"
let MSBUILD_BIN_OUTPUT = "bin"
let MSBUILD_APP_OUTPUT = "apps"
let MSBUILD_PROJECT_FOLDER = sprintf "%s/%s/%s/" MSBUILD_SOLUTION_DIR MASTER_REPO PROJECT_FOLDER
let MSBUILD_PACKAGE_FOLDER = sprintf "%s/%s/%s/" MSBUILD_SOLUTION_DIR MASTER_REPO PACKAGE_FOLDER
let MSBUILD_BIN_FOLDER = sprintf "%s/%s" MSBUILD_SOLUTION_DIR MSBUILD_BIN_OUTPUT
let MSBUILD_NUGET_FOLDER = sprintf "../%s/" PACKAGE_FOLDER

let IsWorkspaceFolder(wsDir : DirectoryInfo) = 
    let subDir = wsDir |> GetSubDirectory MASTER_REPO
    subDir.Exists

let rec private WorkspaceFolderSearch(dir : DirectoryInfo) = 
    if dir = null || not dir.Exists then failwith "Can't find workspace root directory. Check you are in a workspace."
    if IsWorkspaceFolder dir then dir
    else WorkspaceFolderSearch dir.Parent


type Folder = 
       | Workspace
       | BinOutput
       | AppOutput
       | Config
       | View
       | App
       | Project
       | Package


let rec GetFolder folder =
    match folder with
    | Workspace -> CurrentFolder() |> WorkspaceFolderSearch 
    | BinOutput -> GetFolder Workspace |> CreateSubDirectory MSBUILD_BIN_OUTPUT
    | AppOutput -> GetFolder Workspace |> CreateSubDirectory MSBUILD_APP_OUTPUT
    | Config -> GetFolder Workspace |> CreateSubDirectory MASTER_REPO
    | View -> GetFolder Config |> CreateSubDirectory VIEW_FOLDER
    | App -> GetFolder Config |> CreateSubDirectory APP_FOLDER
    | Project -> GetFolder Config |> CreateSubDirectory PROJECT_FOLDER
    | Package -> GetFolder Config |> CreateSubDirectory PACKAGE_FOLDER


let GetAnthologyFileName() = 
    GetFolder Config |> GetFile ANTHOLOGY_FILENAME

let GetBaselineFileName() = 
    GetFolder Config  |> GetFile BASELINE_FILENAME


let IsMono () =
    let monoRuntime = System.Type.GetType ("Mono.Runtime") 
    monoRuntime <> null

let CheckLicense () =
    let fbAssembly = System.Reflection.Assembly.GetExecutingAssembly().Location |> FileInfo
    let licFile = fbAssembly.Directory |> GetFile "LICENSE.txt"
    if not (licFile.Exists) then failwithf "Please ensure original LICENSE.txt is available."

    let licContent = File.ReadAllText (licFile.FullName)
    let guid = StringHelpers.GenerateGuidFromString licContent
    let licGuid = StringHelpers.ParseGuid "dc2991a6-9f65-a56c-26ac-3ba65d875d80"
    if guid <> licGuid then failwithf "Please ensure original LICENSE.txt is available."
