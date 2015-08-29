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
                                 |> Map.toSeq
                                 |> Seq.map fst
                                 |> Set
    roots
