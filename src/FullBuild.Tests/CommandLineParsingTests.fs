module CommandLineParsingTests

open NUnit.Framework
open FsUnit
open Commands
open Anthology

[<Test>]
let CheckErrorInvoked () =
    let result = CommandLine.Parse [ "workspace"; "blah blah" ]
    let expected = Command.Error MainCommand.Unknown
    result |> should equal expected

[<Test>]
let CheckUsageInvoked () =
    let result = CommandLine.Parse [ "help" ]
    let expected = Command.Usage
    result |> should equal expected


[<Test>]
let CheckRepositoriesConvert () =
    let result = CommandLine.Parse [ "convert"; "*" ]
    let expected = Command.ConvertRepositories { Filters = [RepositoryId.from "*"] |> Set.ofSeq }
    result |> should equal expected
