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
module NuGets

open Anthology
open IoHelpers
open System.Linq
open System.Xml.Linq
open MsBuildHelpers
open Collections



let GetPackageDependencies (xnuspec : XDocument) =
    xnuspec.Descendants().Where(fun x -> x.Name.LocalName = "dependency") 
        |> Seq.map (fun x -> !> x.Attribute(NsNone + "id") : string)
        |> Seq.map PackageId.Bind
        |> set

let rec BuildPackageDependencies (packages : PackageId seq) =
    let pkgsDir = Env.WorkspacePackageFolder ()

    let rec buildDependencies (packages : PackageId seq) = seq {
        for package in packages do    
            let pkgDir = pkgsDir |> GetSubDirectory (package.Value)
            let nuspecFile = pkgDir |> GetFile (IoHelpers.AddExt (package.Value) NuSpec)
            let xnuspec = XDocument.Load (nuspecFile.FullName)
            let dependencies = GetPackageDependencies xnuspec
            yield (package, dependencies)
            yield! buildDependencies dependencies
    }

    (buildDependencies packages) |> Map


let ComputePackagesRoots (package2packages : Map<PackageId, PackageId set>) =
    let roots = package2packages |> Map.filter (fun pkg _ -> not <| Map.exists (fun _ files -> files |> Set.contains pkg) package2packages)
                                 |> Seq.map (fun x -> x.Key)
                                 |> Set
    roots

let ComputeTransitivePackageDependencies (packages : PackageId seq) =
    let allPackages = BuildPackageDependencies packages
    let ids = allPackages |> Seq.map (fun x -> x.Key) |> Set
    ids

