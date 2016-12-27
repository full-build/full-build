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

module Generators.Solution
open System.IO
open System.Xml.Linq
open StringHelpers
open XmlHelpers
open Graph
open Collections
open MSBuildHelpers

let private projectToProjectType (filename : string) =
    let file = FileInfo(filename)
    let ext2projType = Map [ (".csproj", "fae04ec0-301f-11d3-bf4b-00c04f79efbc")
                             (".fsproj", "f2a71f9b-5d33-465a-a702-920d77279786")
                             (".vbproj", "f184b08f-c81c-45f6-a57f-5abd9991f28f") ]
    let prjType = ext2projType.[file.Extension]
    prjType


let GenerateSolutionContent (projects : Project set) =
    seq {
        yield "Microsoft Visual Studio Solution File, Format Version 12.00"
        yield "# Visual Studio 14"

        for project in projects do
            yield sprintf @"Project(""{%s}"") = ""%s"", ""%s"", ""{%s}"""
                  (projectToProjectType (project.ProjectFile))
                  (Path.GetFileNameWithoutExtension (project.ProjectFile))
                  (sprintf "%s/%s" (project.Repository.Name) project.ProjectFile)
                  (project.UniqueProjectId)

            yield "\tProjectSection(ProjectDependencies) = postProject"
            for reference in project.References do
                if projects |> Set.contains reference then
                    let dependencyName = sprintf "{%s}" reference.UniqueProjectId
                    yield sprintf "\t\t%s = %s" dependencyName dependencyName
            yield "\tEndProjectSection"
            yield "EndProject"

        let repositories = projects |> Set.map (fun x -> let guid = GenerateGuidFromString (x.Repository.Name)
                                                         (x.Repository, guid |> StringHelpers.toVSGuid))
                                    |> Map
        for repository in repositories do
            let repo = repository.Key.Name
            let guid = repository.Value

            yield sprintf @"Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = %A, %A, ""{%s}""" repo repo guid
            yield "EndProject"

        yield "Global"
        yield "\tGlobalSection(SolutionConfigurationPlatforms) = preSolution"
        yield "\t\tDebug|Any CPU = Debug|Any CPU"
        yield "\t\tRelease|Any CPU = Release|Any CPU"
        yield "\tEndGlobalSection"
        yield "\tGlobalSection(ProjectConfigurationPlatforms) = postSolution"

        for project in projects do
            let guid = project.UniqueProjectId
            yield sprintf "\t\t{%s}.Debug|Any CPU.ActiveCfg = Debug|Any CPU" guid
            yield sprintf "\t\t{%s}.Debug|Any CPU.Build.0 = Debug|Any CPU" guid
            yield sprintf "\t\t{%s}.Release|Any CPU.ActiveCfg = Release|Any CPU" guid
            yield sprintf "\t\t{%s}.Release|Any CPU.Build.0 = Release|Any CPU" guid

        yield "\tEndGlobalSection"

        yield "\tGlobalSection(NestedProjects) = preSolution"
        for project in projects do
            let guid = project.UniqueProjectId
            yield sprintf "\t\t{%s} = {%s}" guid repositories.[project.Repository]
        yield "\tEndGlobalSection"

        yield "EndGlobal"
    }

let GenerateSolutionDefines (projects : Project set) =
    XDocument (
        XElement(NsMsBuild + "Project",
            XAttribute(NsNone + "Condition", "'$(FullBuild_Config)' == ''"),
                XElement (NsMsBuild + "PropertyGroup",
                    XElement(NsMsBuild + "FullBuild_Config", "Y"),
                    projects |> Seq.map (fun x -> XElement (NsMsBuild + (MsBuildProjectPropertyName x), "Y") ) ) ) )
