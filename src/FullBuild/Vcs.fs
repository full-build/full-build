//   Copyright 2014-2015 Pierre Chalamet
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

module Vcs

open System
open Anthology
open System.IO

let private checkErrorCode err =
    if err <> 0 then failwithf "Process failed with error %d" err

let private checkedExec = 
    Exec.Exec checkErrorCode

let private checkedExecReadLine =
    Exec.ExecReadLine checkErrorCode

let private gitCommit (repoDir : DirectoryInfo) (comment : string) =
    checkedExec "git" "add --all" repoDir
    let args = sprintf "commit -m %A" comment
    checkedExec "git" args repoDir

let private hgCommit (repoDir : DirectoryInfo) (comment : string) =
    checkedExec "git" "add -S *" repoDir
    let args = sprintf "commit -A -m %A" comment
    checkedExec "hg" args repoDir


let private gitPush (repoDir : DirectoryInfo) =
    checkedExec "git" "push" repoDir

let private hgPush (repoDir : DirectoryInfo) =
    checkedExec "hg" "push" repoDir
    

let private gitPull (repoDir : DirectoryInfo) =
    checkedExec "git" "pull --rebase" repoDir

let private hgPull (repoDir : DirectoryInfo) =
    checkedExec "hg" "pull -u" repoDir


let private gitTip (repoDir : DirectoryInfo) =
    let args = @"log -1 --format=""%H"""
    let res = checkedExecReadLine "git" args repoDir
    res

let private hgTip (repoDir : DirectoryInfo) =
    let args = @"id -i"
    let res = checkedExecReadLine "hg" args repoDir
    res


let private gitClean (repoDir : DirectoryInfo) =
    checkedExec "git" "reset --hard" repoDir
    checkedExec "git" "clean -fxd" repoDir

let private hgClean (repoDir : DirectoryInfo) =
    checkedExec "hg" "purge" repoDir


let private gitIs (uri : RepositoryUrl) =
    try
        let currDir = IoHelpers.CurrentFolder()
        let args = sprintf @"ls-remote -h %s" uri.toString
        checkedExecReadLine "git" args currDir |> ignore
        true
    with
        _ -> false

let private hgIs (uri : RepositoryUrl) =
    try
        let currDir = IoHelpers.CurrentFolder()
        let args = sprintf @"id -i -R %A" uri.toLocalOrUrl
        checkedExecReadLine "hg" args currDir |> ignore
        true
    with
        _ -> false




let private gitClone (isGerrit : bool) (shallow : bool) (branch : BranchId option) (target : DirectoryInfo) (url : string) = 
    let depth = if shallow then "--depth=1"
                else ""

    let bronly = match branch with
                 | None -> ""
                 | Some x -> sprintf "--branch %s --single-branch" x.toString

    let args = sprintf @"clone %s --quiet %s %s %A" url depth bronly target.FullName
    printfn "%s" args

    let currDir = IoHelpers.CurrentFolder ()
    checkedExec "git" args currDir

    if isGerrit then
        let installDir = Env.GetFolder Env.Installation
        let commitMsgFile = installDir |> IoHelpers.GetFile "commit-msg"
        let target = target |> IoHelpers.GetSubDirectory ".git" |> IoHelpers.GetFile "commit-msg"
        commitMsgFile.CopyTo (target.FullName) |> ignore


let private hgClone (target : DirectoryInfo) (url : string) = 
    let args = sprintf @"clone %A %A" url target.FullName
    let currDir = IoHelpers.CurrentFolder ()
    checkedExec "hg" args currDir


let private gitCheckout (repoDir : DirectoryInfo) (version : BookmarkVersion option) = 
    let rev = match version with
              | Some (BookmarkVersion x) -> x
              | None -> "master"

    let args = sprintf "checkout %A" rev
    checkedExec "git" args repoDir

let private hgCheckout (repoDir : DirectoryInfo) (version : BookmarkVersion option) = 
    let rev = match version with
              | Some (BookmarkVersion x) -> x
              | None -> "tip"

    let args = sprintf "update -r %A" rev
    checkedExec "hg" args repoDir


let private gitIgnore (repoDir : DirectoryInfo) =
    let dstGitIgnore = repoDir |> IoHelpers.GetFile ".gitignore"

    let installDir = Env.GetFolder Env.Installation
    let srcGitIgnore = installDir |> IoHelpers.GetFile "gitignore"
    srcGitIgnore.CopyTo(dstGitIgnore.FullName) |> ignore

let private hgIgnore (repoDir : DirectoryInfo) =
    // FIXME
    ()

let chooseVcs (wsDir : DirectoryInfo) (repo : Repository) gitFun hgFun =
    let repoDir = wsDir |> IoHelpers.GetSubDirectory repo.Name.toString
    let f = match repo.Vcs with
            | VcsType.Git -> gitFun
            | VcsType.Gerrit -> gitFun
            | VcsType.Hg -> hgFun
    f repoDir

let VcsClone (wsDir : DirectoryInfo) (shallow : bool) (repo : Repository) =
    let gitCloneFunc =  gitClone (repo.Vcs = VcsType.Gerrit) shallow repo.Branch
    let hgCloneFunc = hgClone
    (chooseVcs wsDir repo gitCloneFunc hgCloneFunc) repo.Url.toString

let VcsTip (wsDir : DirectoryInfo) (repo : Repository) = 
    chooseVcs wsDir repo gitTip hgTip

// version : None ==> master
let VcsCheckout (wsDir : DirectoryInfo) (repo : Repository) (version : BookmarkVersion option) = 
    (chooseVcs wsDir repo gitCheckout hgCheckout) version

let VcsIgnore (wsDir : DirectoryInfo) (repo : Repository) =
    chooseVcs wsDir repo  gitIgnore hgIgnore

let VcsPull (wsDir : DirectoryInfo) (repo : Repository) =
    chooseVcs wsDir repo  gitPull hgPull

let VcsCommit (wsDir : DirectoryInfo) (repo : Repository) (comment : string) =
    (chooseVcs wsDir repo gitCommit hgCommit) comment

let VcsPush (wsDir : DirectoryInfo) (repo : Repository) =
    (chooseVcs wsDir repo gitPush hgPush)

let VcsClean (wsDir : DirectoryInfo) (repo : Repository) =
    (chooseVcs wsDir repo gitClean hgClean)

let VcsDetermineType (url : RepositoryUrl) =
    if gitIs url then VcsType.Git
    else if hgIs url then VcsType.Hg
    else failwithf "Failed to determine type of repository %A" url.toLocalOrUrl