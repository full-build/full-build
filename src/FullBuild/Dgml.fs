﻿//   Copyright 2014-2016 Pierre Chalamet
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

module Dgml
open System.Xml.Linq
open Anthology
open Collections
open MsBuildHelpers

let GenerateProjectNode (project : Project) =
    let isTest = project.HasTests
    let cat = if isTest then "TestProject"
              else "Project"

    let label = System.IO.Path.GetFileNameWithoutExtension(project.RelativeProjectFile.toString)
    let output = project.Output.toString
    let outputType = project.OutputType.toString

    XElement(NsDgml + "Node",
        XAttribute(NsNone + "Id", project.UniqueProjectId.toString),
        XAttribute(NsNone + "Label", label),
        XAttribute(NsNone + "Category", cat),
        XAttribute(NsNone + "Fx", project.FxTarget.toString),
        XAttribute(NsNone + "Guid", project.UniqueProjectId.toString),
        XAttribute(NsNone + "IsTest", isTest),
        XAttribute(NsNone + "Output", output),
        XAttribute(NsNone + "OutputType", outputType))

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

let GraphNodes (projects : Project set) (allProjects : Project set) (packages : PackageId set) (assemblies : AssemblyId set) (repos : RepositoryId set) =
    let importedProjects = Set.difference allProjects projects

    seq {
        if packages.Count > 0 then
            yield XElement(NsDgml + "Node",
                XAttribute(NsNone + "Id", "Packages"),
                XAttribute(NsNone + "Label", "Packages"),
                XAttribute(NsNone + "Group", "Expanded"))

        if assemblies.Count > 0 then
            yield XElement(NsDgml + "Node",
                XAttribute(NsNone + "Id", "Assemblies"),
                XAttribute(NsNone + "Label", "Assemblies"),
                XAttribute(NsNone + "Group", "Expanded"))

        for project in projects do
            yield GenerateProjectNode project

        for project in importedProjects do
            yield GenerateNode (project.UniqueProjectId.toString) (project.Output.toString) "ProjectImport"

        for repo in repos do
            yield XElement(NsDgml + "Node",
                XAttribute(NsNone + "Id", repo.toString),
                XAttribute(NsNone + "Label", repo.toString),
                XAttribute(NsNone + "Group", "Expanded"))

        for package in packages do
            yield GenerateNode (package.toString) (package.toString) "Package"

        for assembly in assemblies do
            yield GenerateNode (assembly.toString) (assembly.toString) "Assembly"
    }

let GraphLinks (projects : Project set) (allProjects : Project set) =
    let importedProjects = Set.difference allProjects projects

    seq {
        for project in projects do
            yield GenerateLink (project.Repository.toString) (project.UniqueProjectId.toString) "Contains"

        for project in importedProjects do
            yield GenerateLink (project.Repository.toString) (project.UniqueProjectId.toString) "Contains"

        for project in projects do
            for projectRef in project.ProjectReferences do
                let target = allProjects |> Seq.find (fun x -> x.ProjectId = projectRef)
                yield GenerateLink (project.UniqueProjectId.toString) (target.UniqueProjectId.toString) "ProjectRef"

        for project in projects do
            for package in project.PackageReferences do
                yield GenerateLink (project.UniqueProjectId.toString) (package.toString) "PackageRef"

        for project in projects do
            for assembly in project.AssemblyReferences do
                yield GenerateLink (project.UniqueProjectId.toString) (assembly.toString) "AssemblyRef"

        for project in projects do
            for package in project.PackageReferences do
                yield GenerateLink "Packages" (package.toString) "Contains"

        for project in projects do
            for assembly in project.AssemblyReferences do
                yield GenerateLink "Assemblies" (assembly.toString) "Contains"
    }

let GraphCategories (repos : RepositoryId set) =
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

    seq {
        yield! (allCategories |> Seq.map generateCategory)

        for repo in repos do
            yield XElement(NsDgml + "Category", 
                XAttribute(NsNone + "Id", repo.toString), 
                XAttribute(NsNone + "Label", repo.toString), 
                XAttribute(NsNone + "IsContainment", "True"),
                XAttribute(NsNone + "CanBeDataDriven", "False"),
                XAttribute(NsNone + "CanLinkedNodesBeDataDriven", "True"),
                XAttribute(NsNone + "IncomingActionLabel", "Contained By"),
                XAttribute(NsNone + "OutgoingActionLabel", "Contains"))
    }

let GraphProperties () =
    let allProperties = [ ("Fx", "Target Framework Version", "System.String")
                          ("Guid", "Project Guid", "System.Guid") 
                          ("IsTest", "Test Project", "System.Boolean")
                          ("Output", "Project Output", "System.String") 
                          ("OutputType", "Project Output Type", "System.String") ]

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


let GraphContent (antho : Anthology) (projects : Project set) (all : bool) =
    let nonExeProjects = if all then projects
                         else projects |> Set.filter (fun x -> x.ProjectId.toString.IndexOf(".test", System.StringComparison.InvariantCultureIgnoreCase) = -1)

    let allProjects = nonExeProjects |> Set.map (fun x -> x.ProjectReferences)
                                     |> Set.unionMany
                                     |> Set.union (nonExeProjects |> Set.map(fun x -> x.ProjectId))
                                     |> Set.map (fun x -> antho.Projects |> Seq.find (fun y -> y.ProjectId = x))
    let repos = allProjects |> Set.map (fun x -> x.Repository)
    let packages = nonExeProjects |> Set.map (fun x -> x.PackageReferences)
                                  |> Set.unionMany
    let assemblies = nonExeProjects |> Set.map (fun x -> x.AssemblyReferences)
                                    |> Set.unionMany
    let xNodes = XElement(NsDgml + "Nodes", GraphNodes nonExeProjects allProjects packages assemblies repos)
    let xLinks = XElement(NsDgml+"Links", GraphLinks nonExeProjects allProjects)
    let xCategories = XElement(NsDgml + "Categories", GraphCategories repos)
    let xProperties = XElement(NsDgml + "Properties", GraphProperties ())
    let xStyles = GraphStyles ()
    let xGraphDir = XAttribute(NsNone + "GraphDirection", "TopToBottom")
    let xLayout = XAttribute(NsNone + "Layout", "Sugiyama")
    XDocument(
        XElement(NsDgml + "DirectedGraph", 
            xLayout, 
            xGraphDir, 
            xNodes, 
            xLinks, 
            xCategories, 
            xProperties, 
            xStyles))
