// Copyright (c) 2014-2015, Pierre Chalamet
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//     * Redistributions of source code must retain the above copyright
//       notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright
//       notice, this list of conditions and the following disclaimer in the
//       documentation and/or other materials provided with the distribution.
//     * Neither the name of Pierre Chalamet nor the
//       names of its contributors may be used to endorse or promote products
//       derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL PIERRE CHALAMET BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
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
let MSBUILD_BIN_OUTPUT = "bin"
let MSBUILD_PROJECT_FOLDER = sprintf "%s/%s/%s/" MSBUILD_SOLUTION_DIR MASTER_REPO PROJECT_FOLDER
let MSBUILD_PACKAGE_FOLDER = sprintf "%s/%s/%s/" MSBUILD_SOLUTION_DIR MASTER_REPO PACKAGE_FOLDER
let MSBUILD_BIN_FOLDER = sprintf "%s/%s/" MSBUILD_SOLUTION_DIR MSBUILD_BIN_OUTPUT
let MSBUILD_NUGET_FOLDER = sprintf "../%s/" PACKAGE_FOLDER

let IsWorkspaceFolder(wsDir : DirectoryInfo) = 
    let subDir = wsDir |> GetSubDirectory MASTER_REPO
    subDir.Exists

let rec private WorkspaceFolderSearch(dir : DirectoryInfo) = 
    if dir = null || not dir.Exists then failwith "Can't find workspace root directory. Check you are in a workspace."
    if IsWorkspaceFolder dir then dir
    else WorkspaceFolderSearch dir.Parent

let private CurrentFolder() : DirectoryInfo = 
    DirectoryInfo(Environment.CurrentDirectory)


type Folder = 
       | Workspace
       | Bin
       | Config
       | View
       | App
       | Project
       | Package


let rec GetFolder folder =
    match folder with
    | Workspace -> CurrentFolder() |> WorkspaceFolderSearch 
    | Bin -> GetFolder Workspace |> CreateSubDirectory BIN_FOLDER
    | Config -> GetFolder Workspace |> CreateSubDirectory MASTER_REPO
    | View -> GetFolder Config |> CreateSubDirectory VIEW_FOLDER
    | App -> GetFolder Config |> CreateSubDirectory APP_FOLDER
    | Project -> GetFolder Config |> CreateSubDirectory PROJECT_FOLDER
    | Package -> GetFolder Config |> CreateSubDirectory PACKAGE_FOLDER


let GetAnthologyFileName() = 
    GetFolder Config |> GetFile ANTHOLOGY_FILENAME

let GetBaselineFileName() = 
    GetFolder Config  |> GetFile BASELINE_FILENAME

