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
let ``parse tag`` () =
    let tag = "fullbuild/master/1.2.3"
    let tagInfo = tag |> BuildInfo.Parse

    tagInfo.BuildBranch |> should equal "master"
    tagInfo.BuildNumber |> should equal "1.2.3"

[<Test>]
let ``parse failures`` () =
    (fun () -> BuildInfo.Parse "/beta_4.5_pouet" |> ignore) |> should throw typeof<Exception>
    (fun () -> BuildInfo.Parse "fullbuild2_beta_4.5_inc" |> ignore) |> should throw typeof<Exception>

[<Test>]
let ``format taginfo`` () =
    let tag = "fullbuild/master/1.2.3"
    tag |> BuildInfo.Parse |> fun x -> x.Format() |> should equal tag

[<Test>]
let ``check slash is supported in branch`` () =
    let tag = "fullbuild/feature/branch/4.5.6"
    tag |> BuildInfo.Parse |> fun x -> x.Format() |> should equal tag
