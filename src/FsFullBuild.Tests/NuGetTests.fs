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

    let A = PackageRef.Bind "A"
    let B = PackageRef.Bind "B"
    let C = PackageRef.Bind "C"
    let D = PackageRef.Bind "D"
    let E = PackageRef.Bind "E"
    let F = PackageRef.Bind "F"
    let G = PackageRef.Bind "G"

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
