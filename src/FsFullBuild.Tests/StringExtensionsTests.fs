module StringExtensionsTests

open StringExtensions
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
