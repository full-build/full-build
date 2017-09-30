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
    let xoutput = xdoc.Descendants(NsNone + "AssemblyName").Single()
    let soutput = !> xoutput : string
    soutput

let private getProjectOutput (dir : DirectoryInfo) (relFile : string) =
    let file = dir |> FsHelpers.GetFile relFile
    let xdoc = XDocument.Load(file.FullName)
    extractOutput xdoc

let private getProjectReferences (prjDir : DirectoryInfo) (xdoc : XDocument) =
    // VS project references
    let prjRefs = xdoc.Descendants(NsNone + "ProjectReference")
                      |> Seq.map (fun x -> !> x.Attribute(XNamespace.None + "Include") : string)
                      |> Seq.map FsHelpers.ToWindows
                      |> Seq.map (fun x -> getProjectOutput prjDir x |> ProjectId.from)
                      |> Set.ofSeq

    // full-build project references (once converted)
    let fbRefs = xdoc.Descendants(NsNone + "Import")
                     |> Seq.map (fun x -> !> x.Attribute(XNamespace.None + "Project") : string)
                     |> Seq.map (FsHelpers.MigratePath << FsHelpers.ToWindows)
                     |> Seq.filter (fun x -> x.StartsWith(MSBUILD_PROJECT_FOLDER))
                     |> Seq.map FsHelpers.ToPlatformPath
                     |> Seq.map (fun x -> Path.GetFileNameWithoutExtension x |> ProjectId.from)
                     |> Set.ofSeq

    prjRefs |> Set.union fbRefs

let private parsePackageReferencePackage (pkgRef : XElement) =
    let mutable pkgId = !> pkgRef.Attribute(XNamespace.None + "Include") : string
    if pkgId |> isNull then 
        pkgId <- (!> pkgRef.Attribute(XNamespace.None + "Update") : string)
    if pkgId |> isNull |> not then
        let pkgVer = !> pkgRef.Descendants(XmlHelpers.NsNone + "Version").SingleOrDefault() : string
        let ver = if pkgVer |> isNull then PackageVersion.Free
                  else PackageVersion.Constraint pkgVer
        Some { Package.Id = PackageId.from pkgId
               Package.Version = ver }
    else
        None

let private getPackageReferencePackages (prjDoc : XDocument)  =
    let fbPkgs = prjDoc.Descendants(NsNone + "PackageReference")
                     |> Seq.map parsePackageReferencePackage
                     |> Seq.choose id
                     |> Set.ofSeq
    fbPkgs

let private getPaketPackages (prjDoc : XDocument) =
    Set.empty

// NOTE: should be private
let parseProjectContent (xdocLoader : FileInfo -> XDocument option) (repoDir : DirectoryInfo) (repoRef : RepositoryId) (sxs : bool) (file : FileInfo) =
    let tmpFile = FsHelpers.ComputeRelativeFilePath repoDir file
    let relativeProjectFile, sxsRoundtrip = 
        let fbExtProj = "-full-build" + file.Extension
        if sxs then
            if tmpFile.Contains(fbExtProj) then tmpFile, true
            else tmpFile.Replace(file.Extension, fbExtProj), false
        else tmpFile, false

    let xprj = match xdocLoader file with
               | Some x -> x
               | _ -> failwithf "Failed to load project %A" file.FullName
    let assemblyName = file.Name |> Path.GetFileNameWithoutExtension
    let assemblyRef = AssemblyId.from assemblyName
    let projectRef = ProjectId.from assemblyName
    let platform = !> xprj.Descendants(NsNone + "TargetFramework").Single() : string

    let extension =  match !> xprj.Descendants(NsNone + "OutputType").SingleOrDefault() : string with
                     | null | "Library" -> OutputType.Dll
                     | "Database" -> OutputType.Database
                     | _ -> OutputType.Exe

    let prjRefs = getProjectReferences file.Directory xprj

    let pkgRefPackages = if sxsRoundtrip then Set.empty else getPackageReferencePackages xprj
    let paketPackages = if sxsRoundtrip then Set.empty else getPaketPackages xprj
    let packages = pkgRefPackages + paketPackages
    let pkgRefs = packages
    let hasTests = assemblyRef.toString.EndsWith("tests")

    { Packages = packages
      Project = { Repository = repoRef
                  RelativeProjectFile = ProjectRelativeFile relativeProjectFile
                  ProjectId = projectRef
                  Output = assemblyRef
                  OutputType = extension
                  HasTests = hasTests
                  PackageReferences = pkgRefs
                  ProjectReferences = prjRefs 
                  Platform = platform } }

let ParseProject (repoDir : DirectoryInfo) (repoRef : RepositoryId) (sxs : bool) (file : FileInfo) : ProjectDescriptor =
    try
        parseProjectContent FsHelpers.XDocLoader repoDir repoRef sxs file
    with
        e -> exn(sprintf "Failed to parse project %A" (file.FullName), e) |> raise
