module CommandLineParsingTests

open NUnit.Framework
open FsUnit
open CommandLine
open CommandLineParsing

[<Test>]
let CheckErrorInvoked () =
    let result = ParseCommandLine [ "workspace"; "blah blah" ]
    let expected = Command.Error
    result |> should equal expected

[<Test>]
let CheckUsageInvoked () =
    let result = ParseCommandLine [ "help" ]
    let expected = Command.Usage
    result |> should equal expected


[<Test>]
let CheckWorkspaceConvert () =
    let result = ParseCommandLine [ "convert" ]
    let expected = Command.ConvertWorkspace
    result |> should equal expected
