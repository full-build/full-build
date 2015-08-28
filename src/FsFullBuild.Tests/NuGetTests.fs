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

    let A = PackageId "A"
    let B = PackageId "B"
    let C = PackageId "C"
    let D = PackageId "D"
    let E = PackageId "E"
    let F = PackageId "F"
    let G = PackageId "G"

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
