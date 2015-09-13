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


let private GitClean (repoDir : DirectoryInfo) =
    Exec "git" "clean -fxd" repoDir

let private HgClean (repoDir : DirectoryInfo) =
    Exec "hg" "purge" repoDir

let private GitPull (repoDir : DirectoryInfo) =
    Exec "git" "pull --rebase" repoDir

let private HgPull (repoDir : DirectoryInfo) =
    Exec "hg" "pull -u" repoDir


let private GitTip (repoDir : DirectoryInfo) =
    let args = @"log -1 --format=""%H"""
    let res = ExecReadLine "git" args repoDir
    res

let private HgTip (repoDir : DirectoryInfo) =
    let args = @"id -i"
    let res = ExecReadLine "hg" args repoDir
    res




let private GitClone (url : string) (target : DirectoryInfo) = 
    let args = sprintf "clone %A %A" url target.FullName
    let currDir = DirectoryInfo(Environment.CurrentDirectory)
    Exec "git" args currDir

let private HgClone (url : string) (target : DirectoryInfo) = 
    let args = sprintf "clone %A %A" url target.FullName
    let currDir = DirectoryInfo(Environment.CurrentDirectory)
    Exec "hg" args currDir    

let private GitCheckout (repoDir : DirectoryInfo) (version : BookmarkVersion) = 
    let rev = match version with
              | BookmarkVersion x -> x
              | Master -> "master"

    let args = sprintf "checkout %A" rev
    Exec "git" args repoDir

let private HgCheckout (repoDir : DirectoryInfo) (version : BookmarkVersion) = 
    let rev = match version with
              | BookmarkVersion x -> x
              | Master -> "tip"

    let args = sprintf "update -r %A" rev
    Exec "hg" args repoDir

let private GitIgnore (repoDir : DirectoryInfo) =
    let content = seq {
        yield "packages"
        yield "views"
    }
    let gitIgnoreFile = repoDir |> GetFile ".gitignore"
    File.WriteAllLines (gitIgnoreFile.FullName, content)

let private HgIgnore (repoDir : DirectoryInfo) =
    ()


let VcsCloneRepo (wsDir : DirectoryInfo) (repo : Repository) = 
    let repoDir = wsDir |> GetSubDirectory repo.Name.toString
    
    let cloneRepo = 
        match repo.Vcs with
        | VcsType.Git -> GitClone
        | VcsType.Hg -> HgClone
    cloneRepo repo.Url.toString repoDir

let VcsTip (wsDir : DirectoryInfo) (repo : Repository) = 
    let repoDir = wsDir |> GetSubDirectory repo.Name.toString
    
    let tipRepo = 
        match repo.Vcs with
        | VcsType.Git -> GitTip
        | VcsType.Hg -> HgTip
    let tip = tipRepo repoDir
    tip

let VcsCheckout (wsDir : DirectoryInfo) (repo : Repository) (version : BookmarkVersion) = 
    let repoDir = wsDir |> GetSubDirectory repo.Name.toString

    let checkoutRepo = 
        match repo.Vcs with
        | VcsType.Git -> GitCheckout
        | VcsType.Hg -> HgCheckout
    checkoutRepo repoDir version

let VcsIgnore (wsDir : DirectoryInfo) (repo : Repository) =
    let repoDir = wsDir |> GetSubDirectory repo.Name.toString

    let ignoreRepo = 
        match repo.Vcs with
        | VcsType.Git -> GitIgnore
        | VcsType.Hg -> HgIgnore
    ignoreRepo repoDir

let VcsClean (wsDir : DirectoryInfo) (repo : Repository) =
    let repoDir = wsDir |> GetSubDirectory repo.Name.toString

    let cleanRepo = 
        match repo.Vcs with
        | VcsType.Git -> GitClean
        | VcsType.Hg -> HgClean
    cleanRepo repoDir

let VcsPull (wsDir : DirectoryInfo) (repo : Repository) =
    let repoDir = wsDir |> GetSubDirectory repo.Name.toString

    let pullRepo = 
        match repo.Vcs with
        | VcsType.Git -> GitPull
        | VcsType.Hg -> HgPull
    pullRepo repoDir
