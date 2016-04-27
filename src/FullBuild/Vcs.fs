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

let VcsClone (wsDir : DirectoryInfo) (vcsType : VcsType) (shallow : bool) (repo : Repository) =
    let gitCloneFunc =  GitClone (vcsType = VcsType.Gerrit) shallow repo.Branch
    let hgCloneFunc = HgClone repo.Branch
    (chooseVcs wsDir vcsType repo gitCloneFunc hgCloneFunc) repo.Url.toString

let VcsTip (wsDir : DirectoryInfo) (vcsType : VcsType) repo = 
    chooseVcs wsDir vcsType repo GitTip HgTip

// version : None ==> master
let VcsCheckout (wsDir : DirectoryInfo) (vcsType : VcsType) repo (version : BookmarkVersion option) = 
    (chooseVcs wsDir vcsType repo GitCheckout HgCheckout) version

let VcsIgnore (wsDir : DirectoryInfo) (vcsType : VcsType) repo =
    chooseVcs wsDir vcsType repo  GitIgnore HgIgnore

let VcsPull (rebase : bool) (wsDir : DirectoryInfo) (vcsType : VcsType) repo =
    chooseVcs wsDir vcsType repo (GitPull rebase) HgPull 

let VcsCommit (wsDir : DirectoryInfo) (vcsType : VcsType) repo (comment : string) =
    (chooseVcs wsDir vcsType repo GitCommit HgCommit) comment

let VcsPush (wsDir : DirectoryInfo) (vcsType : VcsType) repo =
    (chooseVcs wsDir vcsType repo GitPush HgPush)

let VcsClean (wsDir : DirectoryInfo) (vcsType : VcsType) repo =
    (chooseVcs wsDir vcsType repo GitClean HgClean) repo

let VcsLog (wsDir : DirectoryInfo) (vcsType : VcsType) repo (version : BookmarkVersion) =
    (chooseVcs wsDir vcsType repo GitHistory HgHistory) version

let VcsLastCommit (wsDir : DirectoryInfo) (vcsType : VcsType) repo (relativeFile : string) =
    (chooseVcs wsDir vcsType repo GitLastCommit HgLastCommit) relativeFile
