module Workspace

open System.IO
open Types
open FileExtensions
open WellknownFolders
open Exec
open Configuration
open Vcs

let Init (path : WorkspacePath) =
    let wsDir = new DirectoryInfo(path)
    wsDir.Create()

    let subDir = wsDir |> GetSubDirectory WORKSPACE_CONFIG_FOLDER
    if subDir.Exists then failwith "Workspace already exists"

    VcsCloneRepo wsDir GlobalConfig.Repository 


