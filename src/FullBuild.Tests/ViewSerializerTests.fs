module ViewSerializerTests

open FsUnit
open NUnit.Framework
open View
open Anthology

[<Test>]
let CheckSaveLoadBaseline () =
    let view1 = { Filters = ["cassandra-sharp/*"] |> Set
                  Builder = BuilderType.MSBuild
                  Parameters = ["--mt"; "--debug" ] |> Set 
                  SourceOnly = true 
                  Parents = false 
                  Modified = false }

    let res = View.SerializeView view1
    printfn "%s" res

    let view2 = View.DeserializeView res
    view2 |> should equal view1
