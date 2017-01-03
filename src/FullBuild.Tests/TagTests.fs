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

open NUnit.Framework
open FsUnit


[<Test>]
let parse_tag_incremental () =
    let tagInfo = Tag.Parse "fullbuild-master-1.2.3-inc"
    let expectedTagInfo = { Tag.TagInfo.Branch = "master"
                            Tag.TagInfo.BuildNumber = "1.2.3"
                            Tag.TagInfo.Incremental = true }

    tagInfo |> should equal expectedTagInfo

[<Test>]
let parse_tag_full () =
    let tagInfo = Tag.Parse "fullbuild-beta-4.5-full"
    let expectedTagInfo = { Tag.TagInfo.Branch = "beta"
                            Tag.TagInfo.BuildNumber = "4.5"
                            Tag.TagInfo.Incremental = false }

    tagInfo |> should equal expectedTagInfo
