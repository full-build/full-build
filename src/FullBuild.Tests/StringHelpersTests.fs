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


