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

module Simplify
open Anthology
open NuGets
open Collections

let (|MatchProject|_|) (projects : Project set) (assName : AssemblyId) = 
    let replacementProject = projects |> Seq.tryFind (fun x -> x.Output = assName)
    match replacementProject with
    | Some x -> Some x.ProjectId
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


let TransformPackageToProject (projects : Project set) : Project set =
    let packages = projects |> Seq.collect (fun x -> x.PackageReferences) |> Set

    let pkg2prj = Map.ofSeq <| seq {
        for package in packages do
            let assId = AssemblyId.from package.toString
            let project = projects |> Seq.tryFind (fun x -> x.Output = assId)
            match project with
            | Some x -> yield (package, x.ProjectId)
            | _ -> ()
    }

    let res = seq {
        for project in projects do
            let selection = pkg2prj |> Seq.filter (fun x ->project.PackageReferences |> Set.contains x.Key)
            let removeAssembly = selection |> Seq.map (fun x -> AssemblyId.from x.Key.toString) |> Set
            let removePackage = selection |> Seq.map (fun x -> x.Key) |> Set
            let addProject = selection |> Seq.map (fun x -> x.Value) |> Set
            let newProject = { project
                               with AssemblyReferences = Set.difference project.AssemblyReferences removeAssembly
                                    PackageReferences = Set.difference project.PackageReferences removePackage
                                    ProjectReferences = Set.union project.ProjectReferences addProject }
            yield newProject
    }

    res |> Set.ofSeq

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
            let simplifiedUsedPackages = package2packages |> Map.filter (fun k _ -> newPackages |> Set.contains k)
            let simplifiedPackagesRoot = ComputePackagesRoots simplifiedUsedPackages
            let removeAssemblies = projects |> Seq.filter (fun x -> newProjects |> Set.contains x.ProjectId)
                                            |> Seq.map (fun x -> x.Output)
                                            |> Set
            let newProject = { project
                               with PackageReferences = simplifiedPackagesRoot 
                                    AssemblyReferences = Set.difference project.AssemblyReferences removeAssemblies
                                    ProjectReferences = Set.union project.ProjectReferences newProjects }
            yield newProject
    }
    simplifiedProjects |> Set


let SimplifyAnthologyWithoutPackage (antho : Anthology) =
    let newProjects = antho.Projects |> TransformPackageToProject

    let newAntho = { antho
                     with Projects = newProjects }
    newAntho

let SimplifyAnthologyWithPackages (antho : Anthology) (package2files : Map<PackageId, AssemblyId set>) (package2packages : Map<PackageId, PackageId set>) =
    let newProjects = antho.Projects |> TransformSingleAssemblyToProjectOrPackage  package2files
                                     |> TransformPackageToProject
                                     |> TransformPackagesToProjectsAndPackages package2packages package2files
                                     |> RemoveAssembliesFromPackagesOrProjects package2files

    let newAntho = { antho
                     with Projects = newProjects }
    newAntho
