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

let Init (path : string) =
    let wsDir = new DirectoryInfo(path)
    wsDir.Create()

    if IsWorkspaceFolder wsDir then failwith "Workspace already exists"
    
    VcsCloneRepo wsDir GlobalConfig.Repository 

let private CollectProjects (wsDir : DirectoryInfo) (antho : Anthology) : FileInfo seq =
    seq {
        for repo in antho.Repositories do
            let repoDir = wsDir |> GetSubDirectory repo.Name
            if repoDir.Exists then 
                yield! repoDir.EnumerateFiles ("*.csproj", SearchOption.AllDirectories)
                yield! repoDir.EnumerateFiles ("*.vbproj", SearchOption.AllDirectories)
                yield! repoDir.EnumerateFiles ("*.fsproj", SearchOption.AllDirectories)
    }

let ConvertProject () =
    let wsDir = WorkspaceFolder ()
    let antho = LoadAnthology ()

    let allGuids = antho.Projects |> Seq.map (fun x -> x.ProjectGuid)
    let knownGuids = HashSet<Guid> allGuids

    let projects = CollectProjects wsDir antho
    ()
    



   // let knowProjects = antho.Projects |> Seq.map (x => x.Guid) |> new HashSet<Guid>
