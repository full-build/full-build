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

module Generators.Dgml
open System.Xml.Linq
open Collections
open XmlHelpers
open Graph

let private generateProjectNode (project : Project) =
    let isTest = project.HasTests
    let cat = isTest ? ( "TestProject", "Project")

    let label = System.IO.Path.GetFileNameWithoutExtension(project.ProjectFile)
    let output = project.Output.Name
    let outputType = project.OutputType.ToString()

    let fxVersion = match project.FxVersion with
                    | Some x -> XAttribute(NsNone + "FxVersion", x)
                    | None -> null

    let fxProfile = match project.FxProfile with
                    | Some x -> XAttribute(NsNone + "FxProfile", x)
                    | None -> null

    let fxId = match project.FxIdentifier with
               | Some x -> XAttribute(NsNone + "FxIdentifier", x)
               | None -> null

    XElement(NsDgml + "Node",
        XAttribute(NsNone + "Id", project.UniqueProjectId),
        XAttribute(NsNone + "Label", label),
        XAttribute(NsNone + "Category", cat),
        fxVersion, fxProfile, fxId,
        XAttribute(NsNone + "Guid", project.UniqueProjectId),
        XAttribute(NsNone + "IsTest", project.HasTests),
        XAttribute(NsNone + "Output", output),
        XAttribute(NsNone + "OutputType", outputType))

let private generateNode (source : string) (label : string) (category : string) =
    XElement(NsDgml + "Node",
        XAttribute(NsNone + "Id", source),
        XAttribute(NsNone + "Label", label),
        XAttribute(NsNone + "Category", category))

let private generateLink (source : string) (target : string) (category : string) =
    XElement(NsDgml + "Link",
        XAttribute(NsNone + "Source", source),
        XAttribute(NsNone + "Target", target),
        XAttribute(NsNone + "Category", category))

let private graphNodes (projects : Project set) (allProjects : Project set) (packages : Package set) (assemblies : Assembly set) (repos : Repository set) =
    let importedProjects = allProjects - projects

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
            yield generateProjectNode project

        for project in importedProjects do
            yield generateNode (project.UniqueProjectId) (project.Output.Name) "ProjectImport"

        for repo in repos do
            yield XElement(NsDgml + "Node",
                XAttribute(NsNone + "Id", repo.Name),
                XAttribute(NsNone + "Label", repo.Name),
                XAttribute(NsNone + "Group", "Expanded"))

        for package in packages do
            yield generateNode (package.Name) (package.Name) "Package"

        for assembly in assemblies do
            yield generateNode (assembly.Name) (assembly.Name) "Assembly"
    }

let private graphLinks (projects : Project set) (allProjects : Project set) =
    let importedProjects = allProjects - projects

    seq {
        for project in projects do
            yield generateLink (project.Repository.Name) (project.UniqueProjectId) "Contains"

        for project in importedProjects do
            yield generateLink (project.Repository.Name) (project.UniqueProjectId) "Contains"

        for project in projects do
            for reference in project.References do
                yield generateLink (project.UniqueProjectId) (reference.UniqueProjectId) "ProjectRef"

        for project in projects do
            for package in project.PackageReferences do
                yield generateLink (project.UniqueProjectId) (package.Name) "PackageRef"

        for project in projects do
            for assembly in project.AssemblyReferences do
                yield generateLink (project.UniqueProjectId) (assembly.Name) "AssemblyRef"

        for project in projects do
            for package in project.PackageReferences do
                yield generateLink "Packages" (package.Name) "Contains"

        for project in projects do
            for assembly in project.AssemblyReferences do
                yield generateLink "Assemblies" (assembly.Name) "Contains"
    }

let private graphCategories (repos : Repository set) =
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
        yield! (allCategories |> List.map generateCategory)

        for repo in repos do
            yield XElement(NsDgml + "Category",
                XAttribute(NsNone + "Id", repo.Name),
                XAttribute(NsNone + "Label", repo.Name),
                XAttribute(NsNone + "IsContainment", "True"),
                XAttribute(NsNone + "CanBeDataDriven", "False"),
                XAttribute(NsNone + "CanLinkedNodesBeDataDriven", "True"),
                XAttribute(NsNone + "IncomingActionLabel", "Contained By"),
                XAttribute(NsNone + "OutgoingActionLabel", "Contains"))
    }

let private graphProperties () =
    let allProperties = [ ("FxVersion", "Target Framework Version", "System.String")
                          ("FxProfile", "Target Framework Profile", "System.String")
                          ("FxIdentifier", "Target Framework Identifier", "System.String")
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

    allProperties |> List.map generateProperty

let private graphStyles () =
    XElement(NsDgml + "Styles",
        XElement(NsDgml + "Style",
            XAttribute(NsNone + "TargetType", "Node"), XAttribute(NsNone + "GroupLabel", "Test Project"), XAttribute(NsNone + "ValueLabel", "True"),
            XElement(NsDgml + "Condition", XAttribute(NsNone + "Expression", "IsTest = 'True'")),
            XElement(NsDgml + "Setter", XAttribute(NsNone + "Property", "Icon"), XAttribute(NsNone + "Value", "CodeSchema_Event"))),
        XElement(NsDgml + "Style",
            XAttribute(NsNone + "TargetType", "Node"), XAttribute(NsNone + "GroupLabel", "Test Project"), XAttribute(NsNone + "ValueLabel", "False"),
            XElement(NsDgml + "Condition", XAttribute(NsNone + "Expression", "IsTest = 'False'")),
            XElement(NsDgml + "Setter", XAttribute(NsNone + "Property", "Icon"), XAttribute(NsNone + "Value", "CodeSchema_Method"))))


let GraphContent (projects : Project set) (all : bool) =
    let srcProjects = if all then projects
                      else projects |> Set.filter (fun x -> not x.HasTests)
    let allProjects = srcProjects |> Set.map (fun x -> x.References) |> Set.unionMany
    let repos = allProjects |> Set.map (fun x -> x.Repository)
    let packages = srcProjects |> Set.map (fun x -> x.PackageReferences)
                               |> Set.unionMany
    let assemblies = srcProjects |> Seq.map (fun x -> x.AssemblyReferences)
                                 |> Set.unionMany
    let xNodes = XElement(NsDgml + "Nodes", graphNodes srcProjects allProjects packages assemblies repos)
    let xLinks = XElement(NsDgml+"Links", graphLinks srcProjects allProjects)
    let xCategories = XElement(NsDgml + "Categories", graphCategories repos)
    let xProperties = XElement(NsDgml + "Properties", graphProperties ())
    let xStyles = graphStyles ()
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
