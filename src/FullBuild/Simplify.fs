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
module Simplify
open Anthology
open NuGets
open Collections

let (|MatchProject|_|) (projects : Project set) (assName : AssemblyId) = 
    let replacementProject = projects |> Seq.tryFind (fun x -> x.Output = assName)
    match replacementProject with
    | Some x -> Some x.ProjectGuid
    | _ -> None

let (|MatchPackage|_|) (file2package : Map<AssemblyId, PackageId>) (assName : AssemblyId) =
    let replacementPackage = file2package.TryFind assName
    replacementPackage

let RemoveAssembliesFromPackagesOrProjects (package2files : Map<PackageId, AssemblyId set>) (projects : Project set) : Project set =
    let projectOutputs = projects |> Set.map (fun x -> x.Output)
    let allAssembliesFromPackages = package2files |> Seq.map (fun x -> x.Value)
                                                  |> Set.unionMany
    let assembliesToRemove = Set.union allAssembliesFromPackages projectOutputs

    let removeAssembliesFromPackages (assembliesToRemove : AssemblyId set) (project : Project) =
        { project
          with AssemblyReferences = Set.difference project.AssemblyReferences assembliesToRemove }

    let newProjects = projects |> Set.map (removeAssembliesFromPackages assembliesToRemove)
    newProjects


let TransformSingleAssemblyToProjectOrPackage (package2files : Map<PackageId, AssemblyId set>) (projects : Project set) : Project set =
    let file2package = package2files |> Map.filter (fun _ nugetFiles -> nugetFiles |> Set.count = 1)
                                     |> Map.toSeq
                                     |> Seq.map (fun (id, nugetFiles) -> (nugetFiles |> Seq.head, id))
                                     |> Map

    let rec convertAssemblies (projects : Project set) (assemblies : AssemblyId list) (project : Project) : Project =
        match assemblies with 
        | assName::tail -> let nextConversion = convertAssemblies projects tail
                           match assName with
                           | MatchProject projects newProjectRef -> nextConversion { project 
                                                                                     with AssemblyReferences = Set.remove assName project.AssemblyReferences
                                                                                          ProjectReferences = Set.add newProjectRef project.ProjectReferences }
                           | MatchPackage file2package newPackageRef -> nextConversion { project 
                                                                                         with AssemblyReferences = Set.remove assName project.AssemblyReferences
                                                                                              PackageReferences = Set.add newPackageRef project.PackageReferences }
                           | _ -> nextConversion project
        | [] -> project

    let newProjects = projects |> Set.map (fun x -> convertAssemblies projects (x.AssemblyReferences |> Set.toList) x)
    newProjects


let TransformPackagesToProjectsAndPackages (package2packages : Map<PackageId, PackageId set>) (package2files : Map<PackageId, AssemblyId set>) (projects : Project set) =
    let file2package = package2files |> Map.filter (fun _ nugetFiles -> nugetFiles |> Set.count = 1)
                                     |> Map.toSeq
                                     |> Seq.map (fun (id, nugetFiles) -> (nugetFiles |> Seq.head, id))
                                     |> Map

    // convert assemblies to 
    let rec convertPackageFiles (file2packageScoped : Map<AssemblyId, PackageId>) (newProjects : ProjectId set) (newPackages : PackageId set) (files : AssemblyId list) =
        match files with
        | assName::tail -> match assName with
                           | MatchProject projects newProjectRef -> convertPackageFiles file2packageScoped (newProjects |> Set.add newProjectRef) newPackages tail
                           | MatchPackage file2packageScoped newPackageRef -> convertPackageFiles file2packageScoped newProjects (newPackages |> Set.add newPackageRef) tail
                           | _ -> None
        | [] -> Some (newProjects, newPackages)

    let rec convertPackage (package : PackageId) : (ProjectId set * PackageId set) option =
        let file2packageScoped = file2package |> Map.filter (fun _ x -> x <> package)
        let fileConversion = convertPackageFiles file2packageScoped Set.empty Set.empty (package2files.[package] |> Set.toList)
        match fileConversion with
        | None -> None
        | Some (mapPrj, mapPkg) -> let mutable newProjects = mapPrj
                                   let mutable newPackages = mapPkg
                                   let currPackages = package2packages.[package]
                                   for dependency in currPackages do
                                       let depConversion = convertPackage dependency
                                       match depConversion with
                                       | Some (depProjects, depPackages) -> newProjects <- newProjects |> Set.union depProjects
                                                                            newPackages <- newPackages |> Set.union depPackages
                                       | _ -> newPackages <- newPackages |> Set.add dependency
                                   if Set.isSubset newPackages currPackages && newProjects = Set.empty then None
                                   else Some (newProjects, newPackages)

    let simplifiedProjects = seq {
        for project in projects do
            let usedPackages = package2packages |> Map.filter (fun k v -> project.PackageReferences |> Set.contains k)
            let packagesRoot = ComputePackagesRoots usedPackages
            let mutable newProjects = project.ProjectReferences
            let mutable newPackages = Set.empty
            for package in packagesRoot do
                let pkgConversion = convertPackage package
                match pkgConversion with
                | None -> newPackages <- newPackages |> Set.add package
                | Some (prjs, pkgs) -> newProjects <- newProjects |> Set.union prjs
                                       newPackages <- newPackages |> Set.union pkgs
            let simplifiedUsedPackages = package2packages |> Map.filter (fun k v -> newPackages |> Set.contains k)
            let simplifiedPackagesRoot = ComputePackagesRoots simplifiedUsedPackages
            let newProject = { project
                               with PackageReferences = simplifiedPackagesRoot 
                                    ProjectReferences = Set.union project.ProjectReferences newProjects }
            yield newProject
    }
    simplifiedProjects |> Set


let SimplifyAnthology (antho : Anthology) (package2files : Map<PackageId, AssemblyId set>) (package2packages : Map<PackageId, PackageId set>) =
    let newProjects = antho.Projects |> TransformSingleAssemblyToProjectOrPackage  package2files
                                     |> RemoveAssembliesFromPackagesOrProjects package2files
                                     |> TransformPackagesToProjectsAndPackages package2packages package2files

    let newAntho = { antho
                     with Projects = newProjects }
    newAntho
