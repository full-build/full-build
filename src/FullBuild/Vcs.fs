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

module Vcs

open Anthology
open System.IO
open IoHelpers

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
    checkedExec "git" "push --quiet" repoDir

let private hgPush (repoDir : DirectoryInfo) =
    checkedExec "hg" "push" repoDir
    

let private gitPull (rebase : bool) (repoDir : DirectoryInfo) =
    let dorebase = if rebase then "--rebase" else "--ff-only"
    let args = sprintf "pull %s" dorebase
    checkedExec "git" args  repoDir

let private hgPull (repoDir : DirectoryInfo) =
    checkedExec "hg" "pull -u" repoDir

let private gitTip (repoDir : DirectoryInfo) =
    let args = @"log -1 --format=%H"
    let res = checkedExecReadLine "git" args repoDir
    res

let private hgTip (repoDir : DirectoryInfo) =
    let args = @"id -i"
    let res = checkedExecReadLine "hg" args repoDir
    res


let private gitClean (repoDir : DirectoryInfo) (repo : Repository) =
    let br = match repo.Branch with
             | Some x -> x.toString
             | None -> "master"

    checkedExec "git" "reset --hard" repoDir
    checkedExec "git" "clean -fxd" repoDir
    checkedExec "git" (sprintf "checkout %s" br) repoDir

let private hgClean (repoDir : DirectoryInfo) (repo : Repository) =
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
    let bronly = match branch with
                 | None -> ""
                 | Some x -> sprintf "--branch %s" x.toString

    let depth = if shallow then "--depth=3 --single-branch"
                else ""

    let args = sprintf @"clone %s --quiet %s %s %A" url depth bronly target.FullName

    let currDir = IoHelpers.CurrentFolder ()
    checkedExec "git" args currDir

    if isGerrit then
        let installDir = Env.GetFolder Env.Installation
        let commitMsgFile = installDir |> IoHelpers.GetFile "commit-msg"
        let target = target |> IoHelpers.GetSubDirectory ".git"
                            |> IoHelpers.GetSubDirectory "hooks" 
                            |> IoHelpers.GetFile "commit-msg"
        commitMsgFile.CopyTo (target.FullName) |> ignore


let private hgClone (branch : BranchId option) (target : DirectoryInfo) (url : string) = 
    let bronly = match branch with
                 | None -> ""
                 | Some x -> sprintf "-r %s" x.toString

    let args = sprintf @"clone %s %A %A" bronly url target.FullName
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

let private gitHistory (repoDir : DirectoryInfo) (version : BookmarkVersion) =     
    let args = sprintf "%s %s..HEAD" @"log --pretty=format:""%H %ae %s""" version.toString
    let res = checkedExecReadLine "git" args repoDir
    res

let private hgHistory (repoDir : DirectoryInfo) (version : BookmarkVersion) = 
    null

let private gitLastCommit (repoDir : DirectoryInfo) (relativeFile : string) =     
    let args = sprintf "log -1 --format=%%H %s" relativeFile
    let res = checkedExecReadLine "git" args repoDir
    let ver = BookmarkVersion.from res
    Some ver
    
let private hgLastCommit (repoDir : DirectoryInfo) (relativeFile : string) =     
    None

let private gitIgnore (repoDir : DirectoryInfo) =
    let dstGitIgnore = repoDir |> IoHelpers.GetFile ".gitignore"

    let installDir = Env.GetFolder Env.Installation
    let srcGitIgnore = installDir |> IoHelpers.GetFile "gitignore"
    srcGitIgnore.CopyTo(dstGitIgnore.FullName) |> ignore

let private hgIgnore (repoDir : DirectoryInfo) =
    // FIXME
    ()

let chooseVcs (wsDir : DirectoryInfo) (vcsType : VcsType) (repo : Repository) gitFun hgFun =
    let repoDir = wsDir |> IoHelpers.GetSubDirectory repo.Name.toString
    let f = match vcsType with
            | VcsType.Git -> gitFun
            | VcsType.Gerrit -> gitFun
            | VcsType.Hg -> hgFun
    f repoDir

let VcsClone (wsDir : DirectoryInfo) (vcsType : VcsType) (shallow : bool) (repo : Repository) =
    let gitCloneFunc =  gitClone (vcsType = VcsType.Gerrit) shallow repo.Branch
    let hgCloneFunc = hgClone repo.Branch
    (chooseVcs wsDir vcsType repo gitCloneFunc hgCloneFunc) repo.Url.toString

let VcsTip (wsDir : DirectoryInfo) (vcsType : VcsType) repo = 
    chooseVcs wsDir vcsType repo gitTip hgTip

// version : None ==> master
let VcsCheckout (wsDir : DirectoryInfo) (vcsType : VcsType) repo (version : BookmarkVersion option) = 
    (chooseVcs wsDir vcsType repo gitCheckout hgCheckout) version

let VcsIgnore (wsDir : DirectoryInfo) (vcsType : VcsType) repo =
    chooseVcs wsDir vcsType repo  gitIgnore hgIgnore

let VcsPull (rebase : bool) (wsDir : DirectoryInfo) (vcsType : VcsType) repo =
    chooseVcs wsDir vcsType repo (gitPull rebase) hgPull 

let VcsCommit (wsDir : DirectoryInfo) (vcsType : VcsType) repo (comment : string) =
    (chooseVcs wsDir vcsType repo gitCommit hgCommit) comment

let VcsPush (wsDir : DirectoryInfo) (vcsType : VcsType) repo =
    (chooseVcs wsDir vcsType repo gitPush hgPush)

let VcsClean (wsDir : DirectoryInfo) (vcsType : VcsType) repo =
    (chooseVcs wsDir vcsType repo gitClean hgClean) repo

let VcsLog (wsDir : DirectoryInfo) (vcsType : VcsType) repo (version : BookmarkVersion) =
    (chooseVcs wsDir vcsType repo gitHistory hgHistory) version

let VcsLastCommit (wsDir : DirectoryInfo) (vcsType : VcsType) repo (relativeFile : string) =
    (chooseVcs wsDir vcsType repo gitLastCommit hgLastCommit) relativeFile
