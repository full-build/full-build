﻿// Copyright (c) 2014-2015, Pierre Chalamet
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
module PatternMatching

let private (|MatchZeroOrMore|_|) c = 
    match c with
    | '*' -> Some c
    | _ -> None

let rec private MatchRec (content : char list) (pattern : char list) = 
    seq {
        let matchZeroOrMore remainingPattern = 
            seq {
                match content with
                | [] -> yield! MatchRec content remainingPattern
                | _ :: t1 -> 
                    yield! MatchRec content remainingPattern // match 0 time
                    yield! MatchRec t1 pattern // try match one more time
            }
    
        let matchChar firstPatternChar remainingPattern = 
            seq {
                match content with
                | firstContentChar :: remainingContent when firstContentChar = firstPatternChar -> 
                    yield! MatchRec remainingContent remainingPattern
                | _ -> yield false
            }
    
        match pattern with
        | [] -> yield content = []
        | MatchZeroOrMore(_) :: tail -> yield! matchZeroOrMore tail
        | head :: tail -> yield! matchChar head tail
    }

let Match (content : string) (pattern : string) = 
    let matches = MatchRec (content.ToLowerInvariant() |> Seq.toList) (pattern.ToLowerInvariant() |> Seq.toList)
    matches |> Seq.exists (id)
