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

module Tools.VcsGit

open System.IO
open Graph

let private checkErrorCode err =
    if err <> 0 then failwithf "Process failed with error %d" err

let private checkIgnore err =
    ()

let private checkedExec =
    Exec.Exec checkErrorCode

let private checkedExecGetOutput =
    Exec.ExecGetOutput checkErrorCode

let private checkedExecReadLine =
    Exec.ExecReadLine checkErrorCode

let private checkedExecMaybeIgnore ignoreError =
    let check = if ignoreError then checkIgnore else checkErrorCode
    Exec.Exec check

let GitCommit (repoDir : DirectoryInfo) (comment : string) =
    checkedExec "git" "add --all" repoDir
    let args = sprintf "commit -m %A" comment
    checkedExec "git" args repoDir

let GitPush (repoDir : DirectoryInfo) =
    checkedExec "git" "push --quiet" repoDir

let GitPull (repoDir : DirectoryInfo) (rebase : bool) =
    let dorebase = if rebase then "--rebase" else "--ff-only"
    let args = sprintf "pull %s" dorebase
    checkedExecGetOutput "git" args  repoDir

let GitTip (repoDir : DirectoryInfo) =
    let args = @"log -1 --format=%H"
    let res = checkedExecReadLine "git" args repoDir
    res

let GitClean (repoDir : DirectoryInfo) (repo : Repository) =
    checkedExec "git" "reset --hard" repoDir
    checkedExec "git" "clean -fxd" repoDir
    checkedExec "git" (sprintf "checkout %s" repo.Branch) repoDir

let GitIs (repo : Repository) =
    try
        let currDir = IoHelpers.CurrentFolder()
        let args = sprintf @"ls-remote -h %s" repo.Uri
        checkedExecReadLine "git" args currDir |> ignore
        true
    with
        _ -> false

let GitClone (repo : Repository) (target : DirectoryInfo) (url : string) (shallow : bool) =
    let bronly = sprintf "--branch %s --no-single-branch" repo.Branch
    let depth = if shallow then "--depth=3"
                else ""

    let args = sprintf @"clone %s --quiet %s %s %A" url bronly depth target.FullName

    let currDir = IoHelpers.CurrentFolder ()
    checkedExec "git" args currDir

let GerritClone (repo : Repository) (target : DirectoryInfo) (url : string) (shallow : bool) =
    GitClone repo target url shallow

    let installDir = Env.GetFolder Env.Folder.Installation
    let commitMsgFile = installDir |> IoHelpers.GetFile "commit-msg"
    let target = target |> IoHelpers.GetSubDirectory ".git"
                        |> IoHelpers.GetSubDirectory "hooks"
                        |> IoHelpers.GetFile "commit-msg"
    commitMsgFile.CopyTo (target.FullName) |> ignore

let GitCheckout (repoDir : DirectoryInfo) (version : string option) (ignoreError : bool) =
    let rev = match version with
              | Some x -> x
              | None -> "HEAD"

    let args = sprintf "checkout %A" rev
    checkedExecMaybeIgnore ignoreError "git" args repoDir

let GitHistory (repoDir : DirectoryInfo) (version : string) =
    let args = sprintf @"log --format=""%%H %%ae %%s"" %s..HEAD" version
    try
        let res = checkedExecReadLine "git" args repoDir
        res
    with
        exn -> sprintf "Failed to get history for repository %A from version %A (%s)" repoDir.Name version (exn.ToString())

let GitLastCommit (repoDir : DirectoryInfo) (relativeFile : string) =
    let args = sprintf @"log -1 --format=%%H %s" relativeFile
    let res = checkedExecReadLine "git" args repoDir
    res

let GitIgnore (repoDir : DirectoryInfo) =
    let dstGitIgnore = repoDir |> IoHelpers.GetFile ".gitignore"

    let installDir = Env.GetFolder Env.Folder.Installation
    let srcGitIgnore = installDir |> IoHelpers.GetFile "gitignore"
    srcGitIgnore.CopyTo(dstGitIgnore.FullName) |> ignore
