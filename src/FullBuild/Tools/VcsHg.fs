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

module Tools.VcsHg

open System.IO
open Graph

let private checkErrorCode code out err =
    if code <> 0 then failwithf "Process failed with error %d" code

let private checkIgnore code out err =
    ()

let private checkedExec =
    Exec.Exec checkErrorCode
    
let private checkedExecMaybeIgnore ignoreError =
    let check = if ignoreError then checkIgnore else checkErrorCode
    Exec.Exec check

let private checkedExecReadLine =
    Exec.ExecSingleLine checkErrorCode

let HgCommit (repoDir : DirectoryInfo) (comment : string) =
    checkedExec "git" "add -S *" repoDir Map.empty
    let args = sprintf "commit -A -m %A" comment
    checkedExec "hg" args repoDir Map.empty

let HgPush (repoDir : DirectoryInfo) =
    checkedExec "hg" "push" repoDir Map.empty

let HgPull (repoDir : DirectoryInfo) (rebase : bool) =
    checkedExec "hg" "pull -u" repoDir Map.empty

let HgTip (repoDir : DirectoryInfo) =
    let args = @"id -i"
    let res = checkedExecReadLine "hg" args repoDir Map.empty
    res

let HgClean (repoDir : DirectoryInfo) (repo : Repository) =
    checkedExec "hg" "purge" repoDir Map.empty

let HgIs (repo : Repository) =
    try
        let currDir = IoHelpers.CurrentFolder()
        let args = sprintf @"id -i -R %A" repo.Uri
        checkedExecReadLine "hg" args currDir Map.empty |> ignore
        true
    with
        _ -> false

let HgClone (repo : Repository) (target : DirectoryInfo) (url : string) (shallow : bool) =
    let bronly = sprintf "-r %s" repo.Branch
    let args = sprintf @"clone %s %A %A" bronly url target.FullName
    let currDir = IoHelpers.CurrentFolder ()
    checkedExec "hg" args currDir Map.empty

let HgCheckout (repoDir : DirectoryInfo) (version : string option) (ignoreError : bool) =
    let rev = match version with
              | Some x -> x
              | None -> "tip"

    let args = sprintf "update -r %A" rev
    checkedExecMaybeIgnore ignoreError "hg" args repoDir Map.empty

let HgHistory (repoDir : DirectoryInfo) (version : string) =
    null

let HgLastCommit (repoDir : DirectoryInfo) (relativeFile : string) =
    ""

let HgIgnore (repoDir : DirectoryInfo) =
    // FIXME
    ()
