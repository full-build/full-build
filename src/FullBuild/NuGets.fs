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

module NuGets

open Anthology
open IoHelpers
open System.Linq
open System.Xml.Linq
open MsBuildHelpers
open Collections


let GetFrameworkDependencies (xnuspec : XDocument) =
    xnuspec.Descendants().Where(fun x -> x.Name.LocalName = "frameworkAssembly") 
        |> Seq.map (fun x -> !> x.Attribute(NsNone + "assemblyName") : string)
        |> Seq.map AssemblyId.from
        |> set

let GetPackageDependencies (xnuspec : XDocument) =
    xnuspec.Descendants().Where(fun x -> x.Name.LocalName = "dependency") 
        |> Seq.map (fun x -> !> x.Attribute(NsNone + "id") : string)
        |> Seq.map PackageId.from
        |> set

let rec BuildPackageDependencies (packages : PackageId seq) =
    let pkgsDir = Env.GetFolder Env.Package

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

let ComputeTransitivePackageDependencies (packages : PackageId set) =
    let allPackages = BuildPackageDependencies packages
    let ids = allPackages |> Seq.map (fun x -> x.Key) |> Set
    ids


let Add (url : RepositoryUrl) =
    let antho = Configuration.LoadAnthology ()
    let nugets = antho.NuGets @ [url] |> List.distinct
    let newAntho = { antho
                     with
                        NuGets = nugets }
    Configuration.SaveAnthology newAntho

let List () =
    let antho = Configuration.LoadAnthology()
    for nuget in antho.NuGets do
        printfn "%s" nuget.toLocalOrUrl
