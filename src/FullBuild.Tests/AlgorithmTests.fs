//   Copyright 2014-2017 Pierre Chalamet
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.

module AlgorithmTests
open NUnit.Framework
open FsUnit


let getName = sprintf "Node %A"
let iterDown (links : Map<int, int list>) x = links.[x] |> Set
let iterUp (links : Map<int, int list>) x = links |> Seq.filter (fun y -> y.Value |> List.contains(x))
                                                  |> Seq.map (fun x -> x.Key)
                                                  |> Set


[<Test>]
let CheckCreateClosure () =
    //
    // 1 --> 2 --> 3
    //
    // Closure (1, 3) = 1, 2, 3

    let links = [ (1, [2]); (2, [3]); (3, []) ] |> Map
    let seeds = [1; 3] |> Set
    let nullIterUp _ = Set.empty
    let closure = Algorithm.Closure seeds getName (iterDown links) nullIterUp
    closure |> should equal ([1; 2; 3] |> Set)

[<Test>]
let CheckCreateClosureHole () =
    //
    // 1 <--> 2 <--> 3   4
    //
    // Closure (1, 3) = 1, 2, 3

    let links = [ (1, [2]); (2, [3]); (3, []); (4, []) ] |> Map
    let seeds = [1; 3] |> Set
    let closure = Algorithm.Closure seeds getName (iterDown links) (iterUp links)
    closure |> should equal ([1; 2; 3] |> Set)

[<Test>]
let CheckNoCycle () =
    //
    // 1 --> 2 --> 3
    //
    // Cycle (1) = OK

    let links = [ (1, [2]); (2, [3]); (3, []) ] |> Map
    let seeds = [1; 3] |> Set
    let nullIterUp _ = Set.empty
    Algorithm.FindCycle seeds getName (iterDown links) nullIterUp
        |> should equal None

[<Test>]
let CheckCycle () =
    //
    // 1 --> 2 --> 3
    // ^           |
    // +-----------+
    // 
    //
    // Cycle (1) = fail

    let links = [ (1, [2]); (2, [3]); (3, [1]) ] |> Map
    let seeds = [1] |> Set
    let nullIterUp _ = Set.empty
    Algorithm.FindCycle seeds getName (iterDown links) nullIterUp
        |> should equal (Some "Node 1 -> Node 2 -> Node 3 -> Node 1")
            
