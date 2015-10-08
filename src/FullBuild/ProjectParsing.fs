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
module ProjectParsing

open System
open System.IO
open System.Linq
open System.Xml.Linq
open Anthology
open StringHelpers
open MsBuildHelpers
open Env
open Collections
open System.Text.RegularExpressions

type ProjectDescriptor = 
    { Packages : Package set
      Project : Project }

let ExtractGuid(xdoc : XDocument) = 
    let xguid = xdoc.Descendants(NsMsBuild + "ProjectGuid").Single()
    let sguid = !> xguid : string
    ParseGuid sguid

let GetProjectGuid (dir : DirectoryInfo) (relFile : string) : Guid = 
    let file = dir |> IoHelpers.GetFile relFile
    let xdoc = XDocument.Load(file.FullName)
    ExtractGuid xdoc

let GetProjectReferences (prjDir : DirectoryInfo) (xdoc : XDocument) = 
    // VS project references
    let prjRefs = xdoc.Descendants(NsMsBuild + "ProjectReference")
                  |> Seq.map (fun x -> !> x.Attribute(XNamespace.None + "Include") : string)
                  |> Seq.map (ProjectId.from << GetProjectGuid prjDir)
                  |> Set
    
    // full-build project references (once converted)
    let fbRefs = xdoc.Descendants(NsMsBuild + "Import")
                 |> Seq.map (fun x -> !> x.Attribute(XNamespace.None + "Project") : string)
                 |> Seq.filter (fun x -> x.StartsWith(MSBUILD_PROJECT_FOLDER))
                 |> Seq.map (ProjectId.from << ParseGuid << Path.GetFileNameWithoutExtension)
                 |> Set
    
    prjRefs |> Set.union fbRefs

let GetAssemblies(xdoc : XDocument) : AssemblyId set = 
    let res = seq { 
        for binRef in xdoc.Descendants(NsMsBuild + "Reference") do
            let inc = !> binRef.Attribute(XNamespace.None + "Include") : string
            let assName = inc.Split([| ',' |], StringSplitOptions.RemoveEmptyEntries).[0]
            let assRef = AssemblyId.from (System.Reflection.AssemblyName(assName))
            yield assRef
    }
    res |> Set

let ParseNuGetPackage (pkgRef : XElement) : Package =
    let pkgId : string = !> pkgRef.Attribute(XNamespace.None + "id")
    let pkgVer = !> pkgRef.Attribute(XNamespace.None + "version") : string
    { Id = PackageId.from pkgId
      Version = PackageVersion pkgVer }

let ParseFullBuildPackage (fileName : string) : Package =
    let fi = FileInfo (fileName)
    let fo = fi.Directory.Name

    { Id = PackageId.from fo
      Version = Unspecified }

let GetNuGetPackages (nugetDoc : XDocument) =
    let nugetPkgs = nugetDoc.Descendants(XNamespace.None + "package") |> Seq.map ParseNuGetPackage 
                                                                      |> Set
    nugetPkgs

let IsPaketReference (xel : XElement) =
    let hasPaket = xel.Descendants(NsMsBuild + "Paket").Any() 
    let hasHintPath = xel.Descendants(NsMsBuild + "HintPath").Any() 
    hasPaket && hasHintPath

let (|MatchPackage|_|) hintpath =
    let m = Regex.Match (hintpath, @".*\\packages\\(?<Package>[^\\]*).*")
    if m.Success then Some (m.Groups.["Package"].Value)
    else None

let GetPackageFromPaketReference (xel : XElement) =
    let xhintPath = xel.Descendants(NsMsBuild + "HintPath") |> Seq.head
    let hintPath = !> xhintPath : string
    match hintPath with
    | MatchPackage pkg -> { Id = PackageId.from pkg
                            Version = Unspecified }
    | _ -> failwith "Failed to find package"

let GetFullBuildPackages (prjDoc : XDocument)  =
    let fbPkgs = prjDoc.Descendants(NsMsBuild + "Import")
                 |> Seq.map (fun x -> !> x.Attribute(XNamespace.None + "Project") : string)
                 |> Seq.filter (fun x -> x.StartsWith(MSBUILD_PACKAGE_FOLDER))
                 |> Seq.map ParseFullBuildPackage
                 |> Set
    fbPkgs

let GetPaketPackages (prjDoc : XDocument)  =
    let paketPkgs = prjDoc.Descendants(NsMsBuild + "Reference")
                    |> Seq.filter IsPaketReference
                    |> Seq.map GetPackageFromPaketReference
                    |> Set
    paketPkgs

let ParseProjectContent (xdocLoader : FileInfo -> XDocument option) (repoDir : DirectoryInfo) (repoRef : RepositoryId) (file : FileInfo) =
    let relativeProjectFile = IoHelpers.ComputeRelativePath repoDir file
    let xprj = match xdocLoader file with
               | Some x -> x
               | _ -> failwithf "Failed to load project %A" file.FullName
    let xguid = !> xprj.Descendants(NsMsBuild + "ProjectGuid").Single() : string
    let guid = ParseGuid xguid
    let assemblyName = !> xprj.Descendants(NsMsBuild + "AssemblyName").Single() : string
    let assemblyRef = AssemblyId.from (assemblyName)
    
    let extension =  match !> xprj.Descendants(NsMsBuild + "OutputType").Single() : string with
                     | "Library" -> OutputType.Dll
                     | _ -> OutputType.Exe
    
    let sfxTarget = !> xprj.Descendants(NsMsBuild + "TargetFrameworkVersion").SingleOrDefault() : string
    let fxTarget = if sfxTarget <> null then sfxTarget
                   else "v4.5"

    let prjRefs = GetProjectReferences file.Directory xprj
    
    let assemblies = GetAssemblies xprj
    let pkgFile = file.Directory |> IoHelpers.GetFile "packages.config"
    let nugetPackages = match xdocLoader pkgFile with
                        | Some xnuget -> GetNuGetPackages xnuget
                        | _ -> Set.empty
    let fbPackages = GetFullBuildPackages xprj
    let paketPackages = GetPaketPackages xprj
    let packages = nugetPackages |> Set.union fbPackages 
                                 |> Set.union paketPackages
    let pkgRefs = packages |> Set.map (fun x -> x.Id)

    { Packages = packages
      Project = { Repository = repoRef
                  RelativeProjectFile = ProjectRelativeFile relativeProjectFile
                  ProjectGuid = ProjectId.from guid
                  Output = assemblyRef
                  OutputType = extension
                  FxTarget = FrameworkVersion fxTarget
                  AssemblyReferences = assemblies
                  PackageReferences = pkgRefs
                  ProjectReferences = prjRefs } }

let XDocumentLoader (f : FileInfo) : XDocument option =
    if f.Exists then Some (XDocument.Load (f.FullName))
    else None

let ParseProject (repoDir : DirectoryInfo) (repoRef : RepositoryId) (file : FileInfo) : ProjectDescriptor = 
    try
        ParseProjectContent XDocumentLoader repoDir repoRef file
    with 
        e -> exn(sprintf "Failed to parse project %A" (file.FullName), e) |> raise
