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
open Exec

let private checkErrorCode execResult =
    if execResult.ResultCode <> 0 then failwithf "Process failed with error %d" execResult.ResultCode

let private noBuffering code out err =
    ()

let private checkedExec onEnd =
    let ycheck execResult =
        onEnd execResult.ResultCode execResult.Out execResult.Error
        execResult |> checkErrorCode
    fun x y z s -> Exec x y z s  |> ycheck

let private checkedExecMaybeIgnore ignoreError =
    let check = if ignoreError then ignore else checkErrorCode
    fun x y z s -> Exec x y z s  |> check

let private checkedExecReadLine =
    fun x y z s ->
        let res = ExecGetOutput x y z s
        res |> checkErrorCode
        res.Out @ res.Error

let GitCommit (repoDir : DirectoryInfo) (comment : string) =
    checkedExec noBuffering "git" "add --all" repoDir Map.empty
    let args = sprintf "commit -m %A" comment
    checkedExec noBuffering "git" args repoDir Map.empty

let GitPush (repoDir : DirectoryInfo) =
    checkedExec noBuffering "git" "push --quiet" repoDir Map.empty

let GitPull (repoDir : DirectoryInfo) (rebase : bool) =
    let dorebase = if rebase then "--rebase" else "--ff-only"
    let args = sprintf "pull %s" dorebase
    ExecGetOutput "git" args repoDir Map.empty

let GitTip (repoDir : DirectoryInfo) =
    let args = @"log -1 --format=%H"
    checkedExecReadLine "git" args repoDir Map.empty

let GitClean (repoDir : DirectoryInfo) (repo : Repository) =
    checkedExec noBuffering "git" (sprintf "checkout %s" repo.Branch) repoDir Map.empty
    let resetArgs = sprintf "reset --hard origin/%s" repo.Branch
    checkedExec noBuffering "git" resetArgs repoDir Map.empty
    checkedExec noBuffering "git" "clean -fxd" repoDir Map.empty

let GitIs (repo : Repository) =
    try
        let currDir = IoHelpers.CurrentFolder()
        let args = sprintf @"ls-remote -h %s" repo.Uri
        checkedExecReadLine "git" args currDir Map.empty |> ignore
        true
    with
        _ -> false

let GitClone (repo : Repository) (target : DirectoryInfo) (url : string) (shallow : bool) =
    let bronly = sprintf "--branch %s --no-single-branch" repo.Branch
    let depth = if shallow then "--depth=3"
                else ""

    let args = sprintf @"clone %s --quiet %s %s %A" url bronly depth target.FullName

    let currDir = IoHelpers.CurrentFolder ()
    ExecGetOutput "git" args currDir Map.empty

let GerritClone (repo : Repository) (target : DirectoryInfo) (url : string) (shallow : bool) =
    let res = GitClone repo target url shallow

    let installDir = Env.GetFolder Env.Folder.Installation
    let commitMsgFile = installDir |> IoHelpers.GetFile "commit-msg"
    let target = target |> IoHelpers.GetSubDirectory ".git"
                        |> IoHelpers.GetSubDirectory "hooks"
                        |> IoHelpers.GetFile "commit-msg"
    commitMsgFile.CopyTo (target.FullName) |> ignore
    res

let GitCheckout (repoDir : DirectoryInfo) (version : string option) (ignoreError : bool) =
    let rev = match version with
              | Some x -> x
              | None -> "HEAD"

    let args = sprintf "checkout %A" rev
    checkedExecMaybeIgnore ignoreError "git" args repoDir Map.empty

let GitHistory (repoDir : DirectoryInfo) (version : string) =
    let args = sprintf @"log --format=""%%H %%ae %%s"" %s..HEAD" version
    try
        checkedExecReadLine "git" args repoDir Map.empty
    with
        _ -> [sprintf "Failed to get history from version %A - please pull !" version]

let GitLastCommit (repoDir : DirectoryInfo) (relativeFile : string) =
    let args = sprintf @"log -1 --format=%%H %s" relativeFile
    checkedExecReadLine "git" args repoDir Map.empty

let GitIgnore (repoDir : DirectoryInfo) =
    let dstGitIgnore = repoDir |> IoHelpers.GetFile ".gitignore"

    let installDir = Env.GetFolder Env.Folder.Installation
    let srcGitIgnore = installDir |> IoHelpers.GetFile "gitignore"
    srcGitIgnore.CopyTo(dstGitIgnore.FullName) |> ignore
