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





let SimplifyAssemblies (projects : Project set) (package2Files : Map<PackageRef, AssemblyRef set>) : Project set =
    let file2package = package2Files |> Map.filter (fun _ nugetFiles -> nugetFiles |> Set.count = 1)
                                        |> Map.toSeq
                                        |> Seq.map (fun (id,nugetFiles) -> (nugetFiles |> Seq.head, id))
                                        |> Map

    let rec convertAssemblies  (projects : Project set) (assemblies : AssemblyRef list) (project : Project) =
        match assemblies with 
        | assName :: moreAss -> let replacementProject = projects |> Seq.tryFind (fun x -> x.Output = assName)
                                match replacementProject with 
                                | Some x -> convertAssemblies projects moreAss { project 
                                                                                 with AssemblyReferences = Set.remove assName project.AssemblyReferences 
                                                                                      ProjectReferences = Set.add (ProjectRef.Bind x) project.ProjectReferences }
                                | _ -> let replacementPackage = file2package.TryFind assName
                                       match replacementPackage with
                                       | Some x -> convertAssemblies projects moreAss { project 
                                                                                        with AssemblyReferences = Set.remove assName project.AssemblyReferences 
                                                                                             PackageReferences = Set.add x project.PackageReferences }
                                       | _ -> convertAssemblies projects moreAss project
        | [] -> project

    let newProjects = projects |> Seq.map (fun x -> convertAssemblies projects (x.AssemblyReferences |> Set.toList) x) |> Set
    newProjects


let SimplifyPackages (projects : Project set) (package2Files : Map<PackageRef, AssemblyRef set>) (package2packages : Map<PackageRef, PackageRef set>) =
    seq {
        let file2package = package2Files |> Map.filter (fun _ nugetFiles -> nugetFiles |> Set.count = 1)
                                         |> Map.toSeq
                                         |> Seq.map (fun (id,nugetFiles) -> (nugetFiles |> Seq.head, id))
                                         |> Map.ofSeq

        let package2project = AssociatePackage2Projects file2package projects
        let packages = (Set << Seq.map fst << Map.toSeq) package2Files
        ()
    }


let SimplifyAnthology (antho : Anthology) (package2files : Map<PackageRef, AssemblyRef set>) (package2packages : Map<PackageRef, PackageRef set>) =
    let simplifiedProjectsWithAssemblies = SimplifyAssemblies antho.Projects package2files
    let simplifiedProjectsWithPackages = simplifiedProjectsWithAssemblies

    let newAntho = { antho
                     with Projects = simplifiedProjectsWithPackages }
    newAntho
