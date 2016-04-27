//   Copyright 2014-2016 Pierre Chalamet
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

module VcsHg

open Anthology
open System.IO
open IoHelpers

let private checkErrorCode err =
    if err <> 0 then failwithf "Process failed with error %d" err

let private checkedExec = 
    Exec.Exec checkErrorCode

let private checkedExecReadLine =
    Exec.ExecReadLine checkErrorCode

let HgCommit (repoDir : DirectoryInfo) (comment : string) =
    checkedExec "git" "add -S *" repoDir
    let args = sprintf "commit -A -m %A" comment
    checkedExec "hg" args repoDir


let HgPush (repoDir : DirectoryInfo) =
    checkedExec "hg" "push" repoDir
    

let HgPull (repoDir : DirectoryInfo) =
    checkedExec "hg" "pull -u" repoDir

let HgTip (repoDir : DirectoryInfo) =
    let args = @"id -i"
    let res = checkedExecReadLine "hg" args repoDir
    res


let HgClean (repoDir : DirectoryInfo) (repo : Repository) =
    checkedExec "hg" "purge" repoDir

let HgIs (uri : RepositoryUrl) =
    try
        let currDir = IoHelpers.CurrentFolder()
        let args = sprintf @"id -i -R %A" uri.toLocalOrUrl
        checkedExecReadLine "hg" args currDir |> ignore
        true
    with
        _ -> false

let HgClone (branch : BranchId option) (target : DirectoryInfo) (url : string) = 
    let bronly = match branch with
                 | None -> ""
                 | Some x -> sprintf "-r %s" x.toString

    let args = sprintf @"clone %s %A %A" bronly url target.FullName
    let currDir = IoHelpers.CurrentFolder ()
    checkedExec "hg" args currDir


let HgCheckout (repoDir : DirectoryInfo) (version : BookmarkVersion option) = 
    let rev = match version with
              | Some (BookmarkVersion x) -> x
              | None -> "tip"

    let args = sprintf "update -r %A" rev
    checkedExec "hg" args repoDir


let HgHistory (repoDir : DirectoryInfo) (version : BookmarkVersion) = 
    null

let HgLastCommit (repoDir : DirectoryInfo) (relativeFile : string) =     
    None

let HgIgnore (repoDir : DirectoryInfo) =
    // FIXME
    ()
