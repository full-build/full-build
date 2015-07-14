module CommandLineParsingTests

open NUnit.Framework
open FsUnit
open CommandLineParsing

[<Test>]
let CheckUsageInvoked () =
    let result = ParseCommandLine [ "workspace"; "create" ]
    let expected = Command.Usage
    result |> should equal expected

[<Test>]
let CheckWorkspaceCreate () =
    let result = ParseCommandLine [ "workspace"; "checkout"; "1234" ]
    let expected = Command.CheckoutWorkspace {Version="1234"}
    result |> should equal expected

[<Test>]
let CheckWorkspaceIndex () =
    let result = ParseCommandLine [ "workspace"; "index" ]
    let expected = Command.IndexWorkspace
    result |> should equal expected

    
[<Test>]
let CheckWorkspaceUpdate () =
    let result = ParseCommandLine [ "workspace"; "update" ]
    let expected = Command.RefreshWorkspace
    result |> should equal expected
