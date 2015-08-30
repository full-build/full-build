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
module Workspace

open System.IO
open IoHelpers
open Env
open Configuration
open Vcs
open Anthology
open MsBuildHelpers
open System.Linq
open System.Xml.Linq
open StringHelpers
open Collections
open System

let private FindKnownProjects (repoDir : DirectoryInfo) =
    [AddExt "*" CsProj
     AddExt "*" VbProj
     AddExt "*" FsProj] |> Seq.map (fun x -> repoDir.EnumerateFiles (x, SearchOption.AllDirectories)) 
                        |> Seq.concat

let private ParseRepositoryProjects (parser) (repoRef : RepositoryId) (repoDir : DirectoryInfo) =
    repoDir |> FindKnownProjects 
            |> Seq.map (parser repoDir repoRef)

let private ParseWorkspaceProjects (parser) (wsDir : DirectoryInfo) (repos : Repository seq) = 
    repos |> Seq.map (fun x -> GetSubDirectory x.Name.Value wsDir) 
          |> Seq.filter (fun x -> x.Exists) 
          |> Seq.map (fun x -> ParseRepositoryProjects parser (RepositoryId.Bind(x.Name)) x)
          |> Seq.concat

let Init(path : string) = 
    let wsDir = DirectoryInfo(path)
    wsDir.Create()
    if IsWorkspaceFolder wsDir then failwith "Workspace already exists"
    VcsCloneRepo wsDir GlobalConfig.Repository

let Create(path : string) = 
    let wsDir = DirectoryInfo(path)
    wsDir.Create()
    if IsWorkspaceFolder wsDir then failwith "Workspace already exists"
    VcsCloneRepo wsDir GlobalConfig.Repository
    let antho = { Applications = Set.empty
                  Bookmarks = Set.empty
                  Repositories = Set.empty
                  Projects = Set.empty }
    let confDir = wsDir |> GetSubDirectory ".full-build"
    let anthoFile = confDir |> GetFile "anthology.json"
    Configuration.SaveAnthologyToFile anthoFile antho

    // FIXME
    //  create git repo in .full-build
    //       generate .gitignore
    //       add content of .full-build to git repo
    //       commit

let Index () = 
    let wsDir = WorkspaceFolder()
    let antho = LoadAnthology()
    let projects = ParseWorkspaceProjects ProjectParsing.ParseProject wsDir antho.Repositories

    // FIXME: before merging, it would be better to tell about conflicts

    // merge packages
    let foundPackages = projects |> Seq.map (fun x -> x.Packages) 
                                 |> Seq.concat
    let existingPackages = PaketParsing.ParsePaketDependencies ()
    let packagesToAdd = foundPackages |> Seq.filter (fun x -> not <| Set.contains x.Id existingPackages)
                                      |> Seq.distinctBy (fun x -> x.Id)
                                      |> Set
    PaketParsing.AppendDependencies packagesToAdd

    // merge projects
    let foundProjects = projects |> Seq.map (fun x -> x.Project)
    let newProjects = antho.Projects |> Seq.append foundProjects 
                                     |> Seq.distinctBy (fun x -> x.ProjectGuid)
                                     |> set

    let newAntho = { antho 
                     with Projects = newProjects }
    SaveAnthology newAntho

    let config = Configuration.GlobalConfig
    PaketParsing.UpdateSources config.NuGets

    Package.Simplify ()


let StringifyOutputType (outputType : OutputType) =
    match outputType with
    | OutputType.Exe -> ".exe"
    | OutputType.Dll -> ".dll"


let GenerateProjectTarget (project : Project) =
    let projectProperty = ProjectPropertyName project
    let srcCondition = sprintf "'$(%s)' != ''" projectProperty
    let binCondition = sprintf "'$(%s)' == ''" projectProperty
    let projectFile = sprintf "%s/%s/%s" MSBUILD_SOLUTION_DIR (project.Repository.Value) project.RelativeProjectFile.Value
    let binFile = sprintf "%s/%s/%s%s" MSBUILD_SOLUTION_DIR MSBUILD_BIN_OUTPUT (project.Output.Value) <| StringifyOutputType project.OutputType

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
                    XElement (NsMsBuild + "Project", StringifyGuid project.ProjectGuid.Value),
                    XElement (NsMsBuild + "Name", project.Output.Value)),
                XElement (NsMsBuild + "Reference",
                    XAttribute (NsNone + "Include", project.Output.Value),
                    XAttribute (NsNone + "Condition", binCondition),
                    XElement (NsMsBuild + "HintPath", binFile),
                    XElement (NsMsBuild + "Private", "true")))))

let GenerateProjects (projects : Project seq) (xdocSaver : FileInfo -> XDocument -> Unit) =
    let prjDir = WorkspaceProjectFolder ()
    for project in projects do
        let content = GenerateProjectTarget project
        let projectFile = prjDir |> GetFile (AddExt (project.ProjectGuid.Value.ToString("D")) Targets)
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

    let filterAssemblies (assFiles) (xel : XElement) =
        let inc = !> xel.Attribute(XNamespace.None + "Include") : string
        let assName = inc.Split([| ',' |], StringSplitOptions.RemoveEmptyEntries).[0]
        let assRef = AssemblyId.Bind (System.Reflection.AssemblyName(assName))
        let res = Set.contains assRef assFiles
        not res

    let hasNoChild (xel : XElement) =
        not <| xel.DescendantNodes().Any()

    let setOutputPath (xel : XElement) =
        xel.Value <- MSBUILD_BIN_FOLDER

    let stringifyGuid (guid : System.Guid) =
        guid.ToString("D")

    // cleanup everything that will be modified
    let cproj = XDocument (xproj)

    // remove project references
    cproj.Descendants(NsMsBuild + "ProjectReference").Remove()
    
    // remove unknown assembly references
    cproj.Descendants(NsMsBuild + "Reference").Where(filterAssemblies project.AssemblyReferences).Remove()

    // remove full-build imports
    cproj.Descendants(NsMsBuild + "Import").Where(filterProject).Remove()
    cproj.Descendants(NsMsBuild + "Import").Where(filterPackage).Remove()

    // remove nuget stuff
    cproj.Descendants(NsMsBuild + "Import").Where(filterNuget).Remove()
    cproj.Descendants(NsMsBuild + "Target").Where(filterNugetTarget).Remove()

    // set OutputPath
    cproj.Descendants(NsMsBuild + "OutputPath") |> Seq.iter setOutputPath

    // cleanup project
    cproj.Descendants(NsMsBuild + "BaseIntermediateOutputPath").Remove()
    cproj.Descendants(NsMsBuild + "ItemGroup").Where(hasNoChild).Remove()

    // add project refereces
    let afterItemGroup = cproj.Descendants(NsMsBuild + "ItemGroup").First()
    for projectReference in project.ProjectReferences do
        let prjRef = projectReference.Value.ToString("D")
        let importFile = sprintf "%s%s.targets" MSBUILD_PROJECT_FOLDER prjRef
        let import = XElement (NsMsBuild + "Import",
                        XAttribute (NsNone + "Project", importFile))
        afterItemGroup.AddAfterSelf (import)

    // add nuget references
    for packageReference in project.PackageReferences do
        let pkgId = packageReference.Value
        let importFile = sprintf "%s%s/package.targets" MSBUILD_PACKAGE_FOLDER pkgId
        let pkgProperty = PackagePropertyName pkgId
        let condition = sprintf "'$(%s)' == ''" pkgProperty
        let import = XElement (NsMsBuild + "Import",
                        XAttribute (NsNone + "Project", importFile),
                        XAttribute(NsNone + "Condition", condition))
        afterItemGroup.AddAfterSelf (import)
    cproj

let ConvertProjectContent (xproj : XDocument) (project : Project) =
    let convxproj = ConvertProject xproj project
    convxproj

let ConvertProjects (antho : Anthology) xdocLoader xdocSaver =
    let wsDir = WorkspaceFolder ()
    for project in antho.Projects do
        let repoDir = wsDir |> GetSubDirectory (project.Repository.Value)
        let projFile = repoDir |> GetFile project.RelativeProjectFile.Value 
        let xproj = xdocLoader projFile
        let convxproj = ConvertProjectContent xproj project

        xdocSaver projFile convxproj

let RemoveUselessStuff (antho : Anthology) =
    let wsDir = WorkspaceFolder ()
    for repo in antho.Repositories do
        let repoDir = wsDir |> GetSubDirectory (repo.Name.Value)
        repoDir.EnumerateFiles("*.sln", SearchOption.AllDirectories) |> Seq.iter (fun x -> x.Delete())
        repoDir.EnumerateFiles("packages.config", SearchOption.AllDirectories) |> Seq.iter (fun x -> x.Delete())
        repoDir.EnumerateDirectories("packages", SearchOption.AllDirectories) |> Seq.iter (fun x -> x.Delete(true))

let XDocumentLoader (fileName : FileInfo) =
    XDocument.Load fileName.FullName

let XDocumentSaver (fileName : FileInfo) (xdoc : XDocument) =
    xdoc.Save (fileName.FullName)

let Convert () = 
    // generate paket.dependencies and install packages
    Package.Install ()

    // generate project targets
    let antho = LoadAnthology ()
    GenerateProjects antho.Projects XDocumentSaver
    ConvertProjects antho XDocumentLoader XDocumentSaver
    RemoveUselessStuff antho

