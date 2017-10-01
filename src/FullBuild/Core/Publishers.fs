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

module Core.Publishers
open FsHelpers
open Env
open System.IO
open Graph
open Exec
open Views


type App =
    { App : Application
      PublishAs : string
      View : View }


let private publishCopy (app : App) =
    let wsDir = GetFolder Env.Folder.Workspace
    let appDir = GetFolder Env.Folder.AppOutput |> GetSubDirectory (app.PublishAs)
    let project = app.App.Project
    let repoDir = wsDir |> GetSubDirectory (project.Repository.Name)
    let projFile = repoDir |> GetFile project.ProjectFile
    let args = sprintf "/nologo /t:Publish /p:PublishDir=%A /p:SolutionDir=%A /p:SolutionName=%A %A" 
                    appDir.FullName wsDir.FullName app.View.Name projFile.FullName

    Exec "msbuild" args wsDir Map.empty |> IO.CheckResponseCode

let private publishZip (app : App) =
    let tmpApp = { app
                   with PublishAs = ".tmp-" + app.PublishAs }
    publishCopy tmpApp

    let appDir = GetFolder Env.Folder.AppOutput
    let sourceFolder = appDir |> GetSubDirectory (tmpApp.PublishAs)
    let targetFile = appDir |> GetFile (app.App.Name + ".zip")
    if targetFile.Exists then targetFile.Delete()

    System.IO.Compression.ZipFile.CreateFromDirectory(sourceFolder.FullName, targetFile.FullName, Compression.CompressionLevel.Fastest, false)

let private publishDocker (app : App) =
    let tmpApp = { app
                   with PublishAs = ".tmp-docker" }
    publishCopy tmpApp

    let appDir = GetFolder Env.Folder.AppOutput
    let sourceFolder = appDir |> GetSubDirectory (tmpApp.PublishAs)
    let targetFile = appDir |> GetFile app.App.Name
    if targetFile.Exists then targetFile.Delete()

    let dockerArgs = sprintf "build -t %s ." app.App.Name
    Exec "docker" dockerArgs sourceFolder Map.empty |> IO.CheckResponseCode

    let imgFile = appDir |> GetSubDirectory app.App.Name
    let saveArgs = sprintf "save -o %s %s" imgFile.FullName app.App.Name
    Exec "docker" saveArgs sourceFolder Map.empty |> IO.CheckResponseCode
    sourceFolder.Delete(true)

let PublishWithPublisher (view : View) (app : Application) =
    let publisher = match app.Publisher with
                    | PublisherType.Copy -> publishCopy
                    | PublisherType.Zip -> publishZip
                    | PublisherType.Docker -> publishDocker

    { App = app; PublishAs = app.Name; View = view } |> publisher 
