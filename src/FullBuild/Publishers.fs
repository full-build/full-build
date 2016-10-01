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

module Publishers
open IoHelpers
open Env
open System.IO
open Graph

let private checkErrorCode err =
    if err <> 0 then failwithf "Process failed with error %d" err

let private checkedExec =
    Exec.Exec checkErrorCode


type PublishApp =
    { Name : string
      App : Application }


let publishCopy (app : PublishApp) =
    let wsDir = GetFolder Env.Folder.Workspace
    let antho = Configuration.LoadAnthology ()
    let projects = app.App.Projects
    for project in projects do
        let repoDir = wsDir |> GetSubDirectory (project.Repository.Name)
        if repoDir.Exists then
            let projFile = repoDir |> GetFile project.RelativeProjectFile
            let args = sprintf "/nologo /t:FBPublish /p:SolutionDir=%A /p:FBApp=%A %A" wsDir.FullName app.Name projFile.FullName

            if Env.IsMono () then checkedExec "xbuild" args wsDir
            else checkedExec "msbuild" args wsDir

            let appDir = GetFolder Env.Folder.AppOutput
            let artifactDir = appDir |> GetSubDirectory app.Name
            Bindings.UpdateArtifactBindingRedirects artifactDir
        else
            printfn "[WARNING] Can't publish application %A without repository" app.Name

let publishZip (app : PublishApp) =
    let tmpApp = { Name = ".tmp-" + app.Name
                   App = app.App }
    publishCopy tmpApp

    let appDir = GetFolder Env.Folder.AppOutput
    let sourceFolder = appDir |> GetSubDirectory (tmpApp.Name)
    let targetFile = appDir |> GetFile app.Name
    if targetFile.Exists then targetFile.Delete()

    System.IO.Compression.ZipFile.CreateFromDirectory(sourceFolder.FullName, targetFile.FullName, Compression.CompressionLevel.Optimal, false)

let publishDocker (app : PublishApp) =
    let tmpApp = { Name = ".tmp-docker"
                   App = app.App }
    publishCopy tmpApp

    let appDir = GetFolder Env.Folder.AppOutput
    let sourceFolder = appDir |> GetSubDirectory (tmpApp.Name)
    let targetFile = appDir |> GetFile app.Name
    if targetFile.Exists then targetFile.Delete()

    let dockerArgs = sprintf "build -t %s ." app.Name
    Exec.Exec checkErrorCode "docker" dockerArgs sourceFolder
    sourceFolder.Delete(true)

let choosePublisher (pubType : PublisherType) appCopy appZip appDocker =
    let publish = match pubType with
                  | PublisherType.Copy -> appCopy
                  | PublisherType.Zip -> appZip
                  | PublisherType.Docker -> appDocker
    publish


let PublishWithPublisher (app : Application) =
    let pubApp = { Name = app.Name
                   App = app }
    (choosePublisher app.Publisher publishCopy publishZip publishDocker) pubApp


