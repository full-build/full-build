﻿//   Copyright 2014-2016 Pierre Chalamet
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
open Anthology
open IoHelpers
open Env
open System.IO


let private checkErrorCode err =
    if err <> 0 then failwithf "Process failed with error %d" err

let private checkedExec = 
    Exec.Exec checkErrorCode


let publishCopy (app : Anthology.Application) =
    let wsDir = GetFolder Env.Workspace
    let antho = Configuration.LoadAnthology ()
    let project = antho.Projects |> Seq.find (fun x -> x.ProjectId = app.Project)
    let repoDir = wsDir |> GetSubDirectory (project.Repository.toString)
    if repoDir.Exists then
        let wsDir = GetFolder Env.Workspace
        let projectDir = wsDir |> GetSubDirectory (project.relativeProjectFolderFromWorkspace)
        let sourceFolder = projectDir |> GetSubDirectory "bin"
        let appDir = GetFolder Env.AppOutput
        let artifactDir = appDir |> GetSubDirectory app.Name.toString
        Bindings.UpdateArtifactBindingRedirects sourceFolder

        IoHelpers.CopyFolder sourceFolder artifactDir false
    else
        printfn "[WARNING] Can't publish application %A without repository" app.Name.toString

let publishZip (app : Anthology.Application) =
    let antho = Configuration.LoadAnthology ()

    let project = antho.Projects |> Seq.find (fun x -> x.ProjectId = app.Project)
    let wsDir = GetFolder Env.Workspace
    let projectDir = wsDir |> GetSubDirectory (project.relativeProjectFolderFromWorkspace)
    let sourceFolder = projectDir |> GetSubDirectory "bin"
    Bindings.UpdateArtifactBindingRedirects sourceFolder

    let appDir = GetFolder Env.AppOutput
    let targetFile = appDir |> GetFile app.Name.toString
    if targetFile.Exists then targetFile.Delete()

    System.IO.Compression.ZipFile.CreateFromDirectory(sourceFolder.FullName, targetFile.FullName, Compression.CompressionLevel.Optimal, false)


let publishDocker (app : Anthology.Application) =
    failwith "not implemented"

//    let tmpApp = { app
//                   with Name = ApplicationId.from "tmpdocker"
//                        Publisher = PublisherType.Copy }
//
//    publishCopy tmpApp
//
//    let wsDir = GetFolder Env.Workspace
//    let prjDir = wsDir |> GetSubDirectory (app.Project.relativeProjectFolderFromWorkspace
//
//    let appDir = GetFolder Env.AppOutput
//    let sourceFolder = appDir |> GetSubDirectory (tmpApp.Name.toString)
//    let targetFile = appDir |> GetFile app.Name.toString
//    if targetFile.Exists then targetFile.Delete()
//
//    let dockerArgs = sprintf "build -t %s ." app.Name.toString
//    Exec.Exec checkErrorCode "docker" dockerArgs sourceFolder
//    sourceFolder.Delete(true)        

let choosePublisher (pubType : PublisherType) appCopy appZip appDocker =
    let publish = match pubType with
                  | PublisherType.Copy -> appCopy
                  | PublisherType.Zip -> appZip
                  | PublisherType.Docker -> appDocker
    publish


let PublishWithPublisher (pubType : PublisherType) =
    choosePublisher pubType publishCopy publishZip publishDocker

