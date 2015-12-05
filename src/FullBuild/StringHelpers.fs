//   Copyright 2014-2015 Pierre Chalamet
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.

module StringHelpers

open System
open Microsoft.FSharp.Reflection

let ParseGuid(s : string) = 
    match Guid.TryParseExact(s, "B") with // C# guid
    | true, value -> value
    | _ ->  match Guid.TryParseExact(s, "D") with // F# guid
            | true, value -> value
            | _ -> failwithf "string %A is not a Guid" s

let toString (x:'a) = 
    match FSharpValue.GetUnionFields(x, typeof<'a>) with
    | case, _ -> case.Name.ToLowerInvariant()

let fromString<'a> (s:string) =
    let union = FSharpType.GetUnionCases typeof<'a> |> Seq.tryFind(fun x -> String.Equals(x.Name, s, StringComparison.InvariantCultureIgnoreCase))
    match union with
    | Some x -> FSharpValue.MakeUnion(x,[||]) :?> 'a
    | _ -> failwithf "failed to parse %s as %A" s typeof<'a>

let GenerateGuidFromString (input : string) = 
    use provider = new System.Security.Cryptography.MD5CryptoServiceProvider()
    let inputBytes = System.Text.Encoding.GetEncoding(0).GetBytes(input)
    let hashBytes = provider.ComputeHash(inputBytes) 
    let hashGuid = Guid(hashBytes)
    hashGuid
    