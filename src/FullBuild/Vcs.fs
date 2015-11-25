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
module Vcs

open System
open Exec
open IoHelpers
open Anthology
open System.IO


let private gitCommit (repoDir : DirectoryInfo) (comment : string) =
    Exec "git" "add --all" repoDir
    let args = sprintf "commit -m %A" comment
    Exec "git" args repoDir

let private hgCommit (repoDir : DirectoryInfo) (comment : string) =
    Exec "git" "add -S *" repoDir
    let args = sprintf "commit -A -m %A" comment
    Exec "hg" args repoDir


let private gitPush (repoDir : DirectoryInfo) =
    Exec "git" "push" repoDir

let private hgPush (repoDir : DirectoryInfo) =
    Exec "hg" "push" repoDir
    

let private gitPull (repoDir : DirectoryInfo) =
    Exec "git" "pull --rebase" repoDir

let private hgPull (repoDir : DirectoryInfo) =
    Exec "hg" "pull -u" repoDir


let private gitTip (repoDir : DirectoryInfo) =
    let args = @"log -1 --format=""%H"""
    let res = ExecReadLine "git" args repoDir
    res

let private hgTip (repoDir : DirectoryInfo) =
    let args = @"id -i"
    let res = ExecReadLine "hg" args repoDir
    res


let private gitClean (repoDir : DirectoryInfo) =
    Exec "git" "reset --hard" repoDir
    Exec "git" "clean -fxd" repoDir

let private hgClean (repoDir : DirectoryInfo) =
    Exec "hg" "purge" repoDir


let private gitIs (uri : RepositoryUrl) =
    try
        let currDir = IoHelpers.CurrentFolder()
        let args = sprintf @"ls-remote -h %s" uri.toString
        ExecReadLine "git" args currDir |> ignore
        true
    with
        _ -> false

let private hgIs (uri : RepositoryUrl) =
    try
        let currDir = IoHelpers.CurrentFolder()
        let args = sprintf @"id -i -R %A" uri.toLocalOrUrl
        ExecReadLine "hg" args currDir |> ignore
        true
    with
        _ -> false




let private gitClone (isGerrit : bool) (target : DirectoryInfo) (url : string) = 
    let args = sprintf @"clone --depth=1 %A %A" url target.FullName
    let currDir = DirectoryInfo(Environment.CurrentDirectory)
    Exec "git" args currDir

    if isGerrit then
        let currDir = System.Reflection.Assembly.GetExecutingAssembly().Location |> DirectoryInfo
        let commitMsgFile = currDir |> IoHelpers.GetFile "commit-msg"
        let target = target |> IoHelpers.GetSubDirectory ".git" |> IoHelpers.GetFile "commit-msg"
        commitMsgFile.CopyTo (target.FullName) |> ignore


let private hgClone (target : DirectoryInfo) (url : string) = 
    let args = sprintf @"clone %A %A" url target.FullName
    let currDir = DirectoryInfo(Environment.CurrentDirectory)
    Exec "hg" args currDir


let private gitCheckout (repoDir : DirectoryInfo) (version : BookmarkVersion) = 
    let rev = match version with
              | BookmarkVersion x -> x
              | Master -> "master"

    let args = sprintf "checkout %A" rev
    Exec "git" args repoDir

let private hgCheckout (repoDir : DirectoryInfo) (version : BookmarkVersion) = 
    let rev = match version with
              | BookmarkVersion x -> x
              | Master -> "tip"

    let args = sprintf "update -r %A" rev
    Exec "hg" args repoDir


let private gitIgnore (repoDir : DirectoryInfo) =
    let content = ["packages"; "views"; "apps"]
    let gitIgnoreFile = repoDir |> GetFile ".gitignore"
    File.WriteAllLines (gitIgnoreFile.FullName, content)

let private hgIgnore (repoDir : DirectoryInfo) =
    // FIXME
    ()


let ApplyVcs (wsDir : DirectoryInfo) (repo : Repository) gitFun hgFun =
    let repoDir = wsDir |> GetSubDirectory repo.Name.toString
    let f = match repo.Vcs with
            | VcsType.Git -> gitFun
            | VcsType.Gerrit -> gitFun
            | VcsType.Hg -> hgFun
    f repoDir


let VcsCloneRepo (wsDir : DirectoryInfo) (repo : Repository) =
    let gitCloneFunc =  gitClone (repo.Vcs = VcsType.Gerrit)
    (ApplyVcs wsDir repo gitCloneFunc hgClone) repo.Url.toString

let VcsTip (wsDir : DirectoryInfo) (repo : Repository) = 
    ApplyVcs wsDir repo gitTip hgTip

let VcsCheckout (wsDir : DirectoryInfo) (repo : Repository) (version : BookmarkVersion) = 
    (ApplyVcs wsDir repo gitCheckout hgCheckout) version

let VcsIgnore (wsDir : DirectoryInfo) (repo : Repository) =
    ApplyVcs wsDir repo gitIgnore hgIgnore

let VcsPull (wsDir : DirectoryInfo) (repo : Repository) =
    ApplyVcs wsDir repo gitPull hgPull

let VcsCommit (wsDir : DirectoryInfo) (repo : Repository) (comment : string) =
    (ApplyVcs wsDir repo gitCommit hgCommit) comment

let VcsPush (wsDir : DirectoryInfo) (repo : Repository) =
    (ApplyVcs wsDir repo gitPush hgPush)

let VcsClean (wsDir : DirectoryInfo) (repo : Repository) =
    (ApplyVcs wsDir repo gitClean hgClean)

let VcsDetermineType (url : RepositoryUrl) =
    if gitIs url then VcsType.Git
    else if hgIs url then VcsType.Hg
    else failwithf "Failed to determine type of repository %A" url.toLocalOrUrl
