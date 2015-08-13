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


let private FindKnownProjects (repoDir : DirectoryInfo) =
    [AddExt "*" CsProj
     AddExt "*" VbProj
     AddExt "*" FsProj] |> Seq.map (fun x -> repoDir.EnumerateFiles (x, SearchOption.AllDirectories)) 
                        |> Seq.concat

let private ParseRepositoryProjects (parser) (repoDir : DirectoryInfo) =
    repoDir |> FindKnownProjects 
            |> Seq.map (parser repoDir)

let private ParseWorkspaceProjects (parser) (wsDir : DirectoryInfo) (repos : string seq) = 
    repos |> Seq.map (fun x -> GetSubDirectory x wsDir) 
          |> Seq.filter (fun x -> x.Exists) 
          |> Seq.map (ParseRepositoryProjects parser) 
          |> Seq.concat

let Create(path : string) = 
    let wsDir = DirectoryInfo(path)
    wsDir.Create()
    if IsWorkspaceFolder wsDir then failwith "Workspace already exists"
    VcsCloneRepo wsDir GlobalConfig.Repository

    let vwDir = WorkspaceViewFolder ()
    vwDir.Create ()


let Index () = 
    let wsDir = WorkspaceFolder()
    let antho = LoadAnthology()
    let repos = antho.Repositories |> Seq.map (fun x -> x.Name)
    let projects = ParseWorkspaceProjects ProjectParsing.ParseProject wsDir repos

    // FIXME: before merging, it would be better to tell about conflicts

    // merge binaries
    let foundAssemblies = projects |> Seq.map (fun x -> x.Assemblies) 
                                   |> Seq.concat
    let newAssemblies = antho.Assemblies |> Seq.append foundAssemblies 
                                         |> Seq.distinctBy AssemblyRef.From 
                                         |> Seq.toList

    // merge packages
    let foundPackages = projects |> Seq.map (fun x -> x.Packages) 
                                 |> Seq.concat
    let newPackages = antho.Packages |> Seq.append foundPackages 
                                     |> Seq.distinctBy PackageRef.From 
                                     |> Seq.toList

    // merge projects
    let foundProjects = projects |> Seq.map (fun x -> x.Project)
    let newProjects = antho.Projects |> Seq.append foundProjects 
                                     |> Seq.distinctBy ProjectRef.From 
                                     |> Seq.toList

    let newAntho = { antho 
                     with Assemblies = newAssemblies
                          Packages = newPackages 
                          Projects = newProjects }

    SaveAnthology newAntho


let StringifyOutputType (outputType : OutputType) =
    match outputType with
    | OutputType.Exe -> ".exe"
    | OutputType.Dll -> ".dll"
    | _ -> failwithf "Unknown OutputType %A" outputType


let GenerateProjectTarget (project : Project) =
    let projectProperty = ProjectPropertyName project
    let srcCondition = sprintf "'$(%s)' != ''" projectProperty
    let binCondition = sprintf "'$(%s)' == ''" projectProperty
    let projectFile = sprintf "%s/%s/%s" MSBUILD_SOLUTION_DIR project.Repository project.RelativeProjectFile
    let binFile = sprintf "%s/%s/%s%s" MSBUILD_SOLUTION_DIR MSBUILD_BIN_OUTPUT project.AssemblyName <| StringifyOutputType project.OutputType

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
                    XElement (NsMsBuild + "Project", StringifyGuid project.ProjectGuid),
                    XElement (NsMsBuild + "Name", project.AssemblyName)),
                XElement (NsMsBuild + "Reference",
                    XAttribute (NsNone + "Include", project.AssemblyName),
                    XAttribute (NsNone + "Condition", binCondition),
                    XElement (NsMsBuild + "HintPath", binFile),
                    XElement (NsMsBuild + "Private", "true")))))

let GenerateProjects (projects : Project seq) (xdocSaver : FileInfo -> XDocument -> Unit) =
    let prjDir = WorkspaceProjectFolder ()
    for project in projects do
        let content = GenerateProjectTarget project
        let projectFile = prjDir |> GetFile (AddExt (project.ProjectGuid.ToString("D")) Targets)
        xdocSaver projectFile content

let ConvertProject (xproj : XDocument) (project : Project) (nugetFiles) =
    let filterProject (xel : XElement) =
        let attr = xel.Attribute (NsNone + "Project")
        attr.Value.StartsWith (MSBUILD_PROJECT_FOLDER)

    let filterNuget (nugetFiles) (xel : XElement) =
        let hintPaths = xel.Descendants (NsMsBuild + "HintPath")
        hintPaths.Any(fun x -> let hintPath = !> x : string
                               let filename = Path.GetFileName (hintPath)
                               nugetFiles |> Seq.contains filename)

    let hasNoChild (xel : XElement) =
        not <| xel.DescendantNodes().Any()

    let setOutputPath (xel : XElement) =
        xel.Value <- MSBUILD_BIN_FOLDER

    let stringifyGuid (guid : System.Guid) =
        guid.ToString("D")

    // cleanup everything that will be modified
    let cproj = XDocument (xproj)
    cproj.Descendants(NsMsBuild + "ProjectReference").Remove()
    cproj.Descendants(NsMsBuild + "Import").Where(filterProject).Remove()
    cproj.Descendants(NsMsBuild + "BaseIntermediateOutputPath").Remove()
    cproj.Descendants(NsMsBuild + "ItemGroup").Where(hasNoChild).Remove()
    
    // convert nuget to $(SolutionDir)/packages/
    cproj.Descendants(NsMsBuild + "Reference").Where(filterNuget nugetFiles).Remove()

    // set OutputPath
    cproj.Descendants(NsMsBuild + "OutputPath") |> Seq.iter setOutputPath

    // add project refereces
    let afterItemGroup = cproj.Descendants(NsMsBuild + "ItemGroup").First()
    for projectReference in project.ProjectReferences do
        let prjRef = stringifyGuid projectReference
        let importFile = sprintf "%s%s.targets" MSBUILD_PROJECT_FOLDER prjRef
        let import = XElement (NsMsBuild + "Import",
                        XAttribute (NsNone + "Project", importFile))
        afterItemGroup.AddAfterSelf (import)

    // add nuget references
    for packageReference in project.PackageReferences do
        let pkgRef = packageReference
        let importFile = sprintf "%s%s/package.targets" MSBUILD_PACKAGE_FOLDER pkgRef
        let import = XElement (NsMsBuild + "Import",
                        XAttribute (NsNone + "Project", importFile))
        afterItemGroup.AddAfterSelf (import)
    cproj

let GeneratePaketDependenciesContent (packages : Package seq) (config : GlobalConfiguration) =
    seq {
        for nuget in config.NuGets do
            yield sprintf "source %s" nuget

        yield ""
        for package in packages do
            yield sprintf "nuget %s %s" package.Id package.Version
    }

let GeneratePackages (packages : Package seq) =
    let config = Configuration.GlobalConfig
    let content = GeneratePaketDependenciesContent packages config
    let confDir = Env.WorkspaceConfigFolder ()
    let paketDep = confDir |> GetFile "paket.dependencies" 
    File.WriteAllLines (paketDep.FullName, content)

let ConvertProjectContent (xproj : XDocument) (project : Project) (package2Files) =
    let nugetFiles = package2Files |> Seq.filter (fun (id, _) -> project.PackageReferences |> Seq.contains id)
                                    |> Seq.map (fun (_, files) -> files)
                                    |> Seq.concat

    let convxproj = ConvertProject xproj project nugetFiles
    convxproj

let ConvertProjects (antho : Anthology) (package2Files) xdocLoader xdocSaver =
    let wsDir = WorkspaceFolder ()
    for project in antho.Projects do
        let repoDir = wsDir |> GetSubDirectory project.Repository 
        let projFile = repoDir |> GetFile project.RelativeProjectFile 
        printfn "Converting %A" projFile.FullName
        let xproj = xdocLoader projFile
        let convxproj = ConvertProjectContent xproj project package2Files

        xdocSaver projFile convxproj

let XDocumentLoader (fileName : FileInfo) =
    XDocument.Load fileName.FullName

let XDocumentSaver (fileName : FileInfo) (xdoc : XDocument) =
    xdoc.Save (fileName.FullName)

let Convert () = 
    let antho = LoadAnthology ()

    // generate project targets
    GenerateProjects antho.Projects XDocumentSaver

    // generate paket.dependencies and install packages
    GeneratePackages antho.Packages
    Package.Install ()

    // for each package, get all assemblies
    let package2Files = antho.Packages |> Seq.map (fun x -> (x.Id, Package.GatherAllAssemblies x))

    ConvertProjects antho package2Files XDocumentLoader XDocumentSaver
