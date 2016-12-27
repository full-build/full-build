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
open VcsGit

let chooseVcs (wsDir : DirectoryInfo) (vcsType : VcsType) (repo : Repository) gitFun =
    let repoDir = wsDir |> IoHelpers.GetSubDirectory repo.Name
    let f = match vcsType with
            | VcsType.Git -> gitFun
            | VcsType.Gerrit -> gitFun
    f repoDir


let Unclone (wsDir : DirectoryInfo) (repo : Repository) =
    let repoDir = wsDir |> IoHelpers.GetSubDirectory repo.Name
    if repoDir.Exists then repoDir.Delete(true)

let Clone (wsDir : DirectoryInfo) (repo : Repository) (shallow : bool) =
    let gitCloneFunc = if repo.Vcs = VcsType.Gerrit then GerritClone repo
                                                    else GitClone repo
    (chooseVcs wsDir repo.Vcs repo gitCloneFunc) repo.Uri shallow

let Tip (wsDir : DirectoryInfo) (repo : Repository) =
    (chooseVcs wsDir repo.Vcs repo GitTip).[0]

// version : None ==> master
let Checkout (wsDir : DirectoryInfo) (repo : Repository) (version : string) =
    (chooseVcs wsDir repo.Vcs repo GitCheckout) version

let Ignore (wsDir : DirectoryInfo) (repo : Repository) =
    chooseVcs wsDir repo.Vcs repo  GitIgnore

let Pull (wsDir : DirectoryInfo) (repo : Repository) (rebase : bool) =
    (chooseVcs wsDir repo.Vcs repo GitPull) rebase

let Clean (wsDir : DirectoryInfo) (repo : Repository) =
    (chooseVcs wsDir repo.Vcs repo GitClean) repo

// only used in Baselines
let Log (wsDir : DirectoryInfo) (repo : Repository) (version : string) =
    (chooseVcs wsDir repo.Vcs repo GitHistory) version

let Logs (wsDir : DirectoryInfo) (repo : Repository) =
    (chooseVcs wsDir repo.Vcs repo GitLogs)

let LastCommit (wsDir : DirectoryInfo) (repo : Repository) (relativeFile : string) =
    (chooseVcs wsDir repo.Vcs repo GitLastCommit) relativeFile

let FindLatestMatchingTag (wsDir : DirectoryInfo) (repo : Repository) (filter : string) : string option =
    (chooseVcs wsDir repo.Vcs repo GitFindLatestMatchingTag) filter

let TagToHash (wsDir : DirectoryInfo) (repo : Repository) (tag : string) : string =
    (chooseVcs wsDir repo.Vcs repo GitTagToHash) tag

let Head (wsDir : DirectoryInfo) (repo : Repository) : string =
    (chooseVcs wsDir repo.Vcs repo GitHead) ()
   
let Tag (wsDir : DirectoryInfo) (repo : Repository) (tag : string): unit =
    (chooseVcs wsDir repo.Vcs repo GitTag) tag
