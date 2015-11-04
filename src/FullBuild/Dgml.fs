module Dgml
open System.Xml.Linq
open Anthology
open Collections
open MsBuildHelpers

let GenerateProjectNode (project : Project) =
    let isTest = project.RelativeProjectFile.toString.Contains(".Test.") || project.RelativeProjectFile.toString.Contains(".Tests.")
    let cat = if isTest then "TestProject"
              else "Project"

    let label = System.IO.Path.GetFileNameWithoutExtension(project.RelativeProjectFile.toString)
    let output = project.Output.toString
    let outputType = project.OutputType.toString

    XElement(NsDgml + "Node",
        XAttribute(NsNone + "Id", project.ProjectGuid.toString),
        XAttribute(NsNone + "Label", label),
        XAttribute(NsNone + "Category", cat),
        XAttribute(NsNone + "Fx", project.FxTarget.toString),
        XAttribute(NsNone + "Guid", project.ProjectGuid.toString),
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

let GraphNodes (antho : Anthology) (projects : Project set) =
    let allReferencedProjects = projects |> Set.map (fun x -> x.ProjectReferences)
                                         |> Set.unionMany
                                         |> Set.map (fun x -> antho.Projects |> Seq.find (fun y -> y.ProjectGuid = x))
    let importedProjects = Set.difference allReferencedProjects projects
    let allPackageReferences = projects |> Seq.map (fun x -> x.PackageReferences)
                                        |> Seq.concat
    let allAssemblies = projects |> Seq.map (fun x -> x.AssemblyReferences)
                                 |> Seq.concat

    let repos = allReferencedProjects |> Set.map (fun x -> x.Repository)

    seq {
        for repo in repos do
            yield XElement(NsDgml + "Node",
                XAttribute(NsNone + "Id", repo.toString),
                XAttribute(NsNone + "Label", repo.toString),
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
            yield GenerateLink project.Repository.toString (project.ProjectGuid.toString) "Contains"

        for project in importedProjects do
            yield GenerateLink project.Repository.toString (project.ProjectGuid.toString) "Contains"

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


let GraphContent (antho : Anthology) (allProjects : Project set) =
    let repos = allProjects |> Set.map (fun x -> x.ProjectReferences)
                               |> Set.unionMany
                               |> Set.map (fun x -> antho.Projects |> Seq.find (fun y -> y.ProjectGuid = x))
                               |> Set.map (fun x -> x.Repository)
    let xNodes = XElement(NsDgml + "Nodes", GraphNodes antho allProjects)
    let xLinks = XElement(NsDgml+"Links", GraphLinks antho allProjects)
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
