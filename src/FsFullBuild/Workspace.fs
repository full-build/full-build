module Workspace

open System.IO
open Types
open FileExtensions
open WellknownFolders

let Init (path : WorkspacePath) =
    let pathDir = new DirectoryInfo(path)
    pathDir.Create()

    let subDir = pathDir |> GetSubDirectory WORKSPACE_CONFIG_FOLDER
    if subDir.Exists then failwith "Workspace already exists"
