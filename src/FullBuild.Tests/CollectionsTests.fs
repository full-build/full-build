module CollectionsTests

open NUnit.Framework
open FsUnit
open Collections

[<Test>]
let CheckTernary () =
    true ?/ ("a", "b") |> should equal ("a")
    false ?/ ("a", "b") |> should equal ("b")

