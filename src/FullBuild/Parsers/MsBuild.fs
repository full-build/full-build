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

module Parsers.MSBuild

open System
open System.IO
open System.Linq
open System.Xml.Linq
open Anthology
open StringHelpers
open XmlHelpers
open Env
open Collections
open System.Text.RegularExpressions

type ProjectDescriptor =
    { Packages : Package set
      Project : Project }

let private extractOutput(xdoc : XDocument) =
    let xoutput = xdoc.Descendants(NsMsBuild + "AssemblyName").Single()
    let soutput = !> xoutput : string
    soutput

let private getProjectOutput (dir : DirectoryInfo) (relFile : string) =
    let file = dir |> IoHelpers.GetFile relFile
    let xdoc = XDocument.Load(file.FullName)
    extractOutput xdoc

let private getProjectReferences (prjDir : DirectoryInfo) (xdoc : XDocument) =
    // VS project references
    let prjRefs = xdoc.Descendants(NsMsBuild + "ProjectReference")
                  |> Seq.map (fun x -> !> x.Attribute(XNamespace.None + "Include") : string)
                  |> Seq.map IoHelpers.ToWindows
                  |> Seq.map (fun x -> getProjectOutput prjDir x |> ProjectId.from)
                  |> Set.ofSeq

    // full-build project references (once converted)
    let fbRefs = xdoc.Descendants(NsMsBuild + "Import")
                 |> Seq.map (fun x -> !> x.Attribute(XNamespace.None + "Project") : string)
                 |> Seq.map (IoHelpers.MigratePath << IoHelpers.ToWindows)
                 |> Seq.filter (fun x -> x.StartsWith(MSBUILD_PROJECT_FOLDER))
                 |> Seq.map IoHelpers.ToPlatformPath
                 |> Seq.map (fun x -> Path.GetFileNameWithoutExtension x |> ProjectId.from)
                 |> Set.ofSeq

    prjRefs |> Set.union fbRefs

let private getAssemblies(xdoc : XDocument) : AssemblyId set =
    let res = seq {
        for binRef in xdoc.Descendants(NsMsBuild + "Reference") do
            let inc = !> binRef.Attribute(XNamespace.None + "Include") : string
            let assName = inc.Split([| ',' |], StringSplitOptions.RemoveEmptyEntries).[0]
            let assRef = AssemblyId.from (System.Reflection.AssemblyName(assName))
            yield assRef
    }
    res |> Set

let private parseNuGetPackage (pkgRef : XElement) : Package =
    let pkgId : string = !> pkgRef.Attribute(XNamespace.None + "id")
    let pkgVer = !> pkgRef.Attribute(XNamespace.None + "version") : string
    let ver = if pkgVer |> isNull then PackageVersion.Unspecified
              else PackageVersion.PackageVersion pkgVer
    { Id = PackageId.from pkgId
      Version = ver }

let private parsePackageReferencePackage (pkgRef : XElement) : Package =
    let pkgId : string = !> pkgRef.Attribute(XNamespace.None + "Include")
    let pkgVer = !> pkgRef.Descendants(XmlHelpers.NsMsBuild + "Version").SingleOrDefault() : string
    let ver = if pkgVer |> isNull then PackageVersion.Unspecified
              else PackageVersion.PackageVersion pkgVer
    { Id = PackageId.from pkgId
      Version = ver }

let private parseFullBuildPackage (fileName : string) : Package =
    let fi = FileInfo (fileName)
    let fo = fi.Directory.Name

    { Id = PackageId.from fo
      Version = PackageVersion.Unspecified }

let private getNuGetPackages (nugetDoc : XDocument) =
    let nugetPkgs = nugetDoc.Descendants(XNamespace.None + "package") |> Seq.map parseNuGetPackage
                                                                      |> Set
    nugetPkgs

let private isPaketReference (xel : XElement) =
    let hasPaket = xel.Descendants(NsMsBuild + "Paket").Any()
    let hasHintPath = xel.Descendants(NsMsBuild + "HintPath").Any()
    hasPaket && hasHintPath

// NOTE: should be private
let (|MatchPackage|_|) hintpath =
    let m = Regex.Match (hintpath, @".*\\packages\\(?<Package>[^\\]*).*")
    if m.Success then Some (m.Groups.["Package"].Value)
    else None

let private getPackageFromPaketReference (xel : XElement) =
    let xhintPath = xel.Descendants(NsMsBuild + "HintPath") |> Seq.head
    let hintPath = !> xhintPath : string
    match hintPath with
    | MatchPackage pkg -> { Id = PackageId.from pkg
                            Version = PackageVersion.Unspecified }
    | _ -> failwith "Failed to find package"

let private getFullBuildPackages (prjDoc : XDocument)  =
    let fbPkgs = prjDoc.Descendants(NsMsBuild + "Import")
                 |> Seq.map (fun x -> !> x.Attribute(XNamespace.None + "Project") : string)
                 |> Seq.map (IoHelpers.MigratePath << IoHelpers.ToWindows)
                 |> Seq.filter (fun x -> x.StartsWith(MSBUILD_PACKAGE_FOLDER))
                 |> Seq.map IoHelpers.ToPlatformPath
                 |> Seq.map parseFullBuildPackage
                 |> Set.ofSeq
    fbPkgs

let private getPackageReferencePackages (prjDoc : XDocument)  =
    let fbPkgs = prjDoc.Descendants(NsMsBuild + "PackageReference")
                 |> Seq.map parsePackageReferencePackage
                 |> Set.ofSeq
    fbPkgs

let private getPaketPackages (prjDoc : XDocument)  =
    let paketPkgs = prjDoc.Descendants(NsMsBuild + "Reference")
                    |> Seq.filter isPaketReference
                    |> Seq.map getPackageFromPaketReference
                    |> Set.ofSeq
    paketPkgs

// NOTE: should be private
let parseProjectContent (xdocLoader : FileInfo -> XDocument option) (repoDir : DirectoryInfo) (repoRef : RepositoryId) (file : FileInfo) =
    let relativeProjectFile = IoHelpers.ComputeRelativeFilePath repoDir file
    let xprj = match xdocLoader file with
               | Some x -> x
               | _ -> failwithf "Failed to load project %A" file.FullName
    let xguid = !> xprj.Descendants(NsMsBuild + "ProjectGuid").Single() : string
    let guid = ParseGuid xguid
    let assemblyName = !> xprj.Descendants(NsMsBuild + "AssemblyName").Single() : string
    let assemblyRef = AssemblyId.from assemblyName
    let projectRef = ProjectId.from assemblyName

    let extension =  match !> xprj.Descendants(NsMsBuild + "OutputType").Single() : string with
                     | "Library" -> OutputType.Dll
                     | _ -> OutputType.Exe

    let sfxVersion = !> xprj.Descendants(NsMsBuild + "TargetFrameworkVersion").SingleOrDefault() : string
    let sfxProfile = !> xprj.Descendants(NsMsBuild + "TargetFrameworkProfile").SingleOrDefault() : string
    let sfxIdentifier = !> xprj.Descendants(NsMsBuild + "TargetFrameworkIdentifier").SingleOrDefault() : string
    let fxVersion = FxInfo.from sfxVersion
    let fxProfile = FxInfo.from sfxProfile
    let fxIdentifier = FxInfo.from sfxIdentifier

    let prjRefs = getProjectReferences file.Directory xprj

    let assemblies = getAssemblies xprj
    let pkgFile = file.Directory |> IoHelpers.GetFile "packages.config"
    let nugetPackages = match xdocLoader pkgFile with
                        | Some xnuget -> getNuGetPackages xnuget
                        | _ -> Set.empty
    let fbPackages = getFullBuildPackages xprj
    let pkgRefPackages = getPackageReferencePackages xprj
    let paketPackages = getPaketPackages xprj
    let packages = nugetPackages + fbPackages + pkgRefPackages + paketPackages
    let pkgRefs = packages |> Set.map (fun x -> x.Id)
    let hasTests = assemblyRef.toString.EndsWith(".tests")

    { Packages = packages
      Project = { Repository = repoRef
                  RelativeProjectFile = ProjectRelativeFile relativeProjectFile
                  UniqueProjectId = ProjectUniqueId.from guid
                  ProjectId = projectRef
                  Output = assemblyRef
                  OutputType = extension
                  FxVersion = fxVersion
                  FxProfile = fxProfile
                  FxIdentifier = fxIdentifier
                  HasTests = hasTests
                  AssemblyReferences = assemblies
                  PackageReferences = pkgRefs
                  ProjectReferences = prjRefs } }

let ParseProject (repoDir : DirectoryInfo) (repoRef : RepositoryId) (file : FileInfo) : ProjectDescriptor =
    try
        parseProjectContent IoHelpers.XDocLoader repoDir repoRef file
    with
        e -> exn(sprintf "Failed to parse project %A" (file.FullName), e) |> raise
