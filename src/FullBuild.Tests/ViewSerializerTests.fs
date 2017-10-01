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

module ViewSerializerTests

open FsUnit
open NUnit.Framework
open Anthology

[<Test>]
let CheckSaveLoadView () =
    let view1 = { Name = "toto"
                  Filters = ["cassandra-sharp/*"] |> Set
                  UpReferences = false 
                  DownReferences = true 
                  Modified = false 
                  AppFilter = Some "tagada*" 
                  Tests = false 
                  Configuration = None }

    let res = ViewSerializer.SerializeView view1
    printfn "%s" res

    let view2 = ViewSerializer.DeserializeView res
    view2 |> should equal view1
