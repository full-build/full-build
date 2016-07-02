﻿//   Copyright 2014-2016 Pierre Chalamet
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

module Repo

open Anthology
open PatternMatching
open Configuration
open Collections
open IoHelpers

let List() = 
    let antho = LoadAnthology()
    antho.Repositories |> Seq.iter (fun x -> printfn "%s : %s [%A]" x.Repository.Name.toString x.Repository.Url.toString x.Builder)

let MatchRepo (repo : BuildableRepository set) (filter : RepositoryId) = 
    repo |> Set.filter (fun x -> Match x.Repository.Name.toString filter.toString)

let FilterRepos (filters : RepositoryId set) = 
    let antho = LoadAnthology()
    filters |> Seq.map (MatchRepo antho.Repositories)
            |> Seq.concat
            |> Set

let cloneRepoAndInit wsDir vcs shallow (repo : Repository) =
    async {
        DisplayHighlight repo.Name.toString
        Vcs.VcsClone wsDir vcs shallow repo
    }

let Clone (filters : RepositoryId set) (shallow : bool) (all : bool) (mt : bool) = 
    let antho = LoadAnthology()
    let wsDir = Env.GetFolder Env.Workspace

    let selectedRepos = FilterRepos filters |> Set.map (fun x -> x.Repository.Name)
    let cloneReposId = match all with
                       | true -> AnthologyGraph.ComputeRepositoriesDependencies antho.Projects selectedRepos
                       | _ -> selectedRepos
    let cloneRepos = antho.Repositories |> Set.filter (fun x -> cloneReposId |> Set.contains x.Repository.Name)

    let maxThrottle = if mt then (System.Environment.ProcessorCount*2) else 1
    cloneRepos |> Set.filter (fun x -> let subDir = wsDir |> GetSubDirectory x.Repository.Name.toString
                                       not <| subDir.Exists)
               |> Seq.map (fun x -> cloneRepoAndInit wsDir antho.Vcs shallow x.Repository)
               |> Threading.throttle maxThrottle |> Async.Parallel |> Async.RunSynchronously |> ignore

let Add (name : RepositoryId) (url : RepositoryUrl) (branch : BranchId option) (builder : BuilderType) =
    let antho = LoadAnthology ()
    let repo = { Name = name; Url = url; Branch = branch }
    let buildableRepo = { Repository = repo; Builder = builder }
    let repos = antho.Repositories |> Set.add buildableRepo
                                   |> Seq.distinctBy (fun x -> x.Repository.Name)
                                   |> Set
    let newAntho = {antho 
                    with Repositories = repos}
    SaveAnthology newAntho

let Drop (name : RepositoryId) =
    let antho = LoadAnthology ()
    let projectsInRepo = antho.Projects |> Set.filter (fun x -> x.Repository = name) 
    let projectOutputsInRepo = projectsInRepo |> Set.map (fun x -> x.ProjectId)

    let refOutsideRepo = antho.Projects |> Set.filter (fun x -> x.Repository <> name && Set.intersect x.ProjectReferences projectOutputsInRepo <> Set.empty)
    if refOutsideRepo <> Set.empty then 
        printfn "Repository %s is referenced from following projects:" name.toString
        refOutsideRepo |> Set.iter (fun x -> printfn " - %s/%s" x.Repository.toString x.RelativeProjectFile.toString)
    else
        let newAntho = { antho
                         with Projects = Set.difference antho.Projects projectsInRepo
                              Repositories = antho.Repositories |> Set.filter (fun x -> x.Repository.Name <> name) }
        Configuration.SaveAnthology newAntho

let CollectRepoHash wsDir vcsType (repos : Repository set) =
    let getRepoHash (repo : Repository) =
        let tip = Vcs.VcsTip wsDir vcsType repo
        { Repository = repo.Name; Version = BookmarkVersion tip}

    repos |> Set.map getRepoHash

