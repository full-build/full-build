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
module Solution
open Anthology
open System.IO
open System.Xml.Linq
open StringHelpers
open MsBuildHelpers



let ProjectToProjectType (filename : string) =
    let file = FileInfo(filename)
    let ext2projType = Map [ (".csproj", "fae04ec0-301f-11d3-bf4b-00c04f79efbc")
                             (".fsproj", "f2a71f9b-5d33-465a-a702-920d77279786")
                             (".vbproj", "f184b08f-c81c-45f6-a57f-5abd9991f28f") ]
    let prjType = ext2projType.[file.Extension]
    prjType
    

let GenerateSolutionContent (projects : Project seq) =
    seq {
        yield ""
        yield "Microsoft Visual Studio Solution File, Format Version 12.00"
        yield "# Visual Studio 2013"

        for project in projects do
            yield sprintf @"Project(""{%s}"") = ""%s"", ""%s"", ""{%s}""" 
                  (ProjectToProjectType (project.RelativeProjectFile.toString))
                  (Path.GetFileNameWithoutExtension (project.RelativeProjectFile.toString))
                  (sprintf "%s/%s" (project.Repository.toString) project.RelativeProjectFile.toString)
                  (project.UniqueProjectId.toString)


            yield "\tProjectSection(ProjectDependencies) = postProject"
//            for dependency in project.ProjectReferences do
//                if projects |> Seq.exists (fun x -> x.ProjectGuid = dependency) then
//                    let dependencyName = StringifyGuid dependency.Value
//                    yield sprintf "\t\t%s = %s" dependencyName dependencyName
            yield "\tEndProjectSection"
            yield "EndProject"

        let repositories = projects |> Seq.map (fun x -> (x.Repository, GenerateGuidFromString (x.Repository.toString) |> ProjectUniqueId.from))
                                    |> Set
                                    |> Map

        for repository in repositories do
            let repo = repository.Key
            let guid = repository.Value

            yield sprintf @"Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = %A, %A, ""{%s}""" repo.toString repo.toString guid.toString
            yield "EndProject"

        yield "Global"
        yield "\tGlobalSection(SolutionConfigurationPlatforms) = preSolution"
        yield "\t\tDebug|Any CPU = Debug|Any CPU"
        yield "\t\tRelease|Any CPU = Release|Any CPU"
        yield "\tEndGlobalSection"
        yield "\tGlobalSection(ProjectConfigurationPlatforms) = postSolution"

        for project in projects do
            let guid = project.UniqueProjectId.toString
            yield sprintf "\t\t{%s}.Debug|Any CPU.ActiveCfg = Debug|Any CPU" guid
            yield sprintf "\t\t{%s}.Debug|Any CPU.Build.0 = Debug|Any CPU" guid
            yield sprintf "\t\t{%s}.Release|Any CPU.ActiveCfg = Release|Any CPU" guid
            yield sprintf "\t\t{%s}.Release|Any CPU.Build.0 = Release|Any CPU" guid

        yield "\tEndGlobalSection"

        yield "\tGlobalSection(NestedProjects) = preSolution"
        for project in projects do
            let guid = project.UniqueProjectId.toString
            yield sprintf "\t\t{%s} = {%s}" guid repositories.[project.Repository].toString
        yield "\tEndGlobalSection"

        yield "EndGlobal"
    }

let GenerateSolutionDefines (projects : Project seq) =
    XElement (NsMsBuild + "Project",
        XElement (NsMsBuild + "PropertyGroup",
            XElement(NsMsBuild + "FullBuild_Config", "Y"),
                projects |> Seq.map (fun x -> XElement (NsMsBuild + (ProjectPropertyName x), "Y") ) ) )
