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
open VcsHg
open VcsGit


let chooseVcs (wsDir : DirectoryInfo) (vcsType : VcsType) (repo : Repository) gitFun hgFun =
    let repoDir = wsDir |> IoHelpers.GetSubDirectory repo.Name.toString
    let f = match vcsType with
            | VcsType.Git -> gitFun
            | VcsType.Gerrit -> gitFun
            | VcsType.Hg -> hgFun
    f repoDir



let Clone (vcsType : VcsType) (wsDir : DirectoryInfo) (repo : Repository) (shallow : bool) =
    let gitCloneFunc = if vcsType = VcsType.Gerrit then GerritClone repo.Branch
                                                   else GitClone repo.Branch
    let hgCloneFunc = HgClone repo.Branch
    (chooseVcs wsDir vcsType repo gitCloneFunc hgCloneFunc) repo.Url.toString shallow

let Tip (vcsType : VcsType) (wsDir : DirectoryInfo) (repo : Repository) = 
    chooseVcs wsDir vcsType repo GitTip HgTip

// version : None ==> master
let Checkout (vcsType : VcsType) (wsDir : DirectoryInfo) (repo : Repository) (version : BookmarkVersion option) (ignore : bool) = 
    (chooseVcs wsDir vcsType repo GitCheckout HgCheckout) version ignore

let Ignore (vcsType : VcsType) (wsDir : DirectoryInfo) (repo : Repository) =
    chooseVcs wsDir vcsType repo  GitIgnore HgIgnore

let Pull (vcsType : VcsType) (wsDir : DirectoryInfo) (repo : Repository) (rebase : bool) =
    (chooseVcs wsDir vcsType repo GitPull HgPull) rebase

let Commit (vcsType : VcsType) (wsDir : DirectoryInfo) (repo : Repository) (comment : string) =
    (chooseVcs wsDir vcsType repo GitCommit HgCommit) comment

let Push (vcsType : VcsType) (wsDir : DirectoryInfo) (repo : Repository) =
    (chooseVcs wsDir vcsType repo GitPush HgPush)

let Clean (vcsType : VcsType) (wsDir : DirectoryInfo) (repo : Repository) =
    (chooseVcs wsDir vcsType repo GitClean HgClean) repo

let Log (vcsType : VcsType) (wsDir : DirectoryInfo) (repo : Repository) (version : BookmarkVersion) =
    (chooseVcs wsDir vcsType repo GitHistory HgHistory) version

let LastCommit (vcsType : VcsType) (wsDir : DirectoryInfo) (repo : Repository) (relativeFile : string) =
    (chooseVcs wsDir vcsType repo GitLastCommit HgLastCommit) relativeFile
