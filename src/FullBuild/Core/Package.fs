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

module Core.Package
open Anthology
open IoHelpers
open System.IO
open System.Xml.Linq
open System.Linq
open MsBuildHelpers
open Env
open Collections
open Simplify

let private installPackages (nugets : RepositoryUrl list) =
    Tools.PaketInterface.UpdateSources nugets
    Tools.PaketInterface.PaketInstall ()
    Generators.Package.GeneratePackageImports()

let private removeUnusedPackages (antho : Anthology) =
    let packages = Tools.PaketInterface.ParsePaketDependencies ()
    let usedPackages = antho.Projects |> Set.map (fun x -> x.PackageReferences)
                                      |> Set.unionMany
    let packagesToRemove = packages |> Set.filter (fun x -> (not << Set.contains x) usedPackages)
    Tools.PaketInterface.RemoveDependencies packagesToRemove

let private gatherAllAssemblies (package : PackageId) : AssemblyId set =
    let pkgsDir = Env.GetFolder Env.Folder.Package
    let pkgDir = pkgsDir |> GetSubDirectory (package.toString)

    let nuspecFile = pkgDir |> GetFile (IoHelpers.AddExt NuSpec (package.toString))
    let xnuspec = XDocument.Load (nuspecFile.FullName)
    let fxDependencies = Parsers.PackageRelationship.GetFrameworkDependencies xnuspec

    let dlls = pkgDir.EnumerateFiles("*.dll", SearchOption.AllDirectories)
    let exes = pkgDir.EnumerateFiles("*.exes", SearchOption.AllDirectories)
    let files = Seq.append dlls exes |> Seq.map AssemblyId.from
                                     |> Set
    Set.difference files fxDependencies

let private simplifyAnthologyWithPackages (antho) =
    let promotedPackageAntho = SimplifyAnthologyWithoutPackage antho

    let packages = promotedPackageAntho.Projects |> Set.map (fun x -> x.PackageReferences)
                                                 |> Set.unionMany
    let package2packages = Parsers.PackageRelationship.BuildPackageDependencies packages
    let allPackages = package2packages |> Seq.map (fun x -> x.Key)
    let package2files = allPackages |> Seq.map (fun x -> (x, gatherAllAssemblies x))
                                    |> Map
    let newAntho = SimplifyAnthologyWithPackages antho package2files package2packages
    removeUnusedPackages newAntho
    newAntho

let RestorePackages () =
    Tools.PaketInterface.PaketRestore ()
    Generators.Package.GeneratePackageImports()

let UpdatePackages () =
    Tools.PaketInterface.PaketUpdate ()
    Generators.Package.GeneratePackageImports()

let Simplify (antho : Anthology) =
    installPackages antho.NuGets

    let newAntho = simplifyAnthologyWithPackages antho
    newAntho
