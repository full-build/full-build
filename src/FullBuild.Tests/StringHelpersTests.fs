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

module StringHelpersTests

open System
open StringHelpers
open FsUnit
open NUnit.Framework

[<Test>]
let CheckGuidParsing () =
    // F# & C# guid should be equally parsable
    let expected = ParseGuid "{c1d252b7-d766-4c28-9c46-0696f896846d}"
    ParseGuid "c1d252b7-d766-4c28-9c46-0696f896846d" |> should equal expected

    // invalid guid must fail
    (fun () -> ParseGuid "tralala" |> ignore) |> should throw typeof<Exception>


