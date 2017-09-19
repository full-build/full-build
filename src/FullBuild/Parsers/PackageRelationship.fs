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

module Parsers.PackageRelationship

open Anthology
open Paket
open FsHelpers
open System.Linq
open System.Xml.Linq
open Collections
open XmlHelpers
open Env

let GetFrameworkDependencies (xnuspec : XDocument) =
    xnuspec.Descendants()
        |> Seq.filter (fun x -> x.Name.LocalName = "frameworkAssembly")
        |> Seq.map (fun x -> !> x.Attribute(NsNone + "assemblyName") : string)
        |> Seq.map AssemblyId.from
        |> Set.ofSeq

let GetPackageDependencies (xnuspec : XDocument) =
    let pkgsDir = Env.GetFolder Env.Folder.Package

    let parseDependency (xelement:XElement) = 
        if xelement.Name.LocalName = "dependency" && (!> xelement.Attribute(NsNone + "exclude") : string) <> "Compile" then 
            let packageId = xelement.Attribute(NsNone + "id").Value |> PackageId.from
            if (pkgsDir |> FsHelpers.GetSubDirectory (packageId.toString) |> fun path -> path.Exists) then
                Some packageId
            else 
                None
        else
            None

    let dep = 
        xnuspec.Elements()
        |> Seq.choose parseDependency
        |> set
        |> fun d -> {PackageDependencies.Framework=None; Dependencies=d}

    let depByTarget = 
        xnuspec.Descendants(NsNone + "group")
        |> Seq.map(fun x -> x.Attribute(NsNone + "targetFramework").Value |> Paket.FrameworkDetection.Extract, x.Elements() |> Seq.choose parseDependency |> set)
        |> Seq.choose(fun (framework, dependencies) -> framework |> Option.map(fun framework -> {PackageDependencies.Framework=Some framework; Dependencies=dependencies}))
        |> List.ofSeq
        
    dep :: depByTarget
    |> set

let rec BuildPackageDependencies (packages : PackageId seq) =
    let pkgsDir = Env.GetFolder Env.Folder.Package

    let rec buildDependencies (packages : PackageId seq) = seq {
        for package in packages do
            let pkgDir = pkgsDir |> GetSubDirectory (package.toString)
            let nuspecFile = pkgDir |> GetFile (FsHelpers.AddExt NuSpec (package.toString))
            let xnuspec = XDocument.Load (nuspecFile.FullName)
            let dependencies = GetPackageDependencies xnuspec
            let dependenciesProjects = dependencies |> Set.toSeq |> Seq.collect (fun d -> d.Dependencies |> Set.toSeq) |> Seq.distinct
            yield {PackageWithDependencies.Package = package; Dependencies = dependencies}
            yield! buildDependencies dependenciesProjects
    }

    packages
        |> buildDependencies


let ComputePackagesRoots (package2packages : Map<PackageId, PackageId set>) =
    let roots = package2packages |> Map.filter (fun pkg _ -> not <| Map.exists (fun _ files -> files |> Set.contains pkg) package2packages)
                                 |> Seq.map (fun x -> x.Key)
                                 |> Set.ofSeq
    roots

let GetDependencyNuspec packageName = 
    Env.GetFolder Folder.Package
    |> GetSubDirectory packageName
    |> GetFile(sprintf "%s.nuspec" packageName)
    |> fun x -> x.FullName
      