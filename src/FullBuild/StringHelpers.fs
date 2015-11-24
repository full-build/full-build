// Copyright (c) 2014-2015, Pierre Chalamet
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//     * Redistributions of source code must retain the above copyright
//       notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright
//       notice, this list of conditions and the following disclaimer in the
//       documentation and/or other materials provided with the distribution.
//     * Neither the name of Pierre Chalamet nor the
//       names of its contributors may be used to endorse or promote products
//       derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL PIERRE CHALAMET BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
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
    let inputBytes = System.Text.Encoding.Default.GetBytes(input)
    let hashBytes = provider.ComputeHash(inputBytes) 
    let hashGuid = Guid(hashBytes)
    hashGuid
    