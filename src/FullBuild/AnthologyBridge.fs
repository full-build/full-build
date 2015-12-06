module AnthologyBridge
open Anthology


let RelativeProjectFolderFromWorkspace (project : Project) =
    let relativePath = project.RelativeProjectFile.toString |> System.IO.Path.GetDirectoryName
    let path = sprintf "%s/%s" project.Repository.toString  relativePath
    path
