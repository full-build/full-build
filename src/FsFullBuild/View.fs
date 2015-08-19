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
open System.Xml.Linq
open MsBuildHelpers
open Configuration
open Collections

let Drop (viewName : string) =
    let vwDir = WorkspaceViewFolder ()
    let vwFile = vwDir |> GetFile (AddExt viewName View)
    File.Delete (vwFile.FullName)

    let vwDefineFile = vwDir |> GetFile (AddExt viewName Targets)
    File.Delete (vwDefineFile.FullName)

let List () =
    let vwDir = WorkspaceViewFolder ()
    vwDir.EnumerateFiles (AddExt "*" View) |> Seq.iter (fun x -> printfn "%s" (Path.GetFileNameWithoutExtension (x.Name)))

let Describe (viewName : string) =
    let vwDir = WorkspaceViewFolder ()
    let vwFile = vwDir |> GetFile (AddExt viewName View)
    File.ReadAllLines (vwFile.FullName) |> Seq.iter (fun x -> printfn "%s" x)


let GenerateSolutionContent (projects : Project seq) =
    seq {
        yield ""
        yield "Microsoft Visual Studio Solution File, Format Version 12.00"
        yield "# Visual Studio 2013"

        for project in projects do
            yield sprintf @"Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""%s"", ""%s"", ""%s""" 
                  (Path.GetFileNameWithoutExtension (project.RelativeProjectFile))
                  (sprintf "%s/%s" (project.Repository.Print()) project.RelativeProjectFile)
                  (StringifyGuid project.ProjectGuid)

            yield "\tProjectSection(ProjectDependencies) = postProject"
//            for dependency in project.ProjectReferences do
//                if projects |> Seq.exists (fun x -> x.ProjectGuid = dependency) then
//                    let dependencyName = StringifyGuid dependency
//                    yield sprintf "\t\t%s = %s" dependencyName dependencyName
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

let GenerateSolutionDefines (projects : Project seq) =
    XElement (NsMsBuild + "Project",
        XElement (NsMsBuild + "PropertyGroup",
            XElement(NsMsBuild + "FullBuild_Config", "Y"),
                projects |> Seq.map (fun x -> XElement (NsMsBuild + (ProjectPropertyName x), "Y") ) ) )


// find all referencing projects of a project
let private ReferencingProjects (projects : Project set) (current : ProjectRef) =
    projects |> Seq.filter (fun x -> x.ProjectReferences |> Set.contains current)

let rec private ComputePaths (findParents : ProjectRef -> Project seq) (goal : ProjectRef list) (path : ProjectRef list) (current : ProjectRef) =
    if Seq.contains current goal then current::path
    else
        let parents = findParents current |> Seq.map ProjectRef.Bind
        let paths = parents |> Seq.collect (ComputePaths findParents goal (current::path))
                            |> Seq.toList
        paths

let ComputeProjectSelectionClosure (allProjects : Project set) (filters : RepositoryRef seq) =
    let goal = allProjects |> Seq.filter (fun x -> Seq.contains x.Repository filters) 
                           |> Seq.map ProjectRef.Bind
                           |> Seq.toList

    let findParents = ReferencingProjects allProjects

    let transitiveClosure = goal |> Seq.map (ComputePaths findParents goal [])
                                 |> Seq.concat
                                 |> Seq.distinct
    transitiveClosure

let Generate (viewName : string) =
    let antho = LoadAnthology ()
    let wsDir = WorkspaceFolder ()
    let viewDir = WorkspaceViewFolder ()

    let viewFile = viewDir |> GetFile (AddExt viewName View)
    let slnFile = wsDir |> GetFile (AddExt viewName Solution)
    let repos = File.ReadAllLines (viewFile.FullName) |> Seq.map RepositoryRef.Bind

    let projectRefs = ComputeProjectSelectionClosure antho.Projects repos |> set
    let projects = antho.Projects |> Seq.filter (fun x -> projectRefs.Contains(ProjectRef.Bind(x)))

    let slnContent = GenerateSolutionContent projects
    File.WriteAllLines (slnFile.FullName, slnContent)

    let slnDefines = GenerateSolutionDefines projects
    let slnDefineFile = viewDir |> GetFile (AddExt viewName Targets)
    slnDefines.Save (slnDefineFile.FullName)


let GraphNodes (antho : Anthology) =
    seq {
        for project in antho.Projects do
            yield XElement(NsDgml + "Node",
                      XAttribute(NsNone + "Id", project.ProjectGuid),
                      XAttribute(NsNone + "Label", project.Output.Print()),
                      XAttribute(NsNone + "Category", "Project"))

        for package in antho.Packages do
            yield XElement(NsDgml + "Node",
                      XAttribute(NsNone + "Id", package.Id.Print()),
                      XAttribute(NsNone + "Label", package.Id.Print()),
                      XAttribute(NsNone + "Category", "Package"))

        let assemblies = antho.Projects |> Seq.map (fun x -> x.AssemblyReferences)
                                        |> Seq.concat
                                        |> set
        for assembly in assemblies do
             yield XElement(NsDgml + "Node",
                      XAttribute(NsNone + "Id", assembly.Print()),
                      XAttribute(NsNone + "Label", assembly.Print()),
                      XAttribute(NsNone + "Category", "Assembly"))
    }

let GraphLinks (antho : Anthology) =
    seq {
        for project in antho.Projects do
            let generateLink target category = 
                XElement(NsDgml + "Link",
                              XAttribute(NsNone + "Source", project.ProjectGuid),
                              XAttribute(NsNone + "Target", target),
                              XAttribute(NsNone + "Category", category))

            for projectRef in project.ProjectReferences do
                yield generateLink (projectRef.Print()) "ProjectRef"

            for package in project.PackageReferences do
                yield generateLink (package.Print()) "PackageRef"

            for assembly in project.AssemblyReferences do
                yield generateLink (assembly.Print()) "AssemblyRef"
    }

let GraphCategories () =
    let allCategories = [ ("Project", "Green")
                          ("Package", "Orange")
                          ("Assembly", "Red")       
                          ("ProjectRef", "Green")
                          ("PackageRef", "Orange")
                          ("AssemblyRef", "Red") ]

    let generateCategory (cat) =
        let (key, value) = cat
        XElement(NsDgml + "Category", 
            XAttribute(NsNone + "Id", key), 
            XAttribute(NsNone + "Background", value))

    allCategories |> Seq.map generateCategory

let GraphContent (antho : Anthology) =
    let xNodes = XElement(NsDgml + "Nodes", GraphNodes antho)
    let xLinks = XElement(NsDgml+"Links", GraphLinks antho)
    let xCategories = XElement(NsDgml + "Categories", GraphCategories ())
    let xLayout = XAttribute(NsNone + "Layout", "ForceDirected")
    XDocument(
        XElement(NsDgml + "DirectedGraph", xLayout, xNodes, xLinks, xCategories))

let Graph (viewName : string) =
    let antho = Configuration.LoadAnthology ()
    let graph = GraphContent antho

    let wsDir = Env.WorkspaceFolder ()
    let graphFile = wsDir |> GetSubDirectory (AddExt viewName Dgml)
    graph.Save graphFile.FullName

let Create (viewName : string) (filters : string list) =
    let repos = filters |> Repo.FilterRepos 
                        |> Seq.map RepositoryRef.Bind
                        |> Seq.map (fun x -> x.Print())
    let vwDir = WorkspaceViewFolder ()
    let vwFile = vwDir |> GetFile (AddExt viewName View)
    File.WriteAllLines (vwFile.FullName, repos)

    Generate viewName
