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

module Tools.Vcs

open Graph
open System.IO
open VcsHg
open VcsGit


let chooseVcs (wsDir : DirectoryInfo) (vcsType : VcsType) (repo : Repository) gitFun hgFun =
    let repoDir = wsDir |> IoHelpers.GetSubDirectory repo.Name
    let f = match vcsType with
            | VcsType.Git -> gitFun
            | VcsType.Gerrit -> gitFun
            | VcsType.Hg -> hgFun
    f repoDir


let Unclone (wsDir : DirectoryInfo) (repo : Repository) = 
    let repoDir = wsDir |> IoHelpers.GetSubDirectory repo.Name
    if repoDir.Exists then repoDir.Delete(true)

let Clone (wsDir : DirectoryInfo) (repo : Repository) (shallow : bool) onEnd =
    let gitCloneFunc = if repo.Vcs = VcsType.Gerrit then GerritClone repo
                                                    else GitClone repo
    let hgCloneFunc = HgClone repo
    (chooseVcs wsDir repo.Vcs repo gitCloneFunc hgCloneFunc) repo.Uri shallow onEnd

let Tip (wsDir : DirectoryInfo) (repo : Repository) =
    (chooseVcs wsDir repo.Vcs repo GitTip HgTip).[0]

// version : None ==> master
let Checkout (wsDir : DirectoryInfo) (repo : Repository) (version : string option) (ignore : bool) =
    (chooseVcs wsDir repo.Vcs repo GitCheckout HgCheckout) version ignore

let Ignore (wsDir : DirectoryInfo) (repo : Repository) =
    chooseVcs wsDir repo.Vcs repo  GitIgnore HgIgnore

let Pull (wsDir : DirectoryInfo) (repo : Repository) (rebase : bool) onEnd =
    (chooseVcs wsDir repo.Vcs repo GitPull HgPull) rebase onEnd

let Commit (wsDir : DirectoryInfo) (repo : Repository) (comment : string) =
    (chooseVcs wsDir repo.Vcs repo GitCommit HgCommit) comment

let Push (wsDir : DirectoryInfo) (repo : Repository) =
    (chooseVcs wsDir repo.Vcs repo GitPush HgPush)

let Clean (wsDir : DirectoryInfo) (repo : Repository) =
    (chooseVcs wsDir repo.Vcs repo GitClean HgClean) repo

let Log (wsDir : DirectoryInfo) (repo : Repository) (version : string) =
    (chooseVcs wsDir repo.Vcs repo GitHistory HgHistory) version

let LastCommit (wsDir : DirectoryInfo) (repo : Repository) (relativeFile : string) =
    (chooseVcs wsDir repo.Vcs repo GitLastCommit HgLastCommit) relativeFile
