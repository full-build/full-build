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
module Workspace

open System
open System.IO
open System.Collections.Generic
open FileExtensions
open WellknownFolders
open Configuration
open Vcs
open Anthology

let Init(path : string) = 
    let wsDir = new DirectoryInfo(path)
    wsDir.Create()
    if IsWorkspaceFolder wsDir then failwith "Workspace already exists"
    VcsCloneRepo wsDir GlobalConfig.Repository

let private FindKnownProjects (repoDir : DirectoryInfo) =
    ["*.csproj"; "*.vbproj"; "*.fsproj"] |> Seq.map (fun x -> repoDir.EnumerateFiles (x, SearchOption.AllDirectories)) 
                                         |> Seq.concat

let private ParseRepositoryProjects (parser) (repoDir : DirectoryInfo) =
    repoDir |> FindKnownProjects 
            |> Seq.map (parser repoDir)

let private ParseWorkspaceProjects (parser) (wsDir : DirectoryInfo) (repos : string seq) : Project seq = 
    repos |> Seq.map (GetSubDirectory wsDir) 
          |> Seq.filter (fun x -> x.Exists) 
          |> Seq.map (ParseRepositoryProjects parser) 
          |> Seq.concat

let ConvertProject() = 
    let wsDir = WorkspaceFolder()
    let antho = LoadAnthology()
    let repos = antho.Repositories |> Seq.map (fun x -> x.Name)
    let projects = ParseWorkspaceProjects ProjectParsing.ParseProject wsDir repos

    // get all known projects and ensure tp
    let knownGuids = antho.Projects 
                     |> Seq.map (fun x -> x.ProjectGuid) 
                     |> HashSet<Guid>
    ()
// let knowProjects = antho.Projects |> Seq.map (x => x.Guid) |> new HashSet<Guid>

