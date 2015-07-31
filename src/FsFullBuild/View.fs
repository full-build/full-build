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

module View
open System.IO
open Env
open IoHelpers
open Anthology
open StringHelpers
open Configuration

let Create (viewName : string) (filters : string list) =
    let repos = filters |> Repo.FilterRepos 
                        |> Seq.map (fun x -> x.Name)
    let vwDir = WorkspaceViewFolder ()
    let vwFile = viewName + ".view" |> GetFile vwDir
    File.WriteAllLines (vwFile.FullName, repos)

let Drop (viewName : string) =
    let vwDir = WorkspaceViewFolder ()
    let vwFile = viewName + ".view"|> GetFile vwDir
    File.Delete (vwFile.FullName)

let List () =
    let vwDir = WorkspaceViewFolder ()
    vwDir.EnumerateFiles ("*.view") |> Seq.iter (fun x -> printfn "%s" (Path.GetFileNameWithoutExtension (x.Name)))

let Describe (viewName : string) =
    let vwDir = WorkspaceViewFolder ()
    let vwFile = viewName + ".view" |> GetFile vwDir
    File.ReadAllLines (vwFile.FullName) |> Seq.iter (fun x -> printfn "%s" x)


let GenerateSolutionContent (projects : Project list) =
    seq {
        yield ""
        yield "Microsoft Visual Studio Solution File, Format Version 12.00"
        yield "# Visual Studio 2013"

        for project in projects do
            yield sprintf @"Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""%s"", ""%s"", ""%s""" 
                  (Path.GetFileNameWithoutExtension (project.RelativeProjectFile))
                  (sprintf "%s/%s" project.Repository project.RelativeProjectFile)
                  (StringifyGuid project.ProjectGuid)

            yield "\tProjectSection(ProjectDependencies) = postProject"
            for dependency in project.ProjectReferences do
                if projects |> Seq.exists (fun x -> x.ProjectGuid = dependency) then
                    let dependencyName = StringifyGuid dependency
                    yield sprintf "\t\t%s = %s" dependencyName dependencyName
            yield "\tEndProjectSection"
            yield "EndProject"

        yield "Global"
        yield "\tGlobalSection(SolutionConfigurationPlatforms) = preSolution"
        yield "\t\tDebug|Any CPU = Debug|Any CPU"
        yield "\t\tRelease|Any CPU = Release|Any CPU"
        yield "\tEndGlobalSection"
        yield "\tGlobalSection(ProjectConfigurationPlatforms) = postSolution"

        for project in projects do
            let guid = StringifyGuid project.ProjectGuid
            yield sprintf "\t\t%s.Debug|Any CPU.ActiveCfg = Debug|Any CPU" guid
            yield sprintf "\t\t%s.Debug|Any CPU.Build.0 = Debug|Any CPU" guid
            yield sprintf "\t\t%s.Release|Any CPU.ActiveCfg = Release|Any CPU" guid
            yield sprintf "\t\t%s.Release|Any CPU.Build.0 = Release|Any CPU" guid

        yield "\tEndGlobalSection"
        yield "EndGlobal"
    }

let SelectProjects (projects : Project seq) (filters : string seq) =
    projects |> Seq.filter (fun x -> Seq.contains x.Repository filters) 
             |> Seq.toList

let Generate (viewName : string) =
    let antho = LoadAnthology ()
    let wsDir = WorkspaceFolder ()
    let viewDir = WorkspaceViewFolder ()

    let viewFile = viewName + ".view" |> GetFile viewDir
    let slnFile = viewName + ".sln" |> GetFile wsDir
    let repos = File.ReadAllLines (viewFile.FullName)

    let projects = SelectProjects antho.Projects repos
    let viewContent = GenerateSolutionContent projects

    File.WriteAllLines (slnFile.FullName, viewContent)
