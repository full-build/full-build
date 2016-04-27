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

module VcsGit

open Anthology
open System.IO

let private checkErrorCode err =
    if err <> 0 then failwithf "Process failed with error %d" err

let private checkedExec = 
    Exec.Exec checkErrorCode

let private checkedExecReadLine =
    Exec.ExecReadLine checkErrorCode

let GitCommit (repoDir : DirectoryInfo) (comment : string) =
    checkedExec "git" "add --all" repoDir
    let args = sprintf "commit -m %A" comment
    checkedExec "git" args repoDir

let GitPush (repoDir : DirectoryInfo) =
    checkedExec "git" "push --quiet" repoDir

let GitPull (rebase : bool) (repoDir : DirectoryInfo) =
    let dorebase = if rebase then "--rebase" else "--ff-only"
    let args = sprintf "pull %s" dorebase
    checkedExec "git" args  repoDir

let GitTip (repoDir : DirectoryInfo) =
    let args = @"log -1 --format=%H"
    let res = checkedExecReadLine "git" args repoDir
    res

let GitClean (repoDir : DirectoryInfo) (repo : Repository) =
    let br = match repo.Branch with
             | Some x -> x.toString
             | None -> "master"

    checkedExec "git" "reset --hard" repoDir
    checkedExec "git" "clean -fxd" repoDir
    checkedExec "git" (sprintf "checkout %s" br) repoDir

let GitIs (uri : RepositoryUrl) =
    try
        let currDir = IoHelpers.CurrentFolder()
        let args = sprintf @"ls-remote -h %s" uri.toString
        checkedExecReadLine "git" args currDir |> ignore
        true
    with
        _ -> false

let GitClone (shallow : bool) (branch : BranchId option) (target : DirectoryInfo) (url : string) = 
    let bronly = match branch with
                 | None -> "--no-single-branch"
                 | Some x -> sprintf "--branch %s --single-branch" x.toString

    let depth = if shallow then "--depth=3"
                else ""

    let args = sprintf @"clone %s --quiet %s %s %A" url bronly depth target.FullName

    let currDir = IoHelpers.CurrentFolder ()
    checkedExec "git" args currDir

let GerritClone (shallow : bool) (branch : BranchId option) (target : DirectoryInfo) (url : string) = 
    let bronly = match branch with
                 | None -> "--no-single-branch"
                 | Some x -> sprintf "--branch %s --single-branch" x.toString

    let depth = if shallow then "--depth=3"
                else ""

    let args = sprintf @"clone %s --quiet %s %s %A" url bronly depth target.FullName

    let currDir = IoHelpers.CurrentFolder ()
    checkedExec "git" args currDir

    let installDir = Env.GetFolder Env.Installation
    let commitMsgFile = installDir |> IoHelpers.GetFile "commit-msg"
    let target = target |> IoHelpers.GetSubDirectory ".git"
                        |> IoHelpers.GetSubDirectory "hooks" 
                        |> IoHelpers.GetFile "commit-msg"
    commitMsgFile.CopyTo (target.FullName) |> ignore




let GitCheckout (repoDir : DirectoryInfo) (version : BookmarkVersion option) = 
    let rev = match version with
              | Some (BookmarkVersion x) -> x
              | None -> "master"

    let args = sprintf "checkout %A" rev
    checkedExec "git" args repoDir

let GitHistory (repoDir : DirectoryInfo) (version : BookmarkVersion) =     
    let args = sprintf @"log --format=""%%H %%ae %%s"" %s..HEAD" version.toString
    let res = checkedExecReadLine "git" args repoDir
    res

let GitLastCommit (repoDir : DirectoryInfo) (relativeFile : string) =     
    let args = sprintf @"log -1 --format=%%H %s" relativeFile
    let res = checkedExecReadLine "git" args repoDir
    let ver = BookmarkVersion.from res
    Some ver

let GitIgnore (repoDir : DirectoryInfo) =
    let dstGitIgnore = repoDir |> IoHelpers.GetFile ".gitignore"

    let installDir = Env.GetFolder Env.Installation
    let srcGitIgnore = installDir |> IoHelpers.GetFile "gitignore"
    srcGitIgnore.CopyTo(dstGitIgnore.FullName) |> ignore
