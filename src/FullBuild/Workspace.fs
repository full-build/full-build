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
open Collections
open System

let private FindKnownProjects (repoDir : DirectoryInfo) =
    [AddExt "*" CsProj
     AddExt "*" VbProj
     AddExt "*" FsProj] |> Seq.map (fun x -> repoDir.EnumerateFiles (x, SearchOption.AllDirectories)) 
                        |> Seq.concat

let private ProjectCanBeProcessed (fileName : FileInfo) =
    let xdoc = XDocument.Load (fileName.FullName)
    let fbIgnore = !> xdoc.Descendants(NsMsBuild + "FullBuildIgnore").FirstOrDefault() : string
    match bool.TryParse(fbIgnore) with
    | (true, x) -> not <| x
    | _ -> true

let private ParseRepositoryProjects (parser) (repoRef : RepositoryId) (repoDir : DirectoryInfo) =
    repoDir |> FindKnownProjects 
            |> Seq.filter ProjectCanBeProcessed
            |> Seq.map (parser repoDir repoRef)

let private ParseWorkspaceProjects (parser) (wsDir : DirectoryInfo) (repos : Repository seq) = 
    repos |> Seq.map (fun x -> GetSubDirectory x.Name.toString wsDir) 
          |> Seq.filter (fun x -> x.Exists) 
          |> Seq.map (fun x -> ParseRepositoryProjects parser (RepositoryId.from(x.Name)) x)
          |> Seq.concat

let Init (path : string) (uri : RepositoryUrl) = 
    let wsDir = DirectoryInfo(path)
    wsDir.Create()
    if IsWorkspaceFolder wsDir then failwith "Workspace already exists"
    let vcsType = Vcs.VcsDetermineType uri
    let repo = { Name = RepositoryId.from ".full-build"; Url = uri; Vcs=vcsType}
    VcsCloneRepo wsDir repo

let Create (path : string) (uri : RepositoryUrl) (bin : string) = 
    let wsDir = DirectoryInfo(path)
    wsDir.Create()
    if IsWorkspaceFolder wsDir then failwith "Workspace already exists"
    let vcsType = Vcs.VcsDetermineType uri
    let repo = { Name = RepositoryId.from ".full-build"; Url = uri; Vcs=vcsType}
    VcsCloneRepo wsDir repo

    let antho = { Artifacts = bin
                  NuGets = Set.empty
                  Repositories = Set [repo]
                  Projects = Set.empty }
    let confDir = wsDir |> GetSubDirectory ".full-build"
    let anthoFile = confDir |> GetFile "anthology"
    AnthologySerializer.Save anthoFile antho

    let baseline = { Bookmarks = Set.empty }
    let baselineFile = confDir |> GetFile "baseline"
    BaselineSerializer.Save baselineFile baseline

    Vcs.VcsIgnore wsDir repo

let Index () = 
    let wsDir = Env.GetFolder Env.Workspace
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

    PaketParsing.UpdateSources antho.NuGets


let StringifyOutputType (outputType : OutputType) =
    match outputType with
    | OutputType.Exe -> ".exe"
    | OutputType.Dll -> ".dll"


let GenerateProjectTarget (project : Project) =
    let projectProperty = ProjectPropertyName project
    let srcCondition = sprintf "'$(%s)' != ''" projectProperty
    let binCondition = sprintf "'$(%s)' == ''" projectProperty
    let projectFile = sprintf "%s/%s/%s" MSBUILD_SOLUTION_DIR (project.Repository.toString) project.RelativeProjectFile.toString
    let binFile = sprintf "%s/%s/%s%s" MSBUILD_SOLUTION_DIR MSBUILD_BIN_OUTPUT (project.Output.toString) <| StringifyOutputType project.OutputType

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
                    XElement (NsMsBuild + "Project", sprintf "{%s}" project.ProjectGuid.toString),
                    XElement (NsMsBuild + "Name", project.Output.toString)),
                XElement (NsMsBuild + "Reference",
                    XAttribute (NsNone + "Include", project.Output.toString),
                    XAttribute (NsNone + "Condition", binCondition),
                    XElement (NsMsBuild + "HintPath", binFile),
                    XElement (NsMsBuild + "Private", "true")))))

let GenerateProjects (projects : Project seq) (xdocSaver : FileInfo -> XDocument -> Unit) =
    let prjDir = Env.GetFolder Env.Project
    for project in projects do
        let content = GenerateProjectTarget project
        let projectFile = prjDir |> GetFile (AddExt (project.ProjectGuid.toString) Targets)
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
        xel.Value <- MSBUILD_BIN_FOLDER

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

    // remove nuget stuff
    cproj.Descendants(NsMsBuild + "Import").Where(filterNuget).Remove()
    cproj.Descendants(NsMsBuild + "Target").Where(filterNugetTarget).Remove()
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
    let afterItemGroup = cproj.Descendants(NsMsBuild + "ItemGroup").Last()
    for projectReference in project.ProjectReferences do
        let prjRef = projectReference.toString
        let importFile = sprintf "%s%s.targets" MSBUILD_PROJECT_FOLDER prjRef
        let import = XElement (NsMsBuild + "Import",
                        XAttribute (NsNone + "Project", importFile))
        afterItemGroup.AddAfterSelf (import)

    // add nuget references
    for packageReference in project.PackageReferences do
        let pkgId = packageReference.toString
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
    let wsDir = Env.GetFolder Env.Workspace
    for project in antho.Projects do
        let repoDir = wsDir |> GetSubDirectory (project.Repository.toString)
        let projFile = repoDir |> GetFile project.RelativeProjectFile.toString 
        let xproj = xdocLoader projFile
        let convxproj = ConvertProjectContent xproj project

        xdocSaver projFile convxproj

let RemoveUselessStuff () =
    let antho = LoadAnthology ()
    let wsDir = Env.GetFolder Env.Workspace
    for repo in antho.Repositories do
        let repoDir = wsDir |> GetSubDirectory (repo.Name.toString)
        repoDir.EnumerateFiles("*.sln", SearchOption.AllDirectories) |> Seq.iter (fun x -> x.Delete())
        repoDir.EnumerateFiles("packages.config", SearchOption.AllDirectories) |> Seq.iter (fun x -> x.Delete())
        repoDir.EnumerateFiles("paket.dependencies", SearchOption.AllDirectories) |> Seq.iter (fun x -> x.Delete())
        repoDir.EnumerateFiles("paket.lock", SearchOption.AllDirectories) |> Seq.iter (fun x -> x.Delete())
        repoDir.EnumerateFiles("paket.references", SearchOption.AllDirectories) |> Seq.iter (fun x -> x.Delete())
        repoDir.EnumerateDirectories("packages", SearchOption.AllDirectories) |> Seq.iter (fun x -> x.Delete(true))
        repoDir.EnumerateDirectories(".paket", SearchOption.AllDirectories) |> Seq.iter (fun x -> x.Delete(true))

let XDocumentLoader (fileName : FileInfo) =
    XDocument.Load fileName.FullName

let XDocumentSaver (fileName : FileInfo) (xdoc : XDocument) =
    xdoc.Save (fileName.FullName)

let TransformProjects () =
    let antho = LoadAnthology ()
    GenerateProjects antho.Projects XDocumentSaver
    ConvertProjects antho XDocumentLoader XDocumentSaver


let Convert () = 
    Index ()
    Package.Simplify ()
    TransformProjects()
    RemoveUselessStuff ()

let ClonedRepositories (wsDir : DirectoryInfo) (repos : Repository set) =
    repos |> Set.filter (fun x -> let repoDir = wsDir |> GetSubDirectory x.Name.toString
                                  repoDir.Exists)

let CollectRepoHash wsDir (repos : Repository set) =
    let getRepoHash (repo : Repository) =
        let tip = Vcs.VcsTip wsDir repo
        { Repository = repo.Name; Version = BookmarkVersion tip}

    repos |> Set.map getRepoHash

let Push () = 
    let antho = Configuration.LoadAnthology ()
    let wsDir = Env.GetFolder Env.Workspace
    let clonedRepos = antho.Repositories |> ClonedRepositories wsDir
    let bookmarks = CollectRepoHash wsDir clonedRepos
    let baseline = { Bookmarks = bookmarks }
    Configuration.SaveBaseline baseline

    // copy bin content
    let mainRepo = antho.mainRepository
    let hash = Vcs.VcsTip wsDir mainRepo
    let binDir = Env.GetFolder Env.Bin
    let versionDir = DirectoryInfo(antho.Artifacts) |> GetSubDirectory hash
    IoHelpers.CopyFolder binDir versionDir
    printfn "%s" hash

    // commit
    Vcs.VcsCommit wsDir mainRepo "bookmark"
    Vcs.VcsPush wsDir mainRepo


let Checkout (version : BookmarkVersion) =
    let antho = Configuration.LoadAnthology ()
    let wsDir = Env.GetFolder Env.Workspace
    let mainRepo = antho.mainRepository
    Vcs.VcsCheckout wsDir mainRepo version

    // checkout repositories
    let antho = Configuration.LoadAnthology ()
    let baseline = Configuration.LoadBaseline ()
    let clonedRepos = antho.Repositories |> ClonedRepositories wsDir
    for repo in clonedRepos do
        let repoVersion = baseline.Bookmarks |> Seq.tryFind (fun x -> x.Repository = repo.Name)
        match repoVersion with
        | Some x -> Vcs.VcsCheckout wsDir repo x.Version
        | None -> Vcs.VcsCheckout wsDir repo Master

    // copy binaries from version
    let hash = match version with
               | BookmarkVersion x -> x
               | Master -> Vcs.VcsTip wsDir mainRepo

    let binDir = Env.GetFolder Env.Bin
    let versionDir = DirectoryInfo(antho.Artifacts) |> GetSubDirectory hash
    IoHelpers.CopyFolder versionDir binDir

let Pull () =
    let antho = Configuration.LoadAnthology ()
    let wsDir = Env.GetFolder Env.Workspace
    let mainRepo = antho.mainRepository
    Vcs.VcsPull wsDir mainRepo

    let antho = Configuration.LoadAnthology ()
    let clonedRepos = antho.Repositories |> ClonedRepositories wsDir
    for repo in clonedRepos do
        Vcs.VcsPull wsDir repo
