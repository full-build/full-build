//   Copyright 2014-2016 Pierre Chalamet
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

module BaselineSerializerTests

open BaselineSerializer
open FsUnit
open NUnit.Framework
open StringHelpers
open Anthology

[<Test>]
let CheckSaveLoadBaseline () =
    let baseline1 = { Incremental = true
                      Bookmarks = Set [{ Repository = RepositoryId.from "cassandra-sharp"; Version=BookmarkVersion "1234d"}
                                       { Repository = RepositoryId.from "cassandra-sharp-contrib"; Version=BookmarkVersion "5678c"}] }

    let res = BaselineSerializer.SerializeBaseline baseline1
    printfn "%s" res

    let baseline2 = BaselineSerializer.DeserializeBaseline res
    baseline2 |> should equal baseline1
