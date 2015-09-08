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
module Repo

open Anthology
open PatternMatching
open Env
open Configuration
open Collections
open IoHelpers

let List() = 
    let antho = LoadAnthology()
    antho.Repositories |> Seq.iter (fun x -> printfn "%s : %s [%A]" x.Name.Value x.Url.Value x.Vcs)

let MatchRepo (repo : Repository set) (filter : RepositoryId) = 
    repo |> Set.filter (fun x -> Match x.Name.Value filter.Value)

let FilterRepos (filters : RepositoryId set) = 
    let antho = LoadAnthology()
    filters |> Seq.map (MatchRepo antho.Repositories)
            |> Seq.concat
            |> Set

let Clone (filters : RepositoryId set) = 
    let wsDir = WorkspaceFolder()
    FilterRepos filters |> Set.filter (fun x -> let subDir = wsDir |> GetSubDirectory x.Name.Value
                                                not <| subDir.Exists)
                        |> Set.iter (Vcs.VcsCloneRepo wsDir)

let Add (repo : Repository) =
    let antho = LoadAnthology ()
    let repos = antho.Repositories |> Set.add repo
                                   |> Seq.distinctBy (fun x -> x.Name)
                                   |> Set
    let newAntho = {antho 
                    with Repositories = repos}
    SaveAnthology newAntho
