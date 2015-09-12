module BaseSerializerTests

open BaselineSerializer
open FsUnit
open NUnit.Framework
open StringHelpers
open Anthology

[<Test>]
let CheckSaveLoadBaseline () =
    let baseline1 = { Bookmarks = Set [{ Repository = RepositoryId.from "cassandra-sharp"; Version=BookmarkVersion "1234d"}
                                       { Repository = RepositoryId.from "cassandra-sharp-contrib"; Version=BookmarkVersion "5678c"}] }

    let res = BaselineSerializer.SerializeBaseline baseline1
    printfn "%s" res

    let baseline2 = BaselineSerializer.DeserializeBaseline res
    baseline2 |> should equal baseline1
