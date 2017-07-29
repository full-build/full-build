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
open FsHelpers


let SVN_CMD = "svn" 



let private GetBranchDirectory (repoDir : DirectoryInfo) =
    let fbBranch = Configuration.LoadBranch()
    let svnBranch = match fbBranch with
                    | "master" -> "trunk" 
                    | x -> x
    repoDir |> GetSubDirectory svnBranch


let SvnPull (repoDir : DirectoryInfo) (rebase : bool) =
    let brDir = repoDir |> GetBranchDirectory
    ExecGetOutput SVN_CMD "update" brDir Map.empty

let SvnTip (repoDir : DirectoryInfo) =
    let brDir = repoDir |> GetBranchDirectory
    ExecGetOutput "svnversion" "" brDir Map.empty |> IO.GetOutput |> Seq.head

let SvnClean (repoDir : DirectoryInfo) (repo : Repository) =
    let brDir = repoDir |> GetBranchDirectory
    let cleanArgs = "cleanup"
    Exec SVN_CMD cleanArgs brDir Map.empty |> IO.CheckResponseCode

    SvnPull brDir false |> IO.CheckResponseCode
    
let SvnClone (target : DirectoryInfo) (url : string) (shallow : bool) =
    let fbBranch = Configuration.LoadBranch()
    let svnBranch = match fbBranch with
                    | "master" -> "trunk" 
                    | x -> x

    let urlBranch = sprintf "%s/%s" url svnBranch
    let brDir = target |> GetBranchDirectory

    let args = sprintf @"checkout %s %s" urlBranch brDir.FullName

    let currDir = FsHelpers.CurrentFolder ()
    ExecGetOutput SVN_CMD args currDir Map.empty

let SvnCheckout (repoDir : DirectoryInfo) (version : string) =
    ConHelpers.DisplayError "WARNING: checkout is not supported on Subversion" 
    let args = sprintf "update -r:%A" version
    ExecGetOutput SVN_CMD args repoDir Map.empty

let SvnHistory (repoDir : DirectoryInfo) (version : string) =
    let brDir = repoDir |> GetBranchDirectory
    let args = sprintf @"log -r%s:HEAD" version
    try
        ExecGetOutput SVN_CMD args brDir Map.empty |> IO.GetOutput
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

let SvnLastCommit (repoDir : DirectoryInfo) (relativeFile : string) =
    failwithf "Last Commit is not supported for Subversion repository"