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


let GitPull (repoDir : DirectoryInfo) (rebase : bool) =
    let dorebase = if rebase then "--rebase" else "--ff-only"
    let args = sprintf "pull %s" dorebase
    ExecGetOutput "git" args repoDir Map.empty

let GitTip (repoDir : DirectoryInfo) =
    let args = @"log -1 --format=%H"
    ExecGetOutput "git" args repoDir Map.empty |> GetOutput |> Seq.head

let GitClean (repoDir : DirectoryInfo) (repo : Repository) =
    let cleanArgs = "clean -fxd"
    Exec "git" cleanArgs repoDir Map.empty |> CheckResponseCode

    let checkoutArgs = sprintf "checkout %s" repo.Branch
    Exec "git" checkoutArgs repoDir Map.empty |> CheckResponseCode

    let resetArgs = sprintf "reset --hard origin/%s" repo.Branch
    Exec "git" "checkout" repoDir Map.empty |> CheckResponseCode

let GitIs (repo : Repository) =
    try
        let currDir = IoHelpers.CurrentFolder()
        let args = sprintf @"ls-remote -h %s" repo.Uri
        Exec "git" args currDir Map.empty |> CheckResponseCode
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

let GitCheckout (repoDir : DirectoryInfo) (version : string) =
    let args = sprintf "checkout %A" version
    Exec "git" args repoDir Map.empty

let GitHistory (repoDir : DirectoryInfo) (version : string) =
    let args = sprintf @"log --format=""%%H %%ae %%s"" %s..HEAD" version
    try
        ExecGetOutput "git" args repoDir Map.empty |> GetOutput
    with
        _ -> [sprintf "Failed to get history from version %A - please pull !" version]

let GitLastCommit (repoDir : DirectoryInfo) (relativeFile : string) =
    let args = sprintf @"log -1 --format=%%H %s" relativeFile
    ExecGetOutput "git" args repoDir Map.empty |> GetOutput |> Seq.head

let GitIgnore (repoDir : DirectoryInfo) =
    let dstGitIgnore = repoDir |> IoHelpers.GetFile ".gitignore"

    let installDir = Env.GetFolder Env.Folder.Installation
    let srcGitIgnore = installDir |> IoHelpers.GetFile "gitignore"
    srcGitIgnore.CopyTo(dstGitIgnore.FullName) |> ignore

let GitFindLatestMatchingTag (repoDir : DirectoryInfo) (filter : string) : string option =
    try
        let args = sprintf "describe --match %A" filter
        let res = ExecGetOutput "git" args repoDir Map.empty |> GetOutput |> Seq.head
        res.Split('-').[0] |> Some
    with
        _ -> None

let GitTagToHash (repoDir : DirectoryInfo) (tag : string) : string =
    let args = sprintf @"rev-list --format=""%%H %%s"" -n 1 %s" tag
    let res = ExecGetOutput "git" args repoDir Map.empty |> GetOutput |> Seq.head
    let items = res.Split(' ')
    items.[0]

let GitHead (repoDir : DirectoryInfo) () =
    "HEAD"

let GitTag (repoDir : DirectoryInfo) (tag : string) (comment : string) =
    let argsTag = sprintf @"tag -a %s -m %A" tag comment
    let res = ExecGetOutput "git" argsTag repoDir Map.empty
    if res.ResultCode <> 0 then res
    else
        let argsPush = sprintf @"push origin %s" tag
        ExecGetOutput "git" argsPush repoDir Map.empty
