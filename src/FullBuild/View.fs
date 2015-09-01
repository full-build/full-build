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
            yield sprintf @"Project(""{%s}"") = ""%s"", ""%s"", ""%s""" 
                  (project.ProjectType.Value.ToString("D"))
                  (Path.GetFileNameWithoutExtension (project.RelativeProjectFile.Value))
                  (sprintf "%s/%s" (project.Repository.Value) project.RelativeProjectFile.Value)
                  (StringifyGuid project.ProjectGuid.Value)

            yield "\tProjectSection(ProjectDependencies) = postProject"
//            for dependency in project.ProjectReferences do
//                if projects |> Seq.exists (fun x -> x.ProjectGuid = dependency) then
//                    let dependencyName = StringifyGuid dependency.Value
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
            let guid = StringifyGuid project.ProjectGuid.Value
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
let private ReferencingProjects (projects : Project set) (current : ProjectId) =
    projects |> Seq.filter (fun x -> x.ProjectReferences |> Set.contains current)

let rec private ComputePaths (findParents : ProjectId -> Project seq) (goal : ProjectId list) (path : ProjectId list) (current : ProjectId) =
    if Seq.contains current goal then current::path
    else
        let parents = findParents current |> Seq.map (fun x -> x.ProjectGuid)
        let paths = parents |> Seq.collect (ComputePaths findParents goal (current::path))
                            |> Seq.toList
        paths

let ComputeProjectSelectionClosure (allProjects : Project set) (filters : RepositoryId seq) =
    let goal = allProjects |> Seq.filter (fun x -> Seq.contains x.Repository filters) 
                           |> Seq.map (fun x -> x.ProjectGuid)
                           |> Seq.toList

    let findParents = ReferencingProjects allProjects

    let transitiveClosure = goal |> Seq.map (ComputePaths findParents goal [])
                                 |> Seq.concat
                                 |> Seq.distinct
    transitiveClosure

let FindViewProjects (viewName : string) =
    let antho = LoadAnthology ()
    let viewDir = WorkspaceViewFolder ()

    let viewFile = viewDir |> GetFile (AddExt viewName View)
    let repos = File.ReadAllLines (viewFile.FullName) |> Seq.map (fun x -> RepositoryId.Bind x)
    let projectRefs = ComputeProjectSelectionClosure antho.Projects repos |> Set
    let projects = antho.Projects |> Set.filter (fun x -> projectRefs |> Set.contains x.ProjectGuid)
    projects

let Generate (viewName : string) =
    let projects = FindViewProjects viewName

    let wsDir = WorkspaceFolder ()
    let slnFile = wsDir |> GetFile (AddExt viewName Solution)
    let slnContent = GenerateSolutionContent projects
    File.WriteAllLines (slnFile.FullName, slnContent)

    let slnDefines = GenerateSolutionDefines projects
    let viewDir = WorkspaceViewFolder ()
    let slnDefineFile = viewDir |> GetFile (AddExt viewName Targets)
    slnDefines.Save (slnDefineFile.FullName)


let GenerateNode (source : string) (label : string) (category : string) =
    XElement(NsDgml + "Node",
        XAttribute(NsNone + "Id", source),
        XAttribute(NsNone + "Label", label),
        XAttribute(NsNone + "Category", category))

let GenerateLink (source : string) (target : string) (category : string) =
    XElement(NsDgml + "Link",
        XAttribute(NsNone + "Source", source),
        XAttribute(NsNone + "Target", target),
        XAttribute(NsNone + "Category", category))

let GraphNodes (antho : Anthology) (projects : Project set) =
    let allReferencedProjects = projects |> Set.map (fun x -> x.ProjectReferences)
                                         |> Set.unionMany
                                         |> Set.map (fun x -> antho.Projects |> Seq.find (fun y -> y.ProjectGuid = x))
    let importedProjects = Set.difference allReferencedProjects projects
    let allPackageReferences = projects |> Seq.map (fun x -> x.PackageReferences)
                                        |> Seq.concat
    let allAssemblies = projects |> Seq.map (fun x -> x.AssemblyReferences)
                                 |> Seq.concat
    seq {
        yield XElement(NsDgml + "Node",
                XAttribute(NsNone + "Id", "Projects"),
                XAttribute(NsNone + "Label", "Projects"),
                XAttribute(NsNone + "Group", "Expanded"))

        yield XElement(NsDgml + "Node",
                XAttribute(NsNone + "Id", "Packages"),
                XAttribute(NsNone + "Label", "Packages"),
                XAttribute(NsNone + "Group", "Expanded"))

        yield XElement(NsDgml + "Node",
                XAttribute(NsNone + "Id", "Assemblies"),
                XAttribute(NsNone + "Label", "Assemblies"),
                XAttribute(NsNone + "Group", "Expanded"))

        for project in projects do
            yield GenerateNode (project.ProjectGuid.Value.ToString("D")) (project.Output.Value) "Project"

        for project in importedProjects do
            yield GenerateNode (project.ProjectGuid.Value.ToString("D")) (project.Output.Value) "ProjectImport"

        for package in allPackageReferences do
            yield GenerateNode (package.Value) (package.Value) "Package"

        for assembly in allAssemblies do
            yield GenerateNode (assembly.Value) (assembly.Value) "Assembly"
    }

let GraphLinks (antho : Anthology) (projects : Project set) =
    let allReferencedProjects = projects |> Set.map (fun x -> x.ProjectReferences)
                                         |> Set.unionMany
                                         |> Set.map (fun x -> antho.Projects |> Seq.find (fun y -> y.ProjectGuid = x))
    let importedProjects = Set.difference allReferencedProjects projects

    seq {
        for project in projects do
            for projectRef in project.ProjectReferences do
                yield GenerateLink (project.ProjectGuid.Value.ToString("D")) (projectRef.Value.ToString("D")) "ProjectRef"

        for project in projects do
            for package in project.PackageReferences do
                yield GenerateLink (project.ProjectGuid.Value.ToString("D")) (package.Value) "PackageRef"

        for project in projects do
            for assembly in project.AssemblyReferences do
                yield GenerateLink (project.ProjectGuid.Value.ToString("D")) (assembly.Value) "AssemblyRef"

        for project in projects do
                yield GenerateLink "Projects" (project.ProjectGuid.Value.ToString("D")) "Contains"

        for project in importedProjects do
                yield GenerateLink "Projects" (project.ProjectGuid.Value.ToString("D")) "Contains"

        for project in projects do
            for package in project.PackageReferences do
                yield GenerateLink "Packages" (package.Value) "Contains"

        for project in projects do
            for assembly in project.AssemblyReferences do
                yield GenerateLink "Assemblies" (assembly.Value) "Contains"

    }

let GraphCategories () =
    let allCategories = [ ("Project", "Green")
                          ("ProjectImport", "Navy")
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

    let res = (allCategories |> Seq.map generateCategory)
    seq {
        yield! (allCategories |> Seq.map generateCategory)
        yield XElement(NsDgml + "Category", 
                XAttribute(NsNone + "Id", "Contains"), 
                XAttribute(NsNone + "Label", "Contains"), 
                XAttribute(NsNone + "IsContainment", "True"),
                XAttribute(NsNone + "CanBeDataDriven", "False"),
                XAttribute(NsNone + "CanLinkedNodesBeDataDriven", "True"),
                XAttribute(NsNone + "IncomingActionLabel", "Contained By"),
                XAttribute(NsNone + "OutgoingActionLabel", "Contains"))
    }

let GraphContent (antho : Anthology) (viewName : string) =
    let projects = FindViewProjects viewName |> Set
    let xNodes = XElement(NsDgml + "Nodes", GraphNodes antho projects)
    let xLinks = XElement(NsDgml+"Links", GraphLinks antho projects)
    let xCategories = XElement(NsDgml + "Categories", GraphCategories ())
    let xGraphDir = XAttribute(NsNone + "GraphDirection", "LeftToRight")
    let xLayout = XAttribute(NsNone + "Layout", "Sugiyama")
    XDocument(
        XElement(NsDgml + "DirectedGraph", xLayout, xGraphDir, xNodes, xLinks, xCategories))

let Graph (viewName : string) =
    let antho = Configuration.LoadAnthology ()
    let graph = GraphContent antho viewName

    let wsDir = Env.WorkspaceFolder ()
    let graphFile = wsDir |> GetSubDirectory (AddExt viewName Dgml)
    graph.Save graphFile.FullName

let Create (viewName : string) (filters : RepositoryId set) =
    let repos = filters |> Repo.FilterRepos 
                        |> Seq.map (fun x -> x.Name.Value)
    let vwDir = WorkspaceViewFolder ()
    let vwFile = vwDir |> GetFile (AddExt viewName View)
    File.WriteAllLines (vwFile.FullName, repos)

    Generate viewName
