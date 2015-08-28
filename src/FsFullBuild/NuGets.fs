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


let ComputePackagesRoots (package2packages : Map<PackageRef, PackageRef set>) =
    let roots = package2packages |> Map.filter (fun pkg _ -> not <| Map.exists (fun _ files -> files |> Set.contains pkg) package2packages)
                                 |> Map.toSeq
                                 |> Seq.map fst
                                 |> Set
    roots
