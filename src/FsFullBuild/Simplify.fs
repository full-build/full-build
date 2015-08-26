module Simplify
open Anthology
open NuGets
open Collections


let AssociatePackage2Projects (file2package : Map<AssemblyRef, PackageRef>) (projects : Project seq) =
    let res = seq {
        for project in projects do
            let package = file2package.TryFind project.Output
            match package with
            | Some x -> yield (x, ProjectRef.Bind project)
            | _ -> ()
    }
    res |> Map


let (|MatchProject|_|) (projects : Project set) (assName : AssemblyRef) = 
    let replacementProject = projects |> Seq.tryFind (fun x -> x.Output = assName)
    match replacementProject with
    | Some x -> Some (ProjectRef.Bind x)
    | _ -> None

let (|MatchPackage|_|) (file2package : Map<AssemblyRef, PackageRef>) (assName : AssemblyRef) =
    let replacementPackage = file2package.TryFind assName
    replacementPackage

let SimplifyAssemblies (projects : Project set) (package2Files : Map<PackageRef, AssemblyRef set>) : Project set =
    let file2package = package2Files |> Map.filter (fun _ nugetFiles -> nugetFiles |> Set.count = 1)
                                        |> Map.toSeq
                                        |> Seq.map (fun (id,nugetFiles) -> (nugetFiles |> Seq.head, id))
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

    let newProjects = projects |> Set.map (fun x -> convertAssemblies projects (x.AssemblyReferences |> Set.toList) x)
    newProjects


//let SimplifyPackages (projects : Project set) (package2packages : Map<PackageRef, PackageRef set>) (package2files : Map<PackageRef, AssemblyRef set>) =



let SimplifyAnthology (antho : Anthology) (package2files : Map<PackageRef, AssemblyRef set>) (package2packages : Map<PackageRef, PackageRef set>) =
    let simplifiedProjectsWithAssemblies = SimplifyAssemblies antho.Projects package2files
    let simplifiedProjectsWithPackages = simplifiedProjectsWithAssemblies

    let newAntho = { antho
                     with Projects = simplifiedProjectsWithPackages }
    newAntho
