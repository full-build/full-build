module Workspace

open Types
open FileExtensions
open Constants

let Init (relativePath : RelativePath) =
    relativePath.Create()

    let subDir = relativePath |> GetSubDirectory FullBuildFolder
    if subDir.Exists then failwith "Workspace already exists"
