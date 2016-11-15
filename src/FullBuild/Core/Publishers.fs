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

module Core.Publishers
open IoHelpers
open Env
open System.IO
open Graph

let private checkErrorCode (execResult:Exec.ExecResult) =
    if execResult.ResultCode < 0 then failwithf "Process failed with error %d" execResult.ResultCode


type private PublishApp =
    { Name : string
      App : Application }


let private publishCopy (app : PublishApp) =
    let wsDir = GetFolder Env.Folder.Workspace
    let projects = app.App.Projects
    for project in projects do
        let repoDir = wsDir |> GetSubDirectory (project.Repository.Name)
        if repoDir.Exists then
            let projFile = repoDir |> GetFile project.ProjectFile
            let args = sprintf "/nologo /t:FBPublish /p:SolutionDir=%A /p:FBApp=%A %A" wsDir.FullName app.Name projFile.FullName

            if Env.IsMono () then Exec.Exec "xbuild" args wsDir Map.empty |> checkErrorCode
            else Exec.Exec "msbuild" args wsDir Map.empty |> checkErrorCode

            let appDir = GetFolder Env.Folder.AppOutput
            let artifactDir = appDir |> GetSubDirectory app.Name
            Bindings.UpdateArtifactBindingRedirects artifactDir
        else
            printfn "[WARNING] Can't publish application %A without repository" app.Name

let private publishZip (app : PublishApp) =
    let tmpApp = { Name = ".tmp-" + app.Name
                   App = app.App }
    publishCopy tmpApp

    let appDir = GetFolder Env.Folder.AppOutput
    let sourceFolder = appDir |> GetSubDirectory (tmpApp.Name)
    let targetFile = appDir |> GetFile app.Name
    if targetFile.Exists then targetFile.Delete()

    System.IO.Compression.ZipFile.CreateFromDirectory(sourceFolder.FullName, targetFile.FullName, Compression.CompressionLevel.Optimal, false)

let private publishDocker (app : PublishApp) =
    let tmpApp = { Name = ".tmp-docker"
                   App = app.App }
    publishCopy tmpApp

    let appDir = GetFolder Env.Folder.AppOutput
    let sourceFolder = appDir |> GetSubDirectory (tmpApp.Name)
    let targetFile = appDir |> GetFile app.Name
    if targetFile.Exists then targetFile.Delete()

    let dockerArgs = sprintf "build -t %s ." app.Name
    Exec.Exec "docker" dockerArgs sourceFolder Map.empty |> checkErrorCode
    sourceFolder.Delete(true)

let private publishNuget (app : PublishApp) =
    let tmpApp = { Name = ".tmp-nuget-" + app.Name
                   App = app.App }
    publishCopy tmpApp

    let appDir = GetFolder Env.Folder.AppOutput
    let sourceFolder = appDir |> GetSubDirectory (tmpApp.Name)
    
    let nuspec = sourceFolder.EnumerateFiles("*.nuspec") 
                    |> Seq.tryHead 

    match nuspec with
    | Some nuspecFile ->
        let version =  defaultArg (Builders.getCurrentBuildVersion()) "1.0.0"
        let nugetArgs = sprintf "pack %s -version %s" nuspecFile.Name version
        Exec.Exec "nuget" nugetArgs sourceFolder Map.empty |> Exec.CheckResponseCode
        for file in sourceFolder.EnumerateFiles("*.nupkg") do file.CopyTo(Path.Combine(appDir.FullName, file.Name)) |> ignore
        sourceFolder.Delete(true)
    | None -> failwith (sprintf "No nuspec found for the application %s" app.Name)

let PublishWithPublisher (app : Application) =
    let publisher = 
        match app.Publisher with
        | PublisherType.Copy -> publishCopy
        | PublisherType.Zip -> publishZip
        | PublisherType.Docker -> publishDocker
        | PublisherType.NuGet -> publishNuget
    { Name = app.Name; App = app }
        |> publisher


