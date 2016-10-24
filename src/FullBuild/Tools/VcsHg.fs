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
open Exec

let private checkErrorCode execResult =
    if execResult.ResultCode <> 0 then failwithf "Process failed with error %d" execResult.ResultCode

let private checkedExec command args dir vars =
    Exec command args dir vars |> checkErrorCode
    
let private checkedExecMaybeIgnore command args dir vars ignoreError =
    let check = if ignoreError then ignore else checkErrorCode
    Exec command args dir vars |> check

let private checkedExecReadLine command args dir vars =
    let res = Exec command args dir vars
    res

let HgCommit (repoDir : DirectoryInfo) (comment : string) =
    checkedExec "git" "add -S *" repoDir Map.empty
    let args = sprintf "commit -A -m %A" comment
    checkedExec "hg" args repoDir Map.empty

let HgPush (repoDir : DirectoryInfo) =
    checkedExec "hg" "push" repoDir Map.empty

let HgPull (repoDir : DirectoryInfo) (rebase : bool) =
    ExecGetOutput "hg" "pull -u" repoDir Map.empty

let HgTip (repoDir : DirectoryInfo) =
    let args = @"id -i"
    let res = checkedExecReadLine "hg" args repoDir Map.empty
    res.Out @ res.Error

let HgClean (repoDir : DirectoryInfo) (repo : Repository) =
    checkedExec "hg" "purge" repoDir Map.empty

let HgIs (repo : Repository) =
    try
        let currDir = IoHelpers.CurrentFolder()
        let args = sprintf @"id -i -R %A" repo.Uri
        checkedExec "hg" args currDir Map.empty 
        true
    with
        _ -> false

let HgClone (repo : Repository) (target : DirectoryInfo) (url : string) (shallow : bool) =
    let bronly = sprintf "-r %s" repo.Branch
    let args = sprintf @"clone %s %A %A" bronly url target.FullName
    let currDir = IoHelpers.CurrentFolder ()
    ExecGetOutput "hg" args currDir Map.empty

let HgCheckout (repoDir : DirectoryInfo) (version : string option) (ignoreError : bool) =
    let rev = match version with
              | Some x -> x
              | None -> "tip"

    let args = sprintf "update -r %A" rev
    checkedExecMaybeIgnore "hg" args repoDir Map.empty ignoreError

let HgHistory (repoDir : DirectoryInfo) (version : string) =
    []

let HgLastCommit (repoDir : DirectoryInfo) (relativeFile : string) =
    []

let HgIgnore (repoDir : DirectoryInfo) =
    // FIXME
    ()
