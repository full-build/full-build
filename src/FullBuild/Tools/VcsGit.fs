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

module Tools.VcsGit

open System.IO
open Graph
open Exec

let private checkErrorCode execResult =
    if execResult.ResultCode <> 0 then failwithf "Process failed with error %d" execResult.ResultCode

let private noBuffering code out err =
    ()

let private checkedExec onEnd info =
    let ycheck execResult =
        onEnd execResult.ResultCode execResult.Out execResult.Error
        execResult |> checkErrorCode
    fun x y z s -> Exec x y z s info |> ycheck

let private checkedExecReadLine info =
    fun x y z s ->
        let res = ExecGetOutput x y z s info
        res |> checkErrorCode
        res.Out @ res.Error

let GitPull (repoDir : DirectoryInfo) (rebase : bool) =
    let dorebase = if rebase then "--rebase" else "--ff-only"
    let args = sprintf "pull %s" dorebase
    ExecGetOutput "git" args repoDir Map.empty

let GitTip (repoDir : DirectoryInfo) =
    let args = @"log -1 --format=%H"
    checkedExecReadLine "dummy" "git" args repoDir Map.empty

let GitClean (repoDir : DirectoryInfo) (repo : Repository) =
    let resetArgs = sprintf "reset --hard origin/%s" repo.Branch
    let info = sprintf "checkout %s" repo.Branch
    checkedExec noBuffering info "git" repo.Name repoDir Map.empty
    checkedExec noBuffering info "git" resetArgs repoDir Map.empty
    checkedExec noBuffering info "git" "clean -fxd" repoDir Map.empty

let GitIs (repo : Repository) =
    try
        let currDir = IoHelpers.CurrentFolder()
        let args = sprintf @"ls-remote -h %s" repo.Uri
        checkedExecReadLine "dummy" "git" args currDir Map.empty |> ignore
        true
    with
        _ -> false

let GitClone (repo : Repository) (target : DirectoryInfo) (url : string) (shallow : bool) =
    let bronly = sprintf "--branch %s --no-single-branch" repo.Branch
    let depth = if shallow then "--depth=3"
                else ""

    let args = sprintf @"clone %s --quiet %s %s %A" url bronly depth target.FullName

    let currDir = IoHelpers.CurrentFolder ()
    ExecGetOutput "git" args currDir Map.empty repo.Name

let GerritClone (repo : Repository) (target : DirectoryInfo) (url : string) (shallow : bool) =
    let res = GitClone repo target url shallow

    let installDir = Env.GetFolder Env.Folder.Installation
    let commitMsgFile = installDir |> IoHelpers.GetFile "commit-msg"
    let target = target |> IoHelpers.GetSubDirectory ".git"
                        |> IoHelpers.GetSubDirectory "hooks"
                        |> IoHelpers.GetFile "commit-msg"
    commitMsgFile.CopyTo (target.FullName) |> ignore
    res

let GitCheckout (repoDir : DirectoryInfo) (version : string) =
    let args = sprintf "checkout %A" version
    ExecGetOutput "git" args repoDir Map.empty

let GitHistory (repoDir : DirectoryInfo) (version : string) =
    let args = sprintf @"log --format=""%%H %%ae %%s"" %s..HEAD" version
    try
        checkedExecReadLine "dummy" "git" args repoDir Map.empty
    with
        _ -> [sprintf "Failed to get history from version %A - please pull !" version]

let GitLastCommit (repoDir : DirectoryInfo) (relativeFile : string) =
    let args = sprintf @"log -1 --format=%%H %s" relativeFile
    checkedExecReadLine "dummy" "git" args repoDir Map.empty

let GitLogs (repoDir : DirectoryInfo) =
    let args = sprintf @"log --format=%%H"
    checkedExecReadLine "dummy" "git" args repoDir Map.empty

let GitIgnore (repoDir : DirectoryInfo) =
    let dstGitIgnore = repoDir |> IoHelpers.GetFile ".gitignore"

    let installDir = Env.GetFolder Env.Folder.Installation
    let srcGitIgnore = installDir |> IoHelpers.GetFile "gitignore"
    srcGitIgnore.CopyTo(dstGitIgnore.FullName) |> ignore

let GitFindLatestMatchingTag (repoDir : DirectoryInfo) (filter : string) : string option =
    let args = sprintf "describe --match %A" filter
    let res = ExecGetOutput "git" args repoDir Map.empty "dummy"
    if res.Out.Length = 1 then 
        let res = Some (res.Out.[0].Split('-').[0])
        res
    else None

let GitTagToHash (repoDir : DirectoryInfo) (tag : string) : string =
    let args = sprintf @"rev-list --format=""%%H %%s"" -n 1 %s" tag
    let res = checkedExecReadLine "dummy" "git" args repoDir Map.empty
    let items = res.[0].Split(' ')
    items.[0]

let GitHead (repoDir : DirectoryInfo) () =
    "HEAD"

let GitTag (repoDir : DirectoryInfo) (tag : string) =
    let comment = "fullbuild"
    let argsTag = sprintf @"tag -a %s -m %A" tag comment
    checkedExec noBuffering "dummy" "git" argsTag repoDir Map.empty

    let argsPush = sprintf @"push origin %s" tag
    checkedExec noBuffering "dummy" "git" argsPush repoDir Map.empty
