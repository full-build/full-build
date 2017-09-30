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
open FsHelpers
open XmlHelpers
open MSBuildHelpers
open Env
open Collections
open Graph


let private generatePackageCopy (packageRef : Package) =
    let version = match packageRef.Version with
                  | Some ver -> XAttribute(NsNone + "Version", ver)
                  | _ -> null
    let packageReference = XElement(NsNone + "PackageReference",
                                XAttribute(NsNone + "Include", packageRef.Name),
                                version)
    packageReference


let private generateProjectCopy (projectRef : Project) =
    let propName = MsBuildProjectPropertyName projectRef
    let condition = sprintf "'$(%sCopy)' == ''" propName
    let project = sprintf @"$(SolutionDir)\.projects\%s-copy.targets" projectRef.Output.Name
    let import = XElement(NsNone + "Import",
                       XAttribute(NsNone + "Project", project),
                       XAttribute(NsNone + "Condition", condition))
    import


let private generateProjectTarget (project : Project) =
    let projectProperty = MsBuildProjectPropertyName project
    let srcCondition = sprintf "'$(%s)' != ''" projectProperty
    let cpyCondition = sprintf "'$(%sCopy)' == ''" projectProperty
    let projectFile = sprintf @"%s\%s\%s" MSBUILD_SOLUTION_DIR (project.Repository.Name) project.ProjectFile
    let output = (project.Output.Name)
    let ext, refType = match project.OutputType with
                       | OutputType.Dll -> "dll", "Reference"
                       | OutputType.Exe -> "exe", "Reference"
                       | OutputType.Database -> "dacpac", "ArtifactReference"
    let binFile = sprintf @"%s\%s.%s" MSBUILD_BIN_FOLDER output ext
    let refFile = sprintf @"%s\.projects\%s-copy.targets" MSBUILD_SOLUTION_DIR project.Output.Name

    // This is the import targets that will be Import'ed inside a proj file.
    // First we include full-build view configuration (this is done to avoid adding an extra import inside proj)
    // Then we end up either importing output assembly or project depending on view configuration
    XDocument (
        XElement(NsNone + "Project",
            XElement (NsNone + "Import",
                XAttribute (NsNone + "Project", @"$(SolutionDir)\.views\$(SolutionName).targets"),
                XAttribute (NsNone + "Condition", "'$(FullBuild_Config)' == ''")),
            XElement(NsNone + "Choose",
                XElement(NsNone + "When", 
                    XAttribute(NsNone + "Condition", srcCondition),
                    XElement (NsNone + "ItemGroup",
                        XElement(NsNone + "ProjectReference",
                            XAttribute (NsNone + "Include", projectFile)))),
                XElement(NsNone + "Otherwise",    
                    XElement(NsNone + "ItemGroup",
                        XElement (NsNone + refType,
                            XAttribute (NsNone + "Include", project.Output.Name),
                            XElement(NsNone + "HintPath", binFile),
                            XElement (NsNone + "Private", "true"))))),
                XElement(NsNone + "Import",
                    XAttribute(NsNone + "Project", refFile),
                    XAttribute(NsNone + "Condition", cpyCondition))))

let private generateProjectCopyTarget (project : Project) =
    let projectProperty = MsBuildProjectPropertyName project
    let projectCopyProperty = projectProperty + "Copy"
    let binCondition = sprintf "'$(%s)' == ''" projectProperty
    let copyCondition = sprintf "'$(%s)' == ''" projectCopyProperty
    let prjFiles = project.References |> Seq.map generateProjectCopy
    let pkgFiles =  project.PackageReferences |> Seq.map generatePackageCopy

    let output = (project.Output.Name)
    let ext = match project.OutputType with
                | OutputType.Dll -> "dll"
                | OutputType.Exe -> "exe"
                | OutputType.Database -> "dacpac"
    let binFile = sprintf @"%s\%s.%s" MSBUILD_BIN_FOLDER output ext
    let pdbFile = sprintf @"%s\%s.pdb" MSBUILD_BIN_FOLDER output
    let mdbFile = sprintf @"%s\%s.%s.mdb" MSBUILD_BIN_FOLDER output ext
    let clrFile = sprintf @"%s\%s.%s.dll" MSBUILD_BIN_FOLDER output ext
    let incFile = match project.OutputType with
                  | OutputType.Database -> sprintf "%s;%s;%s;%s" binFile pdbFile mdbFile clrFile
                  | _ -> sprintf "%s;%s;%s" binFile pdbFile mdbFile

    // This is the import targets that will be Import'ed inside a proj file.
    // First we include full-build view configuration (this is done to avoid adding an extra import inside proj)
    // Then we end up either importing output assembly or project depending on view configuration
    XDocument (
        XElement(NsNone + "Project",
                XAttribute (NsNone + "Condition", copyCondition),
                XElement(NsNone + "PropertyGroup",
                    XElement(NsNone + projectCopyProperty, "Y")),
                XElement (NsNone + "ItemGroup",
                    XElement(NsNone + "FBCopyFiles",
                        XAttribute(NsNone + "Include", incFile))),
                prjFiles,
                XElement(NsNone + "ItemGroup", pkgFiles)))



let private cleanupProject (xproj : XDocument) (project : Project) : XDocument =
    let filterFullBuildProject (xel : XElement) =
        let attr = !> (xel.Attribute (NsNone + "Project")) : string 
        let file = attr |> ToWindows |> MigratePath
        file.StartsWith(MSBUILD_PROJECT_FOLDER, StringComparison.CurrentCultureIgnoreCase)

    let filterFullBuildTargets (xel : XElement) =
        let attr = (!> (xel.Attribute (NsNone + "Project")) : string) |> ToWindows |> MigratePath
        attr.EndsWith(@".full-build\full-build.targets", StringComparison.CurrentCultureIgnoreCase)

    // cleanup everything that will be modified
    let cproj = XDocument (xproj)

    let seekAndDestroy =
        [
            // full-build imports
            "Import", filterFullBuildProject
            "Import", filterFullBuildTargets
        ]

    seekAndDestroy |> List.iter (fun (x, y) -> cproj.Descendants(NsNone + x).Where(y).Remove())
    cproj


let private convertProject (xproj : XDocument) (project : Project) =
    let setOutputPath (xel : XElement) =
        xel.Value <- BIN_FOLDER + "\\"

    let filterAssemblyInfo (xel : XElement) =
        let fileName = !> xel.Attribute(XNamespace.None + "Include") : string
        if fileName |> isNull then false
        else fileName.IndexOf("AssemblyInfo.", StringComparison.CurrentCultureIgnoreCase) <> -1

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
        let prjFile = repoDir |> GetFile (project.ProjectFile |> FsHelpers.ToPlatformPath)
        let prjDir = Path.GetDirectoryName (prjFile.FullName) |> DirectoryInfo
        let infoFile = prjDir |> GetFile fileName
        if infoFile.Exists then
            let content = File.ReadAllLines (infoFile.FullName) |> List.ofSeq
                                                                |> patchAssemblyVersion
            File.WriteAllLines(infoFile.FullName, content)

    let cproj = cleanupProject xproj project

    // set assembly info
    cproj.Descendants(NsNone + "Compile")
        |> Seq.filter filterAssemblyInfo
        |> Seq.iter patchAssemblyInfo

    // import fb target
    let lastImport = upcast cproj.Descendants(NsNone + "PropertyGroup").First() : XNode
    let importFB = XElement (NsNone + "Import",
                       XAttribute (NsNone + "Project",
                                   @"$(SolutionDir)\.full-build\full-build.targets"))
    lastImport.AddAfterSelf (importFB)

    // add project references
    for projectReference in project.References do
        let importFile = sprintf "%s%s.targets" MSBUILD_PROJECT_FOLDER projectReference.Output.Name
        let import = XElement (NsNone + "Import",
                        XAttribute (NsNone + "Project", importFile))
        cproj.Root.LastNode.AddAfterSelf(import)
    cproj


let private convertProjectContent (xproj : XDocument) (project : Project) =
    try
        let convxproj = convertProject xproj project
        convxproj
    with
        e -> exn(sprintf "Failed to convert project %A" (project.ProjectId), e) |> raise

let ConvertProjects (projects : Project seq) xdocLoader xdocSaver =
    let wsDir = Env.GetFolder Env.Folder.Workspace
    for project in projects do
        if project.Repository.IsCloned then
            let repoDir = wsDir |> FsHelpers.GetSubDirectory project.Repository.Name
            let projFile = repoDir |> GetFile (project.ProjectFile |> FsHelpers.ToPlatformPath)
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

let rec removeUselessFiles (excludes : string set) (currentDir : DirectoryInfo) (rootDir : DirectoryInfo) =
    for subdir in currentDir.GetDirectories() do
        removeUselessFiles excludes subdir rootDir

    for file in currentDir.GetFiles() do        
        let delFile = file.Extension.Equals(".sln", StringComparison.CurrentCultureIgnoreCase)
        if delFile then
            let canDelete = Ignore.IsFileIncluded excludes rootDir file
            if canDelete then file.Delete()

let RemoveUselessStuff (projects : Project set) =
    let wsDir = Env.GetFolder Env.Folder.Workspace
    let repos = projects |> Set.map (fun x -> x.Repository)
    for repo in repos do
        let repoDir = wsDir |> GetSubDirectory repo.Name
        let excludes = Ignore.LoadFbIgnore repoDir
        removeUselessFiles excludes repoDir repoDir
