module ViewSerializerTests

open FsUnit
open NUnit.Framework
open Anthology

[<Test>]
let CheckSaveLoadBaseline () =
    let view1 = { Filters = ["cassandra-sharp/*"] |> Set
                  Builder = BuilderType.MSBuild
                  Parameters = ["--mt"; "--debug" ] |> Set 
                  SourceOnly = true }

    let res = ViewSerializer.SerializeView view1
    printfn "%s" res

    let view2 = ViewSerializer.DeserializeView res
    view2 |> should equal view1
