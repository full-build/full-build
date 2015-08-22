module NuGets

open Anthology
open IoHelpers
open System.Linq
open System.Xml.Linq
open MsBuildHelpers
open Collections



type NuGet =
    { Id : PackageRef
      Dependencies : NuGet set }


let GetPackageDependencies (xnuspec : XDocument) =
    xnuspec.Descendants().Where(fun x -> x.Name.LocalName = "dependency") 
        |> Seq.map (fun x -> !> x.Attribute(NsNone + "id") : string)
        |> Seq.map PackageRef.Bind
        |> set

let rec BuildPackageDependencies (packages : PackageRef seq) =
    let pkgsDir = Env.WorkspacePackageFolder ()

    let rec buildDependencies (packages : PackageRef seq) = seq {
        for package in packages do    
            let pkgDir = pkgsDir |> GetSubDirectory (package.Print())
            let nuspecFile = pkgDir |> GetFile (IoHelpers.AddExt (package.Print()) NuSpec)
            let xnuspec = XDocument.Load (nuspecFile.FullName)
            let dependencies = GetPackageDependencies xnuspec
            yield (package, dependencies)
            yield! buildDependencies dependencies
    }

    (buildDependencies packages) |> Map


let rec BuildNugetDependencyGraph (dependencies : Map<PackageRef, PackageRef set>) (nugets : Map<PackageRef, NuGet>) : Set<NuGet> =
    if dependencies.Count = 0 then nugets |> Map.toSeq
                                          |> Seq.map snd
                                          |> Set
    else
        let extractKeys theSet = theSet |> Map.toSeq
                                        |> Seq.map fst
                                        |> Set

        let pkgNuget = extractKeys nugets
        let pkgsToAdd = dependencies |> Map.filter (fun _ deps -> Set.isSubset deps pkgNuget)
        let newNugets = pkgsToAdd |> Map.toSeq
                                  |> Seq.map (fun (id, deps) -> { Id = id; Dependencies = deps |> Set.map (fun x -> nugets.[x]) })
                                  |> Seq.map (fun x -> (x.Id, x)) 
                                  |> Seq.append (nugets |> Map.toSeq)
                                  |> Map
        let newDependencies = dependencies |> Map.filter (fun pkg _ -> not <| Map.containsKey pkg newNugets)
        BuildNugetDependencyGraph newDependencies newNugets 

let rec CreateNugetDependencyGraph (dependencies : Map<PackageRef, PackageRef set>) : Set<NuGet> =
    BuildNugetDependencyGraph dependencies Map.empty


//
//let ComputePackageRoots (package2packages : Map<PackageRef, PackageRef set>) (packages : PackageRef set) =
//    let usedPackages = package2packages |> Map.filter (fun pkg _ -> packages |> Set.contains pkg)
//    let roots = usedPackages |> Map.filter (fun pkg _ -> not (Map.exists (fun _ files -> files |> Set.contains pkg) usedPackages))
//                             |> Map.toSeq
//                             |> Seq.map fst
//                             |> Set
//    roots


let rec GetGraphRoots (nugets : Set<NuGet>) : Set<NuGet> =
    let roots = nugets |> Set.filter (fun nuget -> not <| Set.exists (fun x -> x.Dependencies |> Set.contains nuget) nugets)
    roots

let rec ComputePackageTransitiveDependencies (packageDeps : Map<PackageRef,PackageRef set>) (package : PackageRef) =
    let res = seq {
        yield package
        let dependencies = packageDeps.[package]
        yield! dependencies

        for dependency in dependencies do
            yield! ComputePackageTransitiveDependencies packageDeps dependency
    }
    res |> set

