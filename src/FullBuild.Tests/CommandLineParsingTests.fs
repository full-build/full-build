module CommandLineParsingTests

open NUnit.Framework
open FsUnit
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


//[<Test>]
//let CheckWorkspaceCreate () =
//    let result = ParseCommandLine [ "workspace"; "checkout"; "1234" ]
//    let expected = Command.CheckoutWorkspace {Version="1234"}
//    result |> should equal expected

[<Test>]
let CheckWorkspaceIndex () =
    let result = ParseCommandLine [ "debug"; "workspace"; "index" ]
    let expected = Command.IndexWorkspace
    result |> should equal expected

    
//[<Test>]
//let CheckWorkspaceUpdate () =
//    let result = ParseCommandLine [ "workspace"; "update" ]
//    let expected = Command.RefreshWorkspace
//    result |> should equal expected
