//   Copyright 2014-2015 Pierre Chalamet
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

module Application
open Anthology
open Collections
open System.IO
open Env
open IoHelpers
open System.Xml.Linq
open MsBuildHelpers


let private checkErrorCode err =
    if err <> 0 then failwithf "Process failed with error %d" err

let private checkedExec = 
    Exec.Exec checkErrorCode


let publishAppCopy (app : Anthology.Application) =
    let wsDir = GetFolder Env.Workspace
    let antho = Configuration.LoadAnthology ()
    let project = antho.Projects |> Seq.find (fun x -> x.ProjectId = app.Project)
    let repoDir = wsDir |> GetSubDirectory (project.Repository.toString)
    if repoDir.Exists then
        let projFile = repoDir |> GetFile project.RelativeProjectFile.toString 
        let args = sprintf "/nologo /t:FBPublish /p:SolutionDir=%A /p:FBApp=%A %A" wsDir.FullName app.Name.toString projFile.FullName

        if Env.IsMono () then checkedExec "xbuild" args wsDir
        else checkedExec "msbuild" args wsDir
    else
        printfn "[WARNING] Can't publish application %A without repository" app.Name.toString

let publishAppZip (app : Anthology.Application) =
    let tmpApp = { app
                   with Name = ApplicationId.from "tmpzip"
                        Publisher = PublisherType.Copy }

    publishAppCopy tmpApp

    let appDir = GetFolder Env.AppOutput
    let sourceFolder = appDir |> GetSubDirectory (tmpApp.Name.toString)
    let targetFile = appDir |> GetFile (AddExt Extension.Zip app.Name.toString)
    if targetFile.Exists then targetFile.Delete()

    System.IO.Compression.ZipFile.CreateFromDirectory(sourceFolder.FullName, targetFile.FullName, Compression.CompressionLevel.Optimal, false)
    sourceFolder.Delete(true)
    

let choosePublisher (pubType : PublisherType) appCopy appZip =
    let publish = match pubType with
                  | PublisherType.Copy -> appCopy
                  | PublisherType.Zip -> appZip
    publish


let publishWithPublisher (pubType : PublisherType) =
    choosePublisher pubType publishAppCopy publishAppZip


let Publish (filters : string list) =
    let antho = Configuration.LoadAnthology ()
    let appNames = antho.Applications |> Set.map (fun x -> x.Name.toString)

    let appFilters = filters |> Set
    let matchApps filter = appNames |> Set.filter (fun x -> PatternMatching.Match x filter)
    let matches = appFilters |> Set.map matchApps
                             |> Set.unionMany
                             |> Set.map ApplicationId.from

    for appName in matches do
        let app = antho.Applications |> Seq.find (fun x -> x.Name = appName)
        (publishWithPublisher app.Publisher) app

let List () =
    let antho = Configuration.LoadAnthology ()
    antho.Applications |> Seq.iter (fun x -> printfn "%s" (x.Name.toString))

let Add (appName : ApplicationId) (project : ProjectId) (publisher : Anthology.PublisherType) =
    let antho = Configuration.LoadAnthology ()
    let app = { Name = appName
                Publisher = publisher
                Project = project }

    let newAntho = { antho
                     with Applications = antho.Applications |> Set.add app }

    Configuration.SaveAnthology newAntho

let Drop (appName : ApplicationId) =
    let antho = Configuration.LoadAnthology ()

    let newAntho = { antho
                     with Applications = antho.Applications |> Set.filter (fun x -> x.Name <> appName) }

    Configuration.SaveAnthology newAntho
