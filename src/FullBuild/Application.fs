// Copyright (c) 2014-2015, Pierre Chalamet
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//     * Redistributions of source code must retain the above copyright
//       notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright
//       notice, this list of conditions and the following disclaimer in the
//       documentation and/or other materials provided with the distribution.
//     * Neither the name of Pierre Chalamet nor the
//       names of its contributors may be used to endorse or promote products
//       derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL PIERRE CHALAMET BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
module Application
open Anthology
open Collections
open System.IO
open Env
open IoHelpers
open System.Xml.Linq
open MsBuildHelpers

let rec ComputeProjectDependencies (antho : Anthology) (projectIds : ProjectId set) =
    let projects = antho.Projects |> Set.filter (fun x -> projectIds |> Set.contains x.ProjectGuid)
    let dependencies = projects |> Set.map (fun x -> x.ProjectReferences) 
                                |> Set.unionMany

    if dependencies = Set.empty then dependencies
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

let Deploy (appNames : ApplicationId set) =
    let antho = Configuration.LoadAnthology ()
    let wsDir = GetFolder Env.Workspace
    let appDir = GetFolder Env.App
    for appName in appNames do
        let app = antho.Applications |> Seq.find (fun x -> x.Name = appName)
        let projectIds = app.Projects |> ComputeProjectDependencies antho
        let projects = antho.Projects |> Set.filter (fun x -> projectIds |> Set.contains x.ProjectGuid)

        let appFile = appDir |> GetFile (AddExt Targets (appName.toString))
        let appContent = BuildDeployTargetContent projects
        appContent.Save (appFile.FullName)

        let args = sprintf "/nologo /v:m %A" appFile.FullName

        if Env.IsMono () then Exec.Exec "xbuild" args wsDir
        else Exec.Exec "msbuild" args wsDir

let List () =
    let antho = Configuration.LoadAnthology ()
    antho.Applications |> Seq.iter (fun x -> printfn "%s" (x.Name.toString))

let Add (appName : ApplicationId) (projects : ProjectId set) =
    let antho = Configuration.LoadAnthology ()
    let app = { Name = appName
                Projects = projects }

    let newAntho = { antho
                     with Applications = antho.Applications |> Set.add app }

    Configuration.SaveAnthology newAntho

let Drop (appName : ApplicationId) =
    let antho = Configuration.LoadAnthology ()

    let newAntho = { antho
                     with Applications = antho.Applications |> Set.filter (fun x -> x.Name <> appName) }

    Configuration.SaveAnthology newAntho
