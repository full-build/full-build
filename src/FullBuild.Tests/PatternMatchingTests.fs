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

module PatternMatchingTests

open PatternMatching
open NUnit.Framework
open FsUnit

[<Test>]
let CheckEqual () =
    Match "hello" "hello" |> should equal true

[<Test>]
let CheckEqualCaseInsensitive () =
    Match "hello" "HelLo" |> should equal true

[<Test>]
let CheckNotEqual () =
    Match "hello" "world" |> should equal false

[<Test>]
let CheckZeroOrMoreFront () =
    Match "cassandra-sharp" "*sharp" |> should equal true

[<Test>]
let CheckZeroOrMoreBack () =
    Match "cassandra-sharp" "cassandra*" |> should equal true

[<Test>]
let CheckAll () =
    Match "hello world world" "HellO * Wor*d" |> should equal true

[<Test>]
let CheckFailed () =
    Match "hello world world" "_el?Lo* * Wor?p" |> should equal false
