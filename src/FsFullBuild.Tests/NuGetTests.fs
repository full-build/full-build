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
    let graph = NuGets.CreateNugetDependencyGraph dependencies

    let unbind x = match x with
                   | Some y -> y
                   | _ -> failwith "Expecting Some"

    let nA = graph |> Seq.tryFind (fun x -> x.Id = A) |> unbind
    let nB = graph |> Seq.tryFind (fun x -> x.Id = B) |> unbind
    let nC = graph |> Seq.tryFind (fun x -> x.Id = C) |> unbind
    let nD = graph |> Seq.tryFind (fun x -> x.Id = D) |> unbind
    let nE = graph |> Seq.tryFind (fun x -> x.Id = E) |> unbind
    let nF = graph |> Seq.tryFind (fun x -> x.Id = F) |> unbind
    let nG = graph |> Seq.tryFind (fun x -> x.Id = G) |> unbind

    nA.Dependencies |> should equal (Set [nB; nC])
    nB.Dependencies |> should equal Set.empty
    nC.Dependencies |> should equal (Set [nD])
    nD.Dependencies |> should equal Set.empty
    nE.Dependencies |> should equal (Set [nF])
    nF.Dependencies |> should equal (Set [nD])
    nG.Dependencies |> should equal Set.empty

    let roots = NuGets.GetGraphRoots graph
    roots |> should equal (Set [nA; nE; nG])
