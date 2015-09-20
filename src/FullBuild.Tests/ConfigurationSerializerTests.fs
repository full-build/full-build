module ConfigurationSerializerTests

open FsUnit
open NUnit.Framework
open Anthology
open System

[<Test>]
let CheckLoadAndSave () =
    let config1 = { BinRepo = @"C:\Binaries"
                    Repository = { Name=RepositoryId.from ".full-build"; Vcs=VcsType.from "Git"; Url=RepositoryUrl.from "http://www.github.com/"}
                    NuGets = [RepositoryUrl.from "https://www.nuget.org/api/v2/" ; RepositoryUrl.from "file:///C:/src/full-build-packages"] }

    let res = ConfigurationSerializer.SerializeConfiguration config1
    printfn "%s" res

    let config2 = ConfigurationSerializer.DeserializeConfiguration res
    config2 |> should equal config1
