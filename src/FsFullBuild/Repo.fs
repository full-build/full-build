﻿module Repo

open Types
open Configuration
open System.Text.RegularExpressions
open Vcs
open WellknownFolders

let rec List2 (repos : Repository list) =
    match repos with
    | repo::tail -> printfn "%s : %s [%A]" repo.Name repo.Url repo.Vcs
                    List2 tail
    | [] -> ()

let List () =
    let wsConfig = WorkspaceConfig ()
    List2 wsConfig.Repositories

let MatchRepo (repo : Repository seq) (filter : string) =
    let matchRegex = "^" + filter + "$"
    let regex = new Regex(matchRegex, RegexOptions.IgnoreCase)
    repo |> Seq.filter ( fun x -> regex.IsMatch(x.Name)) |> Seq.distinct

let Clone (filters : string list) =
    let wsDir = WorkspaceFolder ()
    let wsConfig = WorkspaceConfig ()
    let res = filters |> Seq.map (MatchRepo wsConfig.Repositories) |> Seq.collect (fun x -> x) |> Seq.distinct
    res |> Seq.iter (Vcs.VcsCloneRepo wsDir)

