module Vcs

open System.IO
open Types
open Exec


let GitCloneRepo (url : Url) (target : DirectoryInfo) =
    let args = sprintf "clone %A %A" url target.FullName
    Exec "git" args

let HgCloneRepo (url : Url) (target : DirectoryInfo) =
    let args = sprintf "clone %A %A" url target.FullName
    Exec "hg" args

let VcsCloneRepo (repo : Repository) (target : DirectoryInfo) =
    match repo with
    | (Git, _, url) -> GitCloneRepo url target
    | (Hg, _, url) -> HgCloneRepo url target
