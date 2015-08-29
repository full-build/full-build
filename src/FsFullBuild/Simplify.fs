module Simplify
open Anthology
open NuGets
open Collections

let AssociatePackage2Projects (file2package : Map<AssemblyRef, PackageId>) (projects : Project seq) =
    let outputs = projects |> Seq.map (fun x -> x.Output) |> Set
    let res = file2package |> Seq.filter (fun x -> outputs |> Set.contains x.Key)
                           |> Seq.map (fun x -> (x.Value, x.Key))
    res

let (|MatchProject|_|) (projects : Project set) (assName : AssemblyRef) = 
    let replacementProject = projects |> Seq.tryFind (fun x -> x.Output = assName)
    match replacementProject with
    | Some x -> Some x.ProjectGuid
    | _ -> None

let (|MatchPackage|_|) (file2package : Map<AssemblyRef, PackageId>) (assName : AssemblyRef) =
    let replacementPackage = file2package.TryFind assName
    replacementPackage

let SimplifyAssemblies (projects : Project set) (package2files : Map<PackageId, AssemblyRef set>) : Project set =
    let allAssembliesFromPackages = package2files |> Seq.map (fun x -> x.Value)
                                                  |> Set.unionMany
    let projectOutputs = projects |> Set.map (fun x -> x.Output)
    let assembliesToRemove = Set.union allAssembliesFromPackages projectOutputs

    let file2package = package2files |> Map.filter (fun _ nugetFiles -> nugetFiles |> Set.count = 1)
                                     |> Map.toSeq
                                     |> Seq.map (fun (id, nugetFiles) -> (nugetFiles |> Seq.head, id))
                                     |> Map

    let rec convertAssemblies (projects : Project set) (assemblies : AssemblyRef list) (project : Project) : Project =
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

    let removeAssembliesFromPackages (assembliesToRemove : AssemblyRef set) (project : Project) =
        { project
          with AssemblyReferences = Set.difference project.AssemblyReferences assembliesToRemove }

    let rewriteToProjectOrPackage = projects |> Set.map (fun x -> convertAssemblies projects (x.AssemblyReferences |> Set.toList) x)
    let removedFromPackages = rewriteToProjectOrPackage |> Set.map (removeAssembliesFromPackages assembliesToRemove)
    removedFromPackages


let SimplifyPackages (projects : Project set) (package2packages : Map<PackageId, PackageId set>) (package2files : Map<PackageId, AssemblyRef set>) =
    let file2package = package2files |> Map.filter (fun _ nugetFiles -> nugetFiles |> Set.count = 1)
                                     |> Map.toSeq
                                     |> Seq.map (fun (id, nugetFiles) -> (nugetFiles |> Seq.head, id))
                                     |> Map

    // convert assemblies to 
    let rec convertPackageFiles (file2packageScoped : Map<AssemblyRef, PackageId>) (newProjects : ProjectRef set) (newPackages : PackageId set) (files : AssemblyRef list) =
        match files with
        | assName::tail -> match assName with
                           | MatchProject projects newProjectRef -> convertPackageFiles file2packageScoped (newProjects |> Set.add newProjectRef) newPackages tail
                           | MatchPackage file2packageScoped newPackageRef -> convertPackageFiles file2packageScoped newProjects (newPackages |> Set.add newPackageRef) tail
                           | _ -> None
        | [] -> Some (newProjects, newPackages)

    let rec convertPackage (package : PackageId) : (ProjectRef set * PackageId set) option =
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


let SimplifyAnthology (antho : Anthology) (package2files : Map<PackageId, AssemblyRef set>) (package2packages : Map<PackageId, PackageId set>) =
    let simplifiedProjectsWithAssemblies = SimplifyAssemblies antho.Projects package2files
    let simplifiedProjectsWithPackages = SimplifyPackages simplifiedProjectsWithAssemblies package2packages package2files

    let newAntho = { antho
                     with Projects = simplifiedProjectsWithPackages }
    newAntho
