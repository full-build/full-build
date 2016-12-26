//   Copyright 2014-2016 Pierre Chalamet
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
open IoHelpers
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
        |> set

let GetPackageDependencies (xnuspec : XDocument) =
    let pkgsDir = Env.GetFolder Env.Folder.Package

    xnuspec.Descendants()
        |> Seq.filter (fun x -> x.Name.LocalName = "dependency" && (!> x.Attribute(NsNone + "exclude") : string) <> "Compile")
        |> Seq.map (fun x -> !> x.Attribute(NsNone + "id") : string)
        |> Seq.map PackageId.from
        |> Seq.filter (fun x -> let path = pkgsDir |> IoHelpers.GetSubDirectory (x.toString)
                                path.Exists)
        |> set

let rec BuildPackageDependencies (packages : PackageId seq) =
    let pkgsDir = Env.GetFolder Env.Folder.Package

    let rec buildDependencies (packages : PackageId seq) = seq {
        for package in packages do
            let pkgDir = pkgsDir |> GetSubDirectory (package.toString)
            let nuspecFile = pkgDir |> GetFile (IoHelpers.AddExt NuSpec (package.toString))
            let xnuspec = XDocument.Load (nuspecFile.FullName)
            let dependencies = GetPackageDependencies xnuspec
            yield (package, dependencies)
            yield! buildDependencies dependencies
    }

    packages
        |> buildDependencies
        |> Map


let ComputePackagesRoots (package2packages : Map<PackageId, PackageId set>) =
    let roots = package2packages |> Map.filter (fun pkg _ -> not <| Map.exists (fun _ files -> files |> Set.contains pkg) package2packages)
                                 |> Seq.map (fun x -> x.Key)
                                 |> Set
    roots

let GetDependencyNuspec packageName = 
    Env.GetFolder Folder.Package
    |> GetSubDirectory packageName
    |> GetFile(sprintf "%s.nuspec" packageName)
    |> fun x -> x.FullName
      