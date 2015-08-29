module NuGetTests

open FsUnit
open NUnit.Framework
open Anthology


[<Test>]
let CheckCreateDependencies () =
    //
    //    A   E   G
    //   / \   \
    //  B   C   F
    //       \ /
    //        D

    let A = PackageId.Bind "A"
    let B = PackageId.Bind "B"
    let C = PackageId.Bind "C"
    let D = PackageId.Bind "D"
    let E = PackageId.Bind "E"
    let F = PackageId.Bind "F"
    let G = PackageId.Bind "G"

    let dependencies = [ (A, Set [B; C]) 
                         (B, Set.empty)
                         (C, Set [D])
                         (D, Set.empty)
                         (E, Set [F]) 
                         (F, Set [D]) 
                         (G, Set.empty) ] 
                         |> Map

    let roots = NuGets.ComputePackagesRoots dependencies
    roots |> should equal (Set [A; E; G])
