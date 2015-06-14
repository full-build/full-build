module Vcs

open System
open System.IO
open Types
open Exec
open WellknownFolders
open FileExtensions


let GitCloneRepo (url : Url) (target : DirectoryInfo) =
    let args = sprintf "clone %A %A" url target.FullName
    Exec "git" args Environment.CurrentDirectory

let HgCloneRepo (url : Url) (target : DirectoryInfo) =
    let args = sprintf "clone %A %A" url target.FullName
    Exec "hg" args Environment.CurrentDirectory

let VcsCloneRepo (wsDir : DirectoryInfo) (repo : Repository) =
    let (vcs, name, url) = repo
    let checkoutDir = wsDir |> GetSubDirectory name 
    match vcs with
    | Git -> GitCloneRepo url checkoutDir
    | Hg -> HgCloneRepo url checkoutDir
