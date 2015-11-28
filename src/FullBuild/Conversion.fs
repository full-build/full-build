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

module Conversion
open System.IO
open System.Xml.Linq
open System
open System.Linq
open System.Text
open IoHelpers
open StringHelpers
open MsBuildHelpers
open Env
open Anthology

let GenerateProjectTarget (project : Project) =
    let projectProperty = ProjectPropertyName project
    let srcCondition = sprintf "'$(%s)' != ''" projectProperty
    let binCondition = sprintf "'$(%s)' == ''" projectProperty
    let projectFile = sprintf "%s/%s/%s" MSBUILD_SOLUTION_DIR (project.Repository.toString) project.RelativeProjectFile.toString
    let output = (project.Output.toString)
    let ext = match project.OutputType with
              | OutputType.Dll -> "dll"
              | OutputType.Exe -> "exe"
    let includeFile = sprintf "%s/%s/%s.%s" MSBUILD_BIN_FOLDER output output ext
    
    // This is the import targets that will be Import'ed inside a proj file.
    // First we include full-build view configuration (this is done to avoid adding an extra import inside proj)
    // Then we end up either importing output assembly or project depending on view configuration
    XDocument (
        XElement(NsMsBuild + "Project", 
            XElement (NsMsBuild + "Import",
                XAttribute (NsNone + "Project", "$(SolutionDir)/.full-build/views/$(SolutionName).targets"),
                XAttribute (NsNone + "Condition", "'$(FullBuild_Config)' == ''")),
            XElement (NsMsBuild + "ItemGroup",
                XElement(NsMsBuild + "ProjectReference",
                    XAttribute (NsNone + "Include", projectFile),
                    XAttribute (NsNone + "Condition", srcCondition),
                    XElement (NsMsBuild + "Project", sprintf "{%s}" project.UniqueProjectId.toString),
                    XElement (NsMsBuild + "Name", project.Output.toString)),
                XElement (NsMsBuild + "Reference",
                    XAttribute (NsNone + "Include", includeFile),
                    XAttribute (NsNone + "Condition", binCondition),
                    XElement (NsMsBuild + "Private", "true")))))

let GenerateProjects (projects : Project seq) (xdocSaver : FileInfo -> XDocument -> Unit) =
    let prjDir = Env.GetFolder Env.Project
    for project in projects do
        let content = GenerateProjectTarget project
        let projectFile = prjDir |> GetFile (AddExt Targets (project.Output.toString))
        xdocSaver projectFile content

let ConvertProject (xproj : XDocument) (project : Project) =
    let filterProject (xel : XElement) =
        let attr = !> (xel.Attribute (NsNone + "Project")) : string
        attr.StartsWith(MSBUILD_PROJECT_FOLDER, StringComparison.CurrentCultureIgnoreCase)

    let filterPackage (xel : XElement) =
        let attr = !> (xel.Attribute (NsNone + "Project")) : string
        attr.StartsWith(MSBUILD_PACKAGE_FOLDER, StringComparison.CurrentCultureIgnoreCase)

    let filterNuget (xel : XElement) =
        let attr = !> (xel.Attribute (NsNone + "Project")) : string
        attr.StartsWith("$(SolutionDir)\.nuget\NuGet.targets", StringComparison.CurrentCultureIgnoreCase)

    let filterNugetTarget (xel : XElement) =
        let attr = !> (xel.Attribute (NsNone + "Name")) : string
        String.Equals(attr, "EnsureNuGetPackageBuildImports", StringComparison.CurrentCultureIgnoreCase)

    let filterNugetPackage (xel : XElement) =
        let attr = !> (xel.Attribute (NsNone + "Include")) : string
        String.Equals(attr, "packages.config", StringComparison.CurrentCultureIgnoreCase)

    let filterPaketReference (xel : XElement) =
        let attr = !> (xel.Attribute (NsNone + "Include")) : string
        attr.StartsWith("paket.references", StringComparison.CurrentCultureIgnoreCase)

    let filterPaketTarget (xel : XElement) =
        let attr = !> (xel.Attribute (NsNone + "Project")) : string
        attr.StartsWith("$(SolutionDir)\.paket\paket.targets", StringComparison.CurrentCultureIgnoreCase)

    let filterPaket (xel : XElement) =
        xel.Descendants(NsMsBuild + "Paket").Any ()

    let filterAssemblies (assFiles) (xel : XElement) =
        let inc = !> xel.Attribute(XNamespace.None + "Include") : string
        let assName = inc.Split([| ',' |], StringSplitOptions.RemoveEmptyEntries).[0]
        let assRef = AssemblyId.from (System.Reflection.AssemblyName(assName))
        let res = Set.contains assRef assFiles
        not res

    let hasNoChild (xel : XElement) =
        not <| xel.DescendantNodes().Any()

    let setOutputPath (xel : XElement) =
        let outputDir = sprintf "%s/%s/" MSBUILD_BIN_FOLDER project.Output.toString
        xel.Value <- outputDir

    // cleanup everything that will be modified
    let cproj = XDocument (xproj)

    // paket
    cproj.Descendants(NsMsBuild + "None").Where(filterPaketReference).Remove()
    cproj.Descendants(NsMsBuild + "Import").Where(filterPaketTarget).Remove()
    cproj.Descendants(NsMsBuild + "Choose").Where(filterPaket).Remove()

    // remove project references
    cproj.Descendants(NsMsBuild + "ProjectReference").Remove()
    
    // remove unknown assembly references
    cproj.Descendants(NsMsBuild + "Reference").Where(filterAssemblies project.AssemblyReferences).Remove()

    // remove full-build imports
    cproj.Descendants(NsMsBuild + "Import").Where(filterProject).Remove()
    cproj.Descendants(NsMsBuild + "Import").Where(filterPackage).Remove()
//    cproj.Descendants(NsMsBuild + "Import").Where(filterPublishImport).Remove()
//    cproj.Descendants(NsMsBuild + "Target").Where(filterPublishTarget).Remove()

    // remove nuget stuff
    cproj.Descendants(NsMsBuild + "Import").Where(filterNuget).Remove()
    cproj.Descendants(NsMsBuild + "Target").Where(filterNugetTarget).Remove()
    cproj.Descendants(NsMsBuild + "None").Where(filterNugetPackage).Remove();
    cproj.Descendants(NsMsBuild + "Content").Where(filterNugetPackage).Remove();

    // set OutputPath
    cproj.Descendants(NsMsBuild + "OutputPath") |> Seq.iter setOutputPath
    cproj.Descendants(NsMsBuild + "TargetFrameworkVersion") |> Seq.iter (fun x -> x.Value <- project.FxTarget.toString)

    // cleanup project
    cproj.Descendants(NsMsBuild + "BaseIntermediateOutputPath").Remove()
    cproj.Descendants(NsMsBuild + "SolutionDir").Remove()
    cproj.Descendants(NsMsBuild + "RestorePackages").Remove()
    cproj.Descendants(NsMsBuild + "NuGetPackageImportStamp").Remove()
    cproj.Descendants(NsMsBuild + "ItemGroup").Where(hasNoChild).Remove()

    // add project references
    for projectReference in project.ProjectReferences do
        let prjRef = projectReference.toString
        let importFile = sprintf "%s%s.targets" MSBUILD_PROJECT_FOLDER prjRef
        let import = XElement (NsMsBuild + "Import",
                        XAttribute (NsNone + "Project", importFile))
        cproj.Root.LastNode.AddAfterSelf (import)

    // add nuget references
    for packageReference in project.PackageReferences do
        let pkgId = packageReference.toString
        let importFile = sprintf "%s%s/package.targets" MSBUILD_PACKAGE_FOLDER pkgId
        let pkgProperty = PackagePropertyName pkgId
        let condition = sprintf "'$(%s)' == ''" pkgProperty
        let import = XElement (NsMsBuild + "Import",
                        XAttribute (NsNone + "Project", importFile),
                        XAttribute(NsNone + "Condition", condition))
        cproj.Root.LastNode.AddAfterSelf (import)
    cproj

let ConvertProjectContent (xproj : XDocument) (project : Project) (antho : Anthology) =
    let convxproj = ConvertProject xproj project
    convxproj

let ConvertProjects (antho : Anthology) xdocLoader xdocSaver =
    let wsDir = Env.GetFolder Env.Workspace
    for project in antho.Projects do
        let repoDir = wsDir |> GetSubDirectory (project.Repository.toString)
        if repoDir.Exists then
            let projFile = repoDir |> GetFile project.RelativeProjectFile.toString 
            let xproj = xdocLoader projFile
            let convxproj = ConvertProjectContent xproj project antho

            // only save if projs differ
            if xproj.ToString() <> convxproj.ToString() then
                xdocSaver projFile convxproj

let RemoveUselessStuff (antho : Anthology) =
    let wsDir = Env.GetFolder Env.Workspace
    for repo in antho.Repositories do
        let repoDir = wsDir |> GetSubDirectory (repo.Name.toString)
        if repoDir.Exists then
            repoDir.EnumerateFiles("*.sln", SearchOption.AllDirectories) |> Seq.iter (fun x -> x.Delete())
            repoDir.EnumerateFiles("packages.config", SearchOption.AllDirectories) |> Seq.iter (fun x -> x.Delete())
            repoDir.EnumerateFiles("paket.dependencies", SearchOption.AllDirectories) |> Seq.iter (fun x -> x.Delete())
            repoDir.EnumerateFiles("paket.lock", SearchOption.AllDirectories) |> Seq.iter (fun x -> x.Delete())
            repoDir.EnumerateFiles("paket.references", SearchOption.AllDirectories) |> Seq.iter (fun x -> x.Delete())
            repoDir.EnumerateDirectories("packages", SearchOption.AllDirectories) |> Seq.iter (fun x -> x.Delete(true))
            repoDir.EnumerateDirectories(".paket", SearchOption.AllDirectories) |> Seq.iter (fun x -> x.Delete(true))

