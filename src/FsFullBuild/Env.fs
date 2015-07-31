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

let private WORKSPACE_CONFIG_FOLDER = ".full-build"
let private WORKSPACE_VIEW_FOLDER = "views"
let private WORKSPACE_PROJECT_FOLDER = "projects"
let private WORKSPACE_PACKAGE_FOLDER = "packages"
let private ANTHOLOGY_FILENAME = "anthology.json"

let IsWorkspaceFolder(wsDir : DirectoryInfo) = 
    let subDir = WORKSPACE_CONFIG_FOLDER |> GetSubDirectory wsDir
    subDir.Exists

let rec private WorkspaceFolderSearch(dir : DirectoryInfo) = 
    if dir = null || not dir.Exists then failwith "Can't find workspace root directory. Check you are in a workspace."
    if IsWorkspaceFolder dir then dir
    else WorkspaceFolderSearch dir.Parent

let private CurrentFolder() : DirectoryInfo = new DirectoryInfo(Environment.CurrentDirectory)

// $
let WorkspaceFolder() : DirectoryInfo = 
    let currDir = CurrentFolder()
    WorkspaceFolderSearch currDir

// $/.full-build/views
let WorkspaceConfigFolder() : DirectoryInfo = 
    let wsDir = WorkspaceFolder()
    CreateSubDirectory wsDir WORKSPACE_CONFIG_FOLDER

// $/.full-build/views
let WorkspaceViewFolder() : DirectoryInfo =
    let wsDir = WorkspaceConfigFolder()
    CreateSubDirectory wsDir WORKSPACE_VIEW_FOLDER

// $/.full-build/projects
let WorkspaceProjectFolder() : DirectoryInfo =
    let wsDir = WorkspaceConfigFolder()
    CreateSubDirectory wsDir WORKSPACE_PROJECT_FOLDER

// $/.full-build/packages
let WorkspacePackageFolder() : DirectoryInfo =
    let wsDir = WorkspaceConfigFolder()
    CreateSubDirectory wsDir WORKSPACE_PACKAGE_FOLDER



let GetAnthologyFileName() = 
    let fbDir = WorkspaceConfigFolder()
    ANTHOLOGY_FILENAME |> GetFile fbDir
