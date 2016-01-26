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

    let A = PackageId.from "A"
    let B = PackageId.from "B"
    let C = PackageId.from "C"
    let D = PackageId.from "D"
    let E = PackageId.from "E"
    let F = PackageId.from "F"
    let G = PackageId.from "G"

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
