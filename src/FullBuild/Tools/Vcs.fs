//   Copyright 2014-2017 Pierre Chalamet
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

let chooseVcs (wsDir : DirectoryInfo) (vcsType : VcsType) (repo : Repository) gitFun svnFun hgFun =
    let repoDir = wsDir |> IoHelpers.GetSubDirectory repo.Name
    let f = match vcsType with
            | VcsType.Git -> gitFun
            | VcsType.Gerrit -> gitFun
            | VcsType.Hg -> hgFun
            | VcsType.Svn -> svnFun
    f repoDir


let Unclone (wsDir : DirectoryInfo) (repo : Repository) =
    let repoDir = wsDir |> IoHelpers.GetSubDirectory repo.Name
    if repoDir.Exists then repoDir.Delete(true)

let Clone (wsDir : DirectoryInfo) (repo : Repository) (shallow : bool) : Exec.ExecResult =
    let gitCloneFunc = if repo.Vcs = VcsType.Gerrit then VcsGit.GerritClone repo
                                                    else VcsGit.GitClone repo
    let svnClone = VcsSvn.SvnClone
    let hgClone = VcsHg.HgClone
    (chooseVcs wsDir repo.Vcs repo gitCloneFunc svnClone hgClone) repo.Uri shallow

// version : None ==> master
let Checkout (wsDir : DirectoryInfo) (repo : Repository) (version : string) =
    (chooseVcs wsDir repo.Vcs repo VcsGit.GitCheckout VcsSvn.SvnCheckout VcsHg.HgCheckout) version

let Ignore (wsDir : DirectoryInfo) (repo : Repository) =
    chooseVcs wsDir repo.Vcs repo  VcsGit.GitIgnore VcsSvn.SvnIgnore VcsHg.HgIgnore

let Pull (wsDir : DirectoryInfo) (repo : Repository) (rebase : bool) : Exec.ExecResult =
    (chooseVcs wsDir repo.Vcs repo VcsGit.GitPull VcsSvn.SvnPull VcsHg.HgPull) rebase

let Clean (wsDir : DirectoryInfo) (repo : Repository) =
    (chooseVcs wsDir repo.Vcs repo VcsGit.GitClean VcsSvn.SvnClean VcsHg.HgClean) repo

// only used in Baselines
let Log (wsDir : DirectoryInfo) (repo : Repository) (version : string) =
    (chooseVcs wsDir repo.Vcs repo VcsGit.GitHistory VcsSvn.SvnHistory VcsHg.HgHistory) version

let LastCommit (wsDir : DirectoryInfo) (repo : Repository) (relativeFile : string) =
    (chooseVcs wsDir repo.Vcs repo VcsGit.GitLastCommit VcsSvn.SvnLastCommit VcsHg.HgLastCommit) relativeFile

let FindLatestMatchingTag (wsDir : DirectoryInfo) (repo : Repository) (filter : string) : string option =
    (chooseVcs wsDir repo.Vcs repo VcsGit.GitFindLatestMatchingTag VcsSvn.SvnFindLatestMatchingTag VcsHg.HgFindLatestMatchingTag) filter

let TagToHash (wsDir : DirectoryInfo) (repo : Repository) (tag : string) : string =
    (chooseVcs wsDir repo.Vcs repo VcsGit.GitTagToHash VcsSvn.SvnTagToHash VcsHg.HgTagToHash) tag

let Head (wsDir : DirectoryInfo) (repo : Repository) : string =
    (chooseVcs wsDir repo.Vcs repo VcsGit.GitHead VcsSvn.SvnHead VcsHg.HgHead) ()
   
let Tag (wsDir : DirectoryInfo) (repo : Repository) (tag : string) (comment : string) : Exec.ExecResult =
    (chooseVcs wsDir repo.Vcs repo VcsGit.GitTag VcsSvn.SvnTag VcsHg.HgTag) tag comment
