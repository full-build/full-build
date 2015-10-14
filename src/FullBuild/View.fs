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

let Drop (viewName : ViewId) =
    let vwDir = GetFolder Env.View
    let vwFile = GetFile (AddExt View viewName.toString) vwDir
    File.Delete (vwFile.FullName)

    let vwDefineFile = GetFile (AddExt Targets viewName.toString) vwDir
    File.Delete (vwDefineFile.FullName)

let List () =
    let vwDir = GetFolder Env.View
    vwDir.EnumerateFiles (AddExt  View "*") |> Seq.iter (fun x -> printfn "%s" (Path.GetFileNameWithoutExtension (x.Name)))

let Describe (viewName : ViewId) =
    let vwDir = GetFolder Env.View
    let vwFile = vwDir |> GetFile (AddExt View viewName.toString)
    File.ReadAllLines (vwFile.FullName) |> Seq.iter (fun x -> printfn "%s" x)


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
                  (project.ProjectGuid.toString)

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
            let guid = project.ProjectGuid.toString
            yield sprintf "\t\t{%s}.Debug|Any CPU.ActiveCfg = Debug|Any CPU" guid
            yield sprintf "\t\t{%s}.Debug|Any CPU.Build.0 = Debug|Any CPU" guid
            yield sprintf "\t\t{%s}.Release|Any CPU.ActiveCfg = Release|Any CPU" guid
            yield sprintf "\t\t{%s}.Release|Any CPU.Build.0 = Release|Any CPU" guid

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

let FindViewProjects (viewName : ViewId) =
    let antho = LoadAnthology ()
    let viewDir = GetFolder Env.View

    let viewFile = viewDir |> GetFile (AddExt View viewName.toString)
    let repos = File.ReadAllLines (viewFile.FullName) |> Seq.map (fun x -> RepositoryId.from x)
    let projectRefs = ComputeProjectSelectionClosure antho.Projects repos |> Set
    let projects = antho.Projects |> Set.filter (fun x -> projectRefs |> Set.contains x.ProjectGuid)
    projects

let Generate (viewName : ViewId) =
    let projects = FindViewProjects viewName

    let wsDir = GetFolder Env.Workspace
    let slnFile = wsDir |> GetFile (AddExt Solution viewName.toString)
    let slnContent = GenerateSolutionContent projects
    File.WriteAllLines (slnFile.FullName, slnContent)

    let slnDefines = GenerateSolutionDefines projects
    let viewDir = GetFolder Env.View
    let slnDefineFile = viewDir |> GetFile (AddExt Targets viewName.toString)
    slnDefines.Save (slnDefineFile.FullName)


let GenerateProjectNode (project : Project) =
    let isTest = project.RelativeProjectFile.toString.Contains(".Test.") || project.RelativeProjectFile.toString.Contains(".Tests.")
    let cat = if isTest then "TestProject"
              else "Project"

    XElement(NsDgml + "Node",
        XAttribute(NsNone + "Id", project.ProjectGuid.toString),
        XAttribute(NsNone + "Label", project.Output.toString),
        XAttribute(NsNone + "Category", cat),
        XAttribute(NsNone + "Fx", project.FxTarget.toString),
        XAttribute(NsNone + "Guid", project.ProjectGuid.toString),
        XAttribute(NsNone + "IsTest", isTest))

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
            yield GenerateProjectNode project

        for project in importedProjects do
            yield GenerateNode (project.ProjectGuid.toString) (project.Output.toString) "ProjectImport"

        for package in allPackageReferences do
            yield GenerateNode (package.toString) (package.toString) "Package"

        for assembly in allAssemblies do
            yield GenerateNode (assembly.toString) (assembly.toString) "Assembly"
    }

let GraphLinks (antho : Anthology) (projects : Project set) =
    let allReferencedProjects = projects |> Set.map (fun x -> x.ProjectReferences)
                                         |> Set.unionMany
                                         |> Set.map (fun x -> antho.Projects |> Seq.find (fun y -> y.ProjectGuid = x))
    let importedProjects = Set.difference allReferencedProjects projects

    seq {
        for project in projects do
            for projectRef in project.ProjectReferences do
                yield GenerateLink (project.ProjectGuid.toString) (projectRef.toString) "ProjectRef"

        for project in projects do
            for package in project.PackageReferences do
                yield GenerateLink (project.ProjectGuid.toString) (package.toString) "PackageRef"

        for project in projects do
            for assembly in project.AssemblyReferences do
                yield GenerateLink (project.ProjectGuid.toString) (assembly.toString) "AssemblyRef"

        for project in projects do
            yield GenerateLink "Projects" (project.ProjectGuid.toString) "Contains"

        for project in importedProjects do
            yield GenerateLink "Projects" (project.ProjectGuid.toString) "Contains"

        for project in projects do
            for package in project.PackageReferences do
                yield GenerateLink "Packages" (package.toString) "Contains"

        for project in projects do
            for assembly in project.AssemblyReferences do
                yield GenerateLink "Assemblies" (assembly.toString) "Contains"

    }

let GraphCategories () =
    let allCategories = [ ("Project", "Green")
                          ("TestProject", "Purple")
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

let GraphProperties () =
    let allProperties = [ ("Fx", "Target Framework Version", "System.String")
                          ("Guid", "Project Guid", "System.Guid") 
                          ("IsTest", "Test Project", "System.Boolean")]

    let generateProperty (prop) =
        let (id, label, dataType) = prop
        XElement(NsDgml + "Property", 
            XAttribute(NsNone + "Id", id), 
            XAttribute(NsNone + "Label", label),
            XAttribute(NsNone + "DataType", dataType))

    allProperties |> Seq.map generateProperty

let GraphStyles () =
    XElement(NsDgml + "Styles",
        XElement(NsDgml + "Style",
            XAttribute(NsNone + "TargetType", "Node"), XAttribute(NsNone + "GroupLabel", "Test Project"), XAttribute(NsNone + "ValueLabel", "True"),
            XElement(NsDgml + "Condition", XAttribute(NsNone + "Expression", "IsTest = 'True'")),
            XElement(NsDgml + "Setter", XAttribute(NsNone + "Property", "Icon"), XAttribute(NsNone + "Value", "CodeSchema_Event"))),
        XElement(NsDgml + "Style",
            XAttribute(NsNone + "TargetType", "Node"), XAttribute(NsNone + "GroupLabel", "Test Project"), XAttribute(NsNone + "ValueLabel", "False"),
            XElement(NsDgml + "Condition", XAttribute(NsNone + "Expression", "IsTest = 'False'")),
            XElement(NsDgml + "Setter", XAttribute(NsNone + "Property", "Icon"), XAttribute(NsNone + "Value", "CodeSchema_Method"))))


let GraphContent (antho : Anthology) (viewName : ViewId) =
    let projects = FindViewProjects viewName |> Set
    let xNodes = XElement(NsDgml + "Nodes", GraphNodes antho projects)
    let xLinks = XElement(NsDgml+"Links", GraphLinks antho projects)
    let xCategories = XElement(NsDgml + "Categories", GraphCategories ())
    let xProperties = XElement(NsDgml + "Properties", GraphProperties ())
    let xStyles = GraphStyles ()
    let xGraphDir = XAttribute(NsNone + "GraphDirection", "LeftToRight")
    let xLayout = XAttribute(NsNone + "Layout", "Sugiyama")
    XDocument(
        XElement(NsDgml + "DirectedGraph", xLayout, xGraphDir, xNodes, xLinks, xCategories, xProperties, xStyles))

let Graph (viewName : ViewId) =
    let antho = Configuration.LoadAnthology ()
    let graph = GraphContent antho viewName

    let wsDir = Env.GetFolder Env.Workspace
    let graphFile = wsDir |> GetSubDirectory (AddExt Dgml viewName.toString)
    graph.Save graphFile.FullName

let Create (viewName : ViewId) (filters : RepositoryId set) =
    if filters.Count = 0 then
        failwith "Expecting at least one filter"

    let repos = filters |> Repo.FilterRepos 
                        |> Seq.map (fun x -> x.Name.toString)
    let vwDir = Env.GetFolder Env.View
    let vwFile = vwDir |> GetFile (AddExt View viewName.toString)
    File.WriteAllLines (vwFile.FullName, repos)

    Generate viewName

let Build (viewName : ViewId) =
    let wsDir = Env.GetFolder Env.Workspace
    let viewFile = AddExt Solution viewName.toString
    let args = sprintf "/p:Configuration=Release %A" viewFile

    Exec.Exec "msbuild" args wsDir
