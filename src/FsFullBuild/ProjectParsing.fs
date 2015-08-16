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

type ProjectDescriptor = 
    { Assemblies : Assembly list
      Packages : Package list
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
                  |> Seq.map (GetProjectGuid prjDir)
    
    // full-build project references (once converted)
    let fbRefs = xdoc.Descendants(NsMsBuild + "Import")
                 |> Seq.map (fun x -> !> x.Attribute(XNamespace.None + "Project") : string)
                 |> Seq.filter (fun x -> x.StartsWith(MSBUILD_PROJECT_FOLDER))
                 |> Seq.map (Path.GetFileNameWithoutExtension)
                 |> Seq.map ParseGuid
    
    prjRefs |> Seq.append fbRefs
            |> Seq.distinct
            |> Seq.toList
            |> List.map ProjectRef.Bind

let GetBinaries(xdoc : XDocument) : Assembly seq = 
    seq { 
        for binRef in xdoc.Descendants(NsMsBuild + "Reference") do
            let inc = !> binRef.Attribute(XNamespace.None + "Include") : string
            let assemblyName = inc.Split([| ',' |], StringSplitOptions.RemoveEmptyEntries).[0]
            yield { AssemblyName = assemblyName }
    }

let ParseNuGetPackage (pkgRef : XElement) : Package =
    let pkgId : string = !> pkgRef.Attribute(XNamespace.None + "id")
    let pkgVer = !> pkgRef.Attribute(XNamespace.None + "version") : string
    let pkgFx = !> pkgRef.Attribute(XNamespace.None + "targetFramework") : string

    { Id = pkgId
      Version = pkgVer
      TargetFramework = pkgFx }

let ParseFullBuildPackage (fileName : string) : Package =
    { Id=Path.GetFileNameWithoutExtension(fileName)
      Version = String.Empty
      TargetFramework = String.Empty }

let GetPackages (prjDoc : XDocument) (nugetDoc : XDocument) =
    let nugetPkgs = nugetDoc.Descendants(XNamespace.None + "package") |> Seq.map ParseNuGetPackage 
                                                                      |> Seq.toList
    let fbPkgs = prjDoc.Descendants(NsMsBuild + "Import")
                 |> Seq.map (fun x -> !> x.Attribute(XNamespace.None + "Project") : string)
                 |> Seq.filter (fun x -> x.StartsWith(MSBUILD_PACKAGE_FOLDER))
                 |> Seq.map ParseFullBuildPackage
    nugetPkgs |> Seq.append fbPkgs |> Seq.toList

let ParseProjectContent (xdocLoader : FileInfo -> XDocument option) (repoDir : DirectoryInfo) (repoRef : RepositoryRef) (file : FileInfo) =
    let relativeProjectFile = IoHelpers.ComputeRelativePath repoDir file
    let xprj = match xdocLoader file with
               | Some x -> x
               | _ -> failwithf "Failed to load project %A" file.FullName
    let xguid = !> xprj.Descendants(NsMsBuild + "ProjectGuid").Single() : string
    let guid = ParseGuid xguid
    let assemblyName = !> xprj.Descendants(NsMsBuild + "AssemblyName").Single() : string
    
    let extension =  match !> xprj.Descendants(NsMsBuild + "OutputType").Single() : string with
                     | "Library" -> OutputType.Dll
                     | _ -> OutputType.Exe
    
    // FIXME
    let fxTarget = "v4.5"
    let prjRefs = GetProjectReferences file.Directory xprj
    
    let assemblies = GetBinaries xprj |> Seq.toList
    let assemblyRefs = assemblies |> List.map AssemblyRef.Bind
    let pkgFile = file.Directory |> IoHelpers.GetFile "packages.config"
    let packages = match xdocLoader pkgFile with
                   | Some xnuget -> GetPackages xprj xnuget
                   | _ -> [] 
    let pkgRefs = packages |> List.map PackageRef.Bind

    { Assemblies = assemblies
      Packages = packages
      Project = { Repository = repoRef
                  RelativeProjectFile = relativeProjectFile
                  ProjectGuid = guid
                  AssemblyName = assemblyName
                  OutputType = extension
                  FxTarget = fxTarget
                  AssemblyReferences = assemblyRefs
                  PackageReferences = pkgRefs
                  ProjectReferences = prjRefs } }

let XDocumentLoader (f : FileInfo) : XDocument option =
    if f.Exists then Some (XDocument.Load (f.FullName))
    else None

let ParseProject (repoDir : DirectoryInfo) (repoRef : RepositoryRef) (file : FileInfo) : ProjectDescriptor = 
    ParseProjectContent XDocumentLoader repoDir repoRef file
