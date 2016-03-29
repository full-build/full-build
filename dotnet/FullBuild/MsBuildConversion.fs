//   Copyright 2014-2016 Pierre Chalamet
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

module MsBuildConversion
open System.IO
open System.Xml.Linq
open System
open System.Linq
open IoHelpers
open MsBuildHelpers
open Env
open Anthology
open Collections


//let importCopyFrom (project : Project) =
//    let prjProperty = ProjectPropertyName project.ProjectId
//    let condition = sprintf "'$(%s)' == ''" prjProperty
//    let prjFolder = Path.GetDirectoryName (project.RelativeProjectFile.toString)
//    let path = sprintf "$(SolutionDir)/%s/%s/bin/" (project.Repository.toString) (prjFolder) 
//    let ext = match project.OutputType with
//              | OutputType.Dll -> ".dll"
//              | OutputType.Exe -> ".exe"
//
//    let inc = sprintf "%s*.dll;%s*.exe;%s*.pdb" path path path
//    let excl = sprintf "%s%s%s" path project.Output.toString ext
//
//    XElement(NsMsBuild + "ItemGroup",
//        XElement(NsMsBuild + "FBCopyFiles", 
//            XAttribute(NsNone + "Include", inc),
//            XAttribute(NsNone + "Exclude", excl)),
//        XAttribute(NsNone + "Condition", condition))

let GenerateProjectTarget (project : Project) =
    let projectProperty = ProjectPropertyName project.ProjectId
    let srcCondition = sprintf "'$(%s)' != ''" projectProperty
    let binCondition = sprintf "'$(%s)' == ''" projectProperty
    let projectFile = sprintf "%s/%s/%s" MSBUILD_SOLUTION_DIR (project.Repository.toString) project.RelativeProjectFile.toString
    let output = (project.Output.toString)
    let ext = match project.OutputType with
              | OutputType.Dll -> "dll"
              | OutputType.Exe -> "exe"
    let binFile = sprintf "%s/%s.%s" MSBUILD_BIN_FOLDER output ext
    let pdbFile = sprintf "%s/%s.pdb" MSBUILD_BIN_FOLDER output

    // This is the import targets that will be Import'ed inside a proj file.
    // First we include full-build view configuration (this is done to avoid adding an extra import inside proj)
    // Then we end up either importing output assembly or project depending on view configuration
    XDocument (
        XElement(NsMsBuild + "Project", 
            XElement (NsMsBuild + "Import",
                XAttribute (NsNone + "Project", "$(FBWorkspaceDir)/.full-build/views/$(SolutionName).targets"),
                XAttribute (NsNone + "Condition", "'$(FullBuild_Config)' == ''")),
            XElement (NsMsBuild + "ItemGroup",
                XElement(NsMsBuild + "ProjectReference",
                    XAttribute (NsNone + "Include", projectFile),
                    XAttribute (NsNone + "Condition", srcCondition),
                    XElement (NsMsBuild + "Project", sprintf "{%s}" project.UniqueProjectId.toString),
                    XElement (NsMsBuild + "Name", project.Output.toString)),
                XElement (NsMsBuild + "Reference",
                    XAttribute (NsNone + "Include", binFile),
                    XAttribute (NsNone + "Condition", binCondition),
                    XElement (NsMsBuild + "Private", "true")),
                XElement (NsMsBuild + "FBCopyFiles", 
                    XAttribute(NsNone + "Include", sprintf "%s;%s" binFile pdbFile),
                    XAttribute(NsNone + "Condition", binCondition)))))

let GenerateProjects (projects : Project seq) (xdocSaver : FileInfo -> XDocument -> Unit) =
    let prjDir = Env.GetFolder Env.Project
    for project in projects do
        let refProjectContent = GenerateProjectTarget project
        let projectFile = prjDir |> GetFile (AddExt Targets (project.Output.toString))
        xdocSaver projectFile refProjectContent


let cleanupProject (xproj : XDocument) (project : Project) : XDocument =
    let filterFullBuildProject (xel : XElement) =
        let attr = !> (xel.Attribute (NsNone + "Project")) : string
        attr.StartsWith(MSBUILD_PROJECT_FOLDER, StringComparison.CurrentCultureIgnoreCase)
            || attr.StartsWith(MSBUILD_PROJECT_FOLDER2, StringComparison.CurrentCultureIgnoreCase)

    let filterFullBuildPackage (xel : XElement) =
        let attr = !> (xel.Attribute (NsNone + "Project")) : string
        attr.StartsWith(MSBUILD_PACKAGE_FOLDER, StringComparison.CurrentCultureIgnoreCase)
            || attr.StartsWith(MSBUILD_PACKAGE_FOLDER2, StringComparison.CurrentCultureIgnoreCase)

    let filterFullBuildTargets (xel : XElement) =
        let attr = !> (xel.Attribute (NsNone + "Project")) : string
        attr.StartsWith(MSBUILD_FULLBUILD_TARGETS, StringComparison.CurrentCultureIgnoreCase)
            || attr.StartsWith(MSBUILD_FULLBUILD_TARGETS2, StringComparison.CurrentCultureIgnoreCase)

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

    let always _ = true

    // cleanup everything that will be modified
    let cproj = XDocument (xproj)

    let seekAndDestroy =
        [ 
            // paket
            "None", filterPaketReference
            "Import", filterPaketTarget
            "Choose", filterPaket

            // project references
            "ProjectReference", always
            
            // unknown assembly references
            "Reference", filterAssemblies project.AssemblyReferences

            // full-build imports
            "Import", filterFullBuildProject
            "Import", filterFullBuildPackage
            "Import", filterFullBuildTargets

            // nuget stuff
            "Import", filterNuget
            "Target", filterNugetTarget
            "None", filterNugetPackage
            "Content", filterNugetPackage

            // cleanup project
            "BaseIntermediateOutputPath", always 
            "SolutionDir", always
            "RestorePackages", always
            "NuGetPackageImportStamp", always
            "ItemGroup", hasNoChild
        ]

    seekAndDestroy |> Seq.iter (fun (x, y) -> cproj.Descendants(NsMsBuild + x).Where(y).Remove())
    cproj


let ConvertProject (xproj : XDocument) (project : Project) =
    let setOutputPath (xel : XElement) =
        xel.Value <- BIN_FOLDER

    let setDebugSymbols (xel : XElement) =
        xel.Value <- "true"

    let setDocumentation (xel : XElement) =
        let fileName = sprintf "%s/%s.xml" BIN_FOLDER project.ProjectId.toString
        xel.Value <- fileName

    let filterAssemblyInfo (xel : XElement) =
        let fileName = !> xel.Attribute(XNamespace.None + "Include") : string
        fileName.IndexOf("AssemblyInfo.", StringComparison.CurrentCultureIgnoreCase) <> -1

    let rec patchAssemblyVersion (lines : string list) =
        match lines with
        | line :: tail -> if line.Contains("AssemblyVersion") || line.Contains("AssemblyFileVersion") then 
                              patchAssemblyVersion tail
                          else 
                              line :: patchAssemblyVersion tail
        | [] -> []

    let patchAssemblyInfo (xel : XElement) =
        let fileName = !> xel.Attribute(XNamespace.None + "Include") : string
        let repoDir = Env.GetFolder Folder.Workspace |> GetSubDirectory project.Repository.toString
        let prjFile = repoDir |> GetFile project.RelativeProjectFile.toString 
        let prjDir = Path.GetDirectoryName (prjFile.FullName) |> DirectoryInfo                       
        let infoFile = prjDir |> GetFile fileName
        let content = File.ReadAllLines (infoFile.FullName) |> List.ofSeq
                                                            |> patchAssemblyVersion
        File.WriteAllLines(infoFile.FullName, content)

    let cproj = cleanupProject xproj project

    // set assembly info
    cproj.Descendants(NsMsBuild + "Compile").Where(filterAssemblyInfo)
                                            |> Seq.iter patchAssemblyInfo

    // set OutputPath
    cproj.Descendants(NsMsBuild + "OutputPath") |> Seq.iter setOutputPath
    cproj.Descendants(NsMsBuild + "DocumentationFile") |> Seq.iter setDocumentation
    cproj.Descendants(NsMsBuild + "AssemblyName") |> Seq.iter (fun x -> x.Value <- project.Output.toString)
    cproj.Descendants(NsMsBuild + "TargetFrameworkVersion") |> Seq.iter (fun x -> x.Value <- project.FxTarget.toString)
    cproj.Descendants(NsMsBuild + "AutoGenerateBindingRedirects") |> Seq.iter (fun x -> x.Value <- "false")

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
        let pkgProperty = PackagePropertyName packageReference
        let condition = sprintf "'$(%s)' == ''" pkgProperty
        let import = XElement (NsMsBuild + "Import",
                        XAttribute (NsNone + "Project", importFile),
                        XAttribute(NsNone + "Condition", condition))
        cproj.Root.LastNode.AddAfterSelf (import)

    // import publish
    let importFB = XElement (NsMsBuild + "Import",
                       XAttribute (NsNone + "Project", Env.MSBUILD_FULLBUILD_TARGETS))

    let firstItemGroup = cproj.Descendants(NsMsBuild + "ItemGroup").First()
    firstItemGroup.AddBeforeSelf (importFB)
    cproj

let ConvertProjectContent (xproj : XDocument) (project : Project) =
    let convxproj = ConvertProject xproj project
    convxproj

let ConvertProjects projects xdocLoader xdocSaver =
    let wsDir = Env.GetFolder Env.Workspace
    for project in projects do
        let repoDir = wsDir |> GetSubDirectory (project.Repository.toString)
        if repoDir.Exists then
            let projFile = repoDir |> GetFile project.RelativeProjectFile.toString 
            let xproj = xdocLoader projFile
            let convxproj = ConvertProjectContent xproj project

            // only save if projs differ
            if xproj.ToString() <> convxproj.ToString() then
                xdocSaver projFile convxproj

let RemoveUselessStuff (projects : Project set) =
    let wsDir = Env.GetFolder Env.Workspace
    let seekAndDestroyFiles = [
                                "*.sln"
                                "packages.config"
                                "paket.dependencies"
                                "paket.lock"
                                "paket.references"
                              ]
    let seekAndDestroyDirs = [
                                "packages"
                                ".paket"
                                ".nuget"
                             ]

    let repos = projects |> Set.map (fun x -> x.Repository)
    for repo in repos do
        let repoDir = wsDir |> GetSubDirectory (repo.toString)
        if repoDir.Exists then
            seekAndDestroyFiles |> Seq.iter (fun x -> repoDir.EnumerateFiles (x, SearchOption.AllDirectories)
                                                      |> Seq.iter (fun x -> x.Delete()))
            seekAndDestroyDirs |> Seq.iter (fun x -> repoDir.EnumerateDirectories (x, SearchOption.AllDirectories)
                                                     |> Seq.iter (fun x -> x.Delete(true)))

    for project in projects do
        let prjDir = wsDir |> GetSubDirectory (AnthologyBridge.RelativeProjectFolderFromWorkspace project)
        if prjDir.Exists then 
            let binDir = prjDir |> GetSubDirectory BIN_FOLDER
            let objDir = prjDir |> GetSubDirectory OBJ_FOLDER
            if binDir.Exists then binDir.Delete(true)
            if objDir.Exists then objDir.Delete(true)
