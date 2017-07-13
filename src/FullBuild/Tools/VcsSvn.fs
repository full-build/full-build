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

module Tools.VcsSvn

open System.IO
open Graph
open Exec


let SVN_CMD = "svn" 

let SvnPull (repoDir : DirectoryInfo) (rebase : bool) =
    ExecGetOutput SVN_CMD "update" repoDir Map.empty

let SvnTip (repoDir : DirectoryInfo) =
    ExecGetOutput "svnversion" "" repoDir Map.empty |> GetOutput |> Seq.head

let SvnClean (repoDir : DirectoryInfo) (repo : Repository) =
    let cleanArgs = "cleanup"
    Exec SVN_CMD cleanArgs repoDir Map.empty |> CheckResponseCode

    SvnPull repoDir false |> CheckResponseCode
    
let SvnClone (target : DirectoryInfo) (url : string) (shallow : bool) =
    let args = sprintf @"checkout %s %s" url target.FullName

    let currDir = IoHelpers.CurrentFolder ()
    ExecGetOutput SVN_CMD args currDir Map.empty

let SvnCheckout (repoDir : DirectoryInfo) (version : string) =
    let args = sprintf "update -r:%A" version
    ExecGetOutput SVN_CMD args repoDir Map.empty

let SvnHistory (repoDir : DirectoryInfo) (version : string) =
    let args = sprintf @"log -r%s:HEAD" version
    try
        ExecGetOutput SVN_CMD args repoDir Map.empty |> GetOutput
    with
        _ -> [sprintf "Failed to get history from version %A - please pull !" version]

let SvnIgnore (repoDir : DirectoryInfo) =
    failwithf "Tag is not supported for Subversion repository"

let SvnFindLatestMatchingTag (repoDir : DirectoryInfo) (filter : string) : string option =
    failwithf "Tag is not supported for Subversion repository"

let SvnTagToHash (repoDir : DirectoryInfo) (tag : string) : string =
    failwithf "Tag is not supported for Subversion repository"

let SvnHead (repoDir : DirectoryInfo) () =
    "HEAD"

let SvnTag (repoDir : DirectoryInfo) (tag : string) (comment : string) =
    failwithf "Tag is not supported for Subversion repository"
