﻿//   Copyright 2014-2017 Pierre Chalamet
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


[<Test>]
let parse_tag_incremental () =
    let tagInfo = Tag.Parse "fullbuild_master_1.2.3_inc"
    let expectedTagInfo = { Tag.TagInfo.Branch = "master"
                            Tag.TagInfo.BuildNumber = "1.2.3"
                            Tag.TagInfo.Incremental = true }

    tagInfo |> should equal expectedTagInfo

[<Test>]
let parse_tag_full () =
    let tagInfo = Tag.Parse "fullbuild_beta_4.5_full"
    let expectedTagInfo = { Tag.TagInfo.Branch = "beta"
                            Tag.TagInfo.BuildNumber = "4.5"
                            Tag.TagInfo.Incremental = false }

    tagInfo |> should equal expectedTagInfo

[<Test>]
let parse_failures () =
    (fun () -> Tag.Parse "fullbuild_beta_4.5" |> ignore) |> should throw typeof<Exception>
    (fun () -> Tag.Parse "fullbuild_beta_4.5_pouet" |> ignore) |> should throw typeof<Exception>
    (fun () -> Tag.Parse "fullbuild2_beta_4.5_inc" |> ignore) |> should throw typeof<Exception>

[<Test>]
let format_taginfo_inc() =
    let tagInfo = { Tag.TagInfo.Branch = "master"
                    Tag.TagInfo.BuildNumber = "1.2.3"
                    Tag.TagInfo.Incremental = true }
    let tag = Tag.Format tagInfo
    tag |> should equal "fullbuild_master_1.2.3_inc"

[<Test>]
let format_taginfo_full() =
    let tagInfo = { Tag.TagInfo.Branch = "beta"
                    Tag.TagInfo.BuildNumber = "4.5.6"
                    Tag.TagInfo.Incremental = false }
    let tag = Tag.Format tagInfo
    tag |> should equal "fullbuild_beta_4.5.6_full"
