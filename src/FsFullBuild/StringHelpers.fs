module StringHelpers

open System

let ParseGuid(s : string) = 
    match Guid.TryParseExact(s, "B") with // C# guid
    | true, value -> value
    | _ ->  match Guid.TryParseExact(s, "D") with // F# guid
            | true, value -> value
            | _ -> failwith (sprintf "string %A is not a Guid" s)

let StringifyGuid (guid : Guid) =
    guid.ToString("B")

