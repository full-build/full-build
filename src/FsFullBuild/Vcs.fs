module Vcs

open System
open System.IO
open Types
open Exec
open WellknownFolders
open FileExtensions


let GitCloneRepo (url : string) (target : DirectoryInfo) =
    let args = sprintf "clone %A %A" url target.FullName
    Exec "git" args Environment.CurrentDirectory

let HgCloneRepo (url : string) (target : DirectoryInfo) =
    let args = sprintf "clone %A %A" url target.FullName
    Exec "hg" args Environment.CurrentDirectory

let VcsCloneRepo (wsDir : DirectoryInfo) (repo : Repository) =
    let checkoutDir = wsDir |> GetSubDirectory repo.Name 
    let cloneRepo = match repo.Vcs with
                    | Git -> GitCloneRepo
                    | Hg -> HgCloneRepo
    cloneRepo repo.Url checkoutDir
