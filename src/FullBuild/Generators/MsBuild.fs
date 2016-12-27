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

module Generators.MSBuild
open System.IO
open System.Xml.Linq
open System
open System.Linq
open IoHelpers
open XmlHelpers
open MSBuildHelpers
open Env
open Collections
open Graph


let private generatePackageCopy (packageRef : Package) =
    let propName = MsBuildPackagePropertyName packageRef
    let condition = sprintf "'$(%sCopy)' == ''" propName
    let project = sprintf @"$(SolutionDir)\.full-build\packages\%s\package-copy.targets" packageRef.Name
    let import = XElement(NsMsBuild + "Import",
                       XAttribute(NsNone + "Project", project),
                       XAttribute(NsNone + "Condition", condition))
    import


let private generateProjectCopy (projectRef : Project) =
    let propName = MsBuildProjectPropertyName projectRef
    let condition = sprintf "'$(%sCopy)' == ''" propName
    let project = sprintf @"$(SolutionDir)\.full-build\projects\%s-copy.targets" projectRef.Output.Name
    let import = XElement(NsMsBuild + "Import",
                       XAttribute(NsNone + "Project", project),
                       XAttribute(NsNone + "Condition", condition))
    import


let private generateProjectTarget (project : Project) =
    let projectProperty = MsBuildProjectPropertyName project
    let srcCondition = sprintf "'$(%s)' != ''" projectProperty
    let binCondition = sprintf "'$(%s)' == ''" projectProperty
    let cpyCondition = sprintf "'$(%sCopy)' == ''" projectProperty
    let projectFile = sprintf @"%s\%s\%s" MSBUILD_SOLUTION_DIR (project.Repository.Name) project.ProjectFile
    let output = (project.Output.Name)
    let ext = match project.OutputType with
              | OutputType.Dll -> "dll"
              | OutputType.Exe -> "exe"
    let binFile = sprintf @"%s\%s.%s" MSBUILD_BIN_FOLDER output ext
    let refFile = sprintf @"%s\.full-build\projects\%s-copy.targets" MSBUILD_SOLUTION_DIR project.Output.Name

    // This is the import targets that will be Import'ed inside a proj file.
    // First we include full-build view configuration (this is done to avoid adding an extra import inside proj)
    // Then we end up either importing output assembly or project depending on view configuration
    XDocument (
        XElement(NsMsBuild + "Project",
            XElement (NsMsBuild + "Import",
                XAttribute (NsNone + "Project", @"$(SolutionDir)\.full-build\views\$(SolutionName).targets"),
                XAttribute (NsNone + "Condition", "'$(FullBuild_Config)' == ''")),
            XElement (NsMsBuild + "ItemGroup",
                XElement(NsMsBuild + "ProjectReference",
                    XAttribute (NsNone + "Include", projectFile),
                    XAttribute (NsNone + "Condition", srcCondition),
                    XElement (NsMsBuild + "Project", sprintf "{%s}" project.UniqueProjectId),
                    XElement (NsMsBuild + "Name", project.Output.Name)),
                XElement (NsMsBuild + "Reference",
                    XAttribute (NsNone + "Include", binFile),
                    XAttribute (NsNone + "Condition", binCondition),
                    XElement (NsMsBuild + "Private", "true"))),
                XElement(NsMsBuild + "Import",
                    XAttribute(NsNone + "Project", refFile),
                    XAttribute(NsNone + "Condition", cpyCondition))))

let private generateProjectCopyTarget (project : Project) =
    let projectProperty = MsBuildProjectPropertyName project
    let projectCopyProperty = projectProperty + "Copy"
    let binCondition = sprintf "'$(%s)' == ''" projectProperty
    let copyCondition = sprintf "'$(%s)' == ''" projectCopyProperty
    let prjFiles = project.References |> Seq.map generateProjectCopy
    let pkgFiles = project.PackageReferences |> Seq.map generatePackageCopy

    let output = (project.Output.Name)
    let ext = match project.OutputType with
                | OutputType.Dll -> "dll"
                | OutputType.Exe -> "exe"
    let binFile = sprintf @"%s\%s.%s" MSBUILD_BIN_FOLDER output ext
    let pdbFile = sprintf @"%s\%s.pdb" MSBUILD_BIN_FOLDER output
    let mdbFile = sprintf @"%s\%s.%s.mdb" MSBUILD_BIN_FOLDER output ext
    let incFile = sprintf "%s;%s;%s" binFile pdbFile mdbFile

    // This is the import targets that will be Import'ed inside a proj file.
    // First we include full-build view configuration (this is done to avoid adding an extra import inside proj)
    // Then we end up either importing output assembly or project depending on view configuration
    XDocument (
        XElement(NsMsBuild + "Project",
                XAttribute (NsNone + "Condition", copyCondition),
                XElement(NsMsBuild + "PropertyGroup",
                    XElement(NsMsBuild + projectCopyProperty, "Y")),
                XElement (NsMsBuild + "ItemGroup",
                    XElement(NsMsBuild + "FBCopyFiles",
                        XAttribute(NsNone + "Include", incFile))),
                prjFiles,
                pkgFiles))



let private cleanupProject (xproj : XDocument) (project : Project) : XDocument =
    let filterFullBuildProject (xel : XElement) =
        let attr = !> (xel.Attribute (NsNone + "Project")) : string
        attr.StartsWith(MSBUILD_PROJECT_FOLDER, StringComparison.CurrentCultureIgnoreCase)
            || attr.StartsWith(MSBUILD_PROJECT_FOLDER2, StringComparison.CurrentCultureIgnoreCase)

    let filterFullBuildPackage (xel : XElement) =
        let attr = !> (xel.Attribute (NsNone + "Project")) : string
        attr.StartsWith(MSBUILD_PACKAGE_FOLDER, StringComparison.CurrentCultureIgnoreCase)
            || attr.StartsWith(MSBUILD_PACKAGE_FOLDER2, StringComparison.CurrentCultureIgnoreCase)

    let filterFullBuildTargets (xel : XElement) =
        let attr = (!> (xel.Attribute (NsNone + "Project")) : string) |> ToWindows
        attr.EndsWith(@".full-build\full-build.targets", StringComparison.CurrentCultureIgnoreCase)

    let filterNuget (xel : XElement) =
        let attr = (!> (xel.Attribute (NsNone + "Project")) : string) |> ToWindows
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
        let attr = (!> (xel.Attribute (NsNone + "Project")) : string) |> ToWindows
        attr.StartsWith("$(SolutionDir)\.paket\paket.targets", StringComparison.CurrentCultureIgnoreCase)

    let filterPaket (xel : XElement) =
        xel.Descendants(NsMsBuild + "Paket").Any ()

    let filterAssemblies (assFiles : Assembly set) (xel : XElement) =
        let inc = !> xel.Attribute(XNamespace.None + "Include") : string
        let assName = inc.Split([| ',' |], StringSplitOptions.RemoveEmptyEntries).[0]
        let assRef = Anthology.AssemblyId.from (System.Reflection.AssemblyName(assName))
        let exists = assFiles |> Set.exists (fun x -> x.Name = assRef.toString)
        not exists

    let hasNoChild (xel : XElement) =
        not <| xel.DescendantNodes().Any()

    let always _ = true

    // cleanup everything that will be modified
    let cproj = XDocument (xproj)

    let seekAndDestroy =
        [
            // package reference
            "PackageReference", always

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
            "FBWorkspaceDir", always

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
            "PropertyGroup", hasNoChild
        ]

    seekAndDestroy |> List.iter (fun (x, y) -> cproj.Descendants(NsMsBuild + x).Where(y).Remove())
    cproj


let private convertProject (xproj : XDocument) (project : Project) =
    let setOutputPath (xel : XElement) =
        xel.Value <- BIN_FOLDER

    let setDocumentation (xel : XElement) =
        let fileName = sprintf "%s/%s.xml" BIN_FOLDER project.Output.Name
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
        let repoDir = Env.GetFolder Folder.Workspace |> GetSubDirectory project.Repository.Name
        let prjFile = repoDir |> GetFile project.ProjectFile
        let prjDir = Path.GetDirectoryName (prjFile.FullName) |> DirectoryInfo
        let infoFile = prjDir |> GetFile fileName
        if infoFile.Exists then
            let content = File.ReadAllLines (infoFile.FullName) |> List.ofSeq
                                                                |> patchAssemblyVersion
            File.WriteAllLines(infoFile.FullName, content)

    let cproj = cleanupProject xproj project

    // set assembly info
    cproj.Descendants(NsMsBuild + "Compile")
        |> Seq.filter filterAssemblyInfo
        |> Seq.iter patchAssemblyInfo

    // set OutputPath
    cproj.Descendants(NsMsBuild + "OutputPath") |> Seq.iter setOutputPath
    cproj.Descendants(NsMsBuild + "DocumentationFile") |> Seq.iter setDocumentation
    cproj.Descendants(NsMsBuild + "AssemblyName") |> Seq.iter (fun x -> x.Value <- project.Output.Name)
    cproj.Descendants(NsMsBuild + "AutoGenerateBindingRedirects") |> Seq.iter (fun x -> x.Value <- "false")

    // TODO: both 3 fields are optional - must discard everything and reset everything if not null or empty
    match project.FxVersion with
    | Some fxVersion -> cproj.Descendants(NsMsBuild + "TargetFrameworkVersion") |> Seq.iter (fun x -> x.Value <- fxVersion)
    | None -> ()

    match project.FxProfile with
    | Some fxProfile -> cproj.Descendants(NsMsBuild + "TargetFrameworkProfile") |> Seq.iter (fun x -> x.Value <- fxProfile)
    | None -> ()

    match project.FxIdentifier with
    | Some fxIdentifier -> cproj.Descendants(NsMsBuild + "TargetFrameworkIdentifier") |> Seq.iter (fun x -> x.Value <- fxIdentifier)
    | None -> ()

    // import fb target
    let firstItemGroup = cproj.Descendants(NsMsBuild + "ItemGroup").First()
    let importFB = XElement (NsMsBuild + "Import",
                       XAttribute (NsNone + "Project",
                                   @"$(SolutionDir)\.full-build\full-build.targets"))
    firstItemGroup.AddBeforeSelf (importFB)

    // add project references
    for projectReference in project.References do
        let importFile = sprintf "%s%s.targets" MSBUILD_PROJECT_FOLDER projectReference.Output.Name
        let import = XElement (NsMsBuild + "Import",
                        XAttribute (NsNone + "Project", importFile))
        cproj.Root.LastNode.AddAfterSelf(import)

    // add nuget references
    for packageReference in project.PackageReferences do
        let importFile = sprintf @"%s%s\package.targets" MSBUILD_PACKAGE_FOLDER packageReference.Name
        let import = XElement (NsMsBuild + "Import",
                        XAttribute (NsNone + "Project", importFile))
        cproj.Root.LastNode.AddAfterSelf(import)
    cproj

let private convertProjectContent (xproj : XDocument) (project : Project) =
    let convxproj = convertProject xproj project
    convxproj

let ConvertProjects (projects : Project seq) xdocLoader xdocSaver =
    let wsDir = Env.GetFolder Env.Folder.Workspace
    for project in projects do
        if project.Repository.IsCloned then
            let repoDir = wsDir |> IoHelpers.GetSubDirectory project.Repository.Name
            let projFile = repoDir |> GetFile project.ProjectFile
            let maybexproj = xdocLoader projFile
            match maybexproj with
            | Some xproj -> let convxproj = convertProjectContent xproj project
                            xdocSaver projFile convxproj
            | _ -> failwithf "Project %A does not exist" projFile

let GenerateProjects (projects : Project seq) (xdocSaver : FileInfo -> XDocument -> Unit) =
    let prjDir = Env.GetFolder Env.Folder.Project
    for project in projects do
        let refProjectContent = generateProjectTarget project
        let projectFile = prjDir |> GetFile (AddExt Targets (project.Output.Name))
        xdocSaver projectFile refProjectContent

        let refProjectCopyContent = generateProjectCopyTarget project
        let projectCopyFile = prjDir |> GetFile (AddExt Targets (project.Output.Name + "-copy"))
        xdocSaver projectCopyFile refProjectCopyContent


let isFileIncluded (excludes : string set) (file : string) (rootDir : string) =
    let relativeFile = file.Replace(rootDir, "") |> IoHelpers.ToUnix |> Set.singleton
    let res = PatternMatching.FilterMatch relativeFile id excludes
    res = Set.empty


let rec removeUselessFiles (excludes : string set) (dir : DirectoryInfo) (rootDir : string) =
    for subdir in dir.GetDirectories() do
        let delDir = subdir.Name.Equals("packages", StringComparison.CurrentCultureIgnoreCase)
                     || subdir.Name.Equals(".paket", StringComparison.CurrentCultureIgnoreCase)
                     || subdir.Name.Equals(".nuget", StringComparison.CurrentCultureIgnoreCase)
        if delDir then 
            let canDelete = isFileIncluded excludes subdir.FullName rootDir
            if canDelete then subdir.Delete(true)
        else removeUselessFiles excludes subdir rootDir

    for file in dir.GetFiles() do        
        let delFile = file.Extension.Equals(".sln", StringComparison.CurrentCultureIgnoreCase)
                      || file.Name.Equals("packages.config", StringComparison.CurrentCultureIgnoreCase)
                      || file.Name.Equals("paket.dependencies", StringComparison.CurrentCultureIgnoreCase)
                      || file.Name.Equals("paket.references", StringComparison.CurrentCultureIgnoreCase)
        if delFile then
            let canDelete = isFileIncluded excludes file.FullName rootDir
            if canDelete then file.Delete()

let private loadFbIgnore (repoDir : DirectoryInfo) : string set =
    let excludeFile = repoDir |> GetFile ".fbignore"
    let excludes = if excludeFile.Exists then System.IO.File.ReadAllLines(excludeFile.FullName)
                                              |> Seq.map (fun x -> let idx = x.IndexOf('#')
                                                                   let s = if idx <> -1  then x.Substring(0, idx)
                                                                                         else x
                                                                   s.Trim())
                                              |> Seq.filter (fun x -> String.IsNullOrWhiteSpace(x) |> not)
                                              |> Seq.map (fun x -> "*" + x)
                                              |> Set
                    else Set.empty
    excludes

let RemoveUselessStuff (projects : Project set) =
    let wsDir = Env.GetFolder Env.Folder.Workspace
    let repos = projects |> Set.map (fun x -> x.Repository)
    for repo in repos do
        let repoDir = wsDir |> GetSubDirectory repo.Name
        let excludes = loadFbIgnore repoDir
        removeUselessFiles excludes repoDir repoDir.FullName
