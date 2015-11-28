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



let rec ComputeProjectDependencies (antho : Anthology) (projectIds : ProjectId set) =
    let projects = antho.Projects |> Set.filter (fun x -> projectIds |> Set.contains x.ProjectId)
    let dependencies = projects |> Set.map (fun x -> x.ProjectReferences) 
                                |> Set.unionMany

    if dependencies = Set.empty then projectIds
    else ComputeProjectDependencies antho dependencies |> Set.union projectIds


let BuildDeployTargetContent (projects : Project set) =
    let items = projects |> Seq.map (fun x -> sprintf "../../bin/%s/**/*.*" x.Output.toString)
                         |> Seq.map (fun x -> XElement(NsMsBuild + "ProjectFiles", XAttribute(NsNone + "Include", x)))

    XDocument (
        XElement(NsMsBuild + "Project", XAttribute(NsNone + "DefaultTargets", "Publish"),
            XElement(NsMsBuild + "Target", XAttribute (NsNone + "Name", "Publish"),
                XElement(NsMsBuild + "ItemGroup", items),
                XElement(NsMsBuild + "RemoveDir", XAttribute(NsNone + "Directories", "../../apps/$(MSBuildProjectName)")),
                XElement(NsMsBuild + "Copy", XAttribute(NsNone + "SourceFiles", "@(ProjectFiles)"), XAttribute(NsNone + "DestinationFolder", "../../apps/$(MSBuildProjectName)")))))



let publishAppCopy (app : Anthology.Application) (projects : Anthology.Project set) =
    let appDir = GetFolder Env.App
    let wsDir = GetFolder Env.Workspace
    let appFile = appDir |> GetFile (AddExt Targets (app.Name.toString))
    let appContent = BuildDeployTargetContent projects
    appContent.Save (appFile.FullName)

    let args = sprintf "/nologo /v:m %A" appFile.FullName

    if Env.IsMono () then checkedExec "xbuild" args wsDir
    else checkedExec "msbuild" args wsDir


let PublishAppSelector (app : Anthology.Application) (projects : Anthology.Project set) appCopy =
    let publish = match app.Publisher with
                  | PublisherType.Copy -> appCopy
    publish app projects


let publishApp (app : Anthology.Application) (projects : Anthology.Project set) =
    PublishAppSelector app projects publishAppCopy


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
        let projectIds = app.Projects |> ComputeProjectDependencies antho
        let projects = antho.Projects |> Set.filter (fun x -> projectIds |> Set.contains x.ProjectId)
        publishApp app projects

let List () =
    let antho = Configuration.LoadAnthology ()
    antho.Applications |> Seq.iter (fun x -> printfn "%s" (x.Name.toString))

let Add (appName : ApplicationId) (projects : ProjectId set) (publisher : Anthology.PublisherType) =
    let antho = Configuration.LoadAnthology ()
    let app = { Name = appName
                Publisher = publisher
                Projects = projects }

    let newAntho = { antho
                     with Applications = antho.Applications |> Set.add app }

    Configuration.SaveAnthology newAntho

let Drop (appName : ApplicationId) =
    let antho = Configuration.LoadAnthology ()

    let newAntho = { antho
                     with Applications = antho.Applications |> Set.filter (fun x -> x.Name <> appName) }

    Configuration.SaveAnthology newAntho
