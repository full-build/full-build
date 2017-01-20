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

module TagTests

open System
open NUnit.Framework
open FsUnit
open Baselines

[<Test>]
let ``parse tag incremental`` () =
    let tag = "fullbuild_master_1.2.3_inc"
    let tagInfo = tag |> TagInfo.Parse

    tagInfo.Branch |> should equal "master"
    tagInfo.Version |> should equal "1.2.3"
    tagInfo.Type |> should equal BuildType.Incremental

[<Test>]
let ``parse tag full`` () =
    let tag = "fullbuild_beta_4.5_full"
    let tagInfo = tag |> TagInfo.Parse

    tagInfo.Branch |> should equal "beta"
    tagInfo.Version |> should equal "4.5"
    tagInfo.Type |> should equal BuildType.Full

[<Test>]
let ``parse tag draft`` () =
    let tag = "fullbuild_beta_4.5"
    let tagInfo = tag |> TagInfo.Parse

    tagInfo.Branch |> should equal "beta"
    tagInfo.Version |> should equal "4.5"
    tagInfo.Type |> should equal BuildType.Draft


[<Test>]
let ``parse failures`` () =
    (fun () -> TagInfo.Parse "fullbuild_beta_4.5_pouet" |> ignore) |> should throw typeof<Exception>
    (fun () -> TagInfo.Parse "fullbuild2_beta_4.5_inc" |> ignore) |> should throw typeof<Exception>


[<Test>]
let ``format taginfo inc`` () =
    let tag = "fullbuild_master_1.2.3_inc"
    tag |> TagInfo.Parse |> fun x -> x.Format() |> should equal tag

[<Test>]
let ``format taginfo full`` () =
    let tag = "fullbuild_beta_4.5.6_full"
    tag |> TagInfo.Parse |> fun x -> x.Format() |> should equal tag
