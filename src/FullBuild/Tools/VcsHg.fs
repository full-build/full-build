//   Copyright 2014-2017 Pierre Chalamet
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

module Tools.VcsHg

open System.IO
open Graph
open Exec


let HG_CMD = "hg" 

let HgPull (repoDir : DirectoryInfo) (rebase : bool) =
    ExecGetOutput HG_CMD "pull -u" repoDir Map.empty

let HgTip (repoDir : DirectoryInfo) =
    let args = @"id -i"
    ExecGetOutput HG_CMD args repoDir Map.empty |> IO.GetOutput |> Seq.head

let HgClean (repoDir : DirectoryInfo) (repo : Repository) =
    let cleanArgs = "purge"
    Exec HG_CMD cleanArgs repoDir Map.empty |> IO.CheckResponseCode
    
let HgClone (target : DirectoryInfo) (url : string) (shallow : bool) =
    let args = sprintf @"clone %s %s" url target.FullName

    let currDir = FsHelpers.CurrentFolder ()
    ExecGetOutput HG_CMD args currDir Map.empty

let HgCheckout (repoDir : DirectoryInfo) (version : string) =
    let args = sprintf "checkout %A" version
    ExecGetOutput HG_CMD args repoDir Map.empty

let HgHistory (repoDir : DirectoryInfo) (version : string) =
    let args = sprintf @"log -r %s" version
    try
        ExecGetOutput HG_CMD args repoDir Map.empty |> IO.GetOutput
    with
        _ -> [sprintf "Failed to get history from version %A - please pull !" version]

let HgIgnore (repoDir : DirectoryInfo) =
    failwithf "Tag is not supported for Mercurial repository"

let HgFindLatestMatchingTag (repoDir : DirectoryInfo) (filter : string) : string option =
    failwithf "Tag is not supported for Mercurial repository"

let HgTagToHash (repoDir : DirectoryInfo) (tag : string) : string =
    failwithf "Tag is not supported for Mercurial repository"

let HgHead (repoDir : DirectoryInfo) () =
    "tip"

let HgTag (repoDir : DirectoryInfo) (tag : string) (comment : string) =
    failwithf "Tag is not supported for Mercurial repository"

let HgLastCommit (repoDir : DirectoryInfo) (relativeFile : string) =
    failwithf "Last Commit is not supported for Mercurial repository"