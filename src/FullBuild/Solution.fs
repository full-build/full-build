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
            for dependency in project.ProjectReferences do
                let depProject = projects |> Seq.tryFind (fun x -> x.ProjectId = dependency)
                match depProject with
                | Some x -> let dependencyName = sprintf "{%s}" x.UniqueProjectId.toString
                            yield sprintf "\t\t%s = %s" dependencyName dependencyName
                | None -> ()
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
