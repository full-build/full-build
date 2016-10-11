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

module RepoCommands

open PatternMatching
open Collections
open IoHelpers
open Graph

let List() =
    let graph = Configuration.LoadAnthology() |> Graph.from

    let printRepo (repo : Repository) =
        // HACK: use correct builder
        printfn "%s : %s [%A]" repo.Name repo.Uri (StringHelpers.toString BuilderType.MSBuild)

    graph.Repositories |> Seq.iter printRepo

let cloneRepoAndInit wsDir shallow (repo : Repository) =
    async {
        DisplayHighlight repo.Name
        Plumbing.Vcs.Clone wsDir repo shallow
    }

let Clone (cmd : Commands.CloneRepositories) =
    let wsDir = Env.GetFolder Env.Folder.Workspace
    let graph = Configuration.LoadAnthology() |> Graph.from
    let filter (repos : Repository seq) (filter : string) = repos |> Seq.filter (fun x -> Match x.Name filter)                                                            
    let filteredRepos = cmd.Filters |> Seq.map (fun x -> x.toString)
                                    |> Seq.map (filter graph.Repositories)
                                    |> Seq.concat
                                    |> Set
    let filteredProjects = filteredRepos |> Set.map (fun x -> x.Projects |> set)
                                         |> Set.unionMany

    let selectedProjects = if cmd.All then GraphHelpers.ComputeTransitiveReferences filteredProjects
                           else filteredProjects
    let selectedRepos = selectedProjects |> Set.map (fun x -> x.Repository)

    let maxThrottle = cmd.Multithread ? (System.Environment.ProcessorCount*2, 1)
    selectedRepos |> Seq.map (cloneRepoAndInit wsDir cmd.Shallow)
                  |> Threading.throttle maxThrottle |> Async.Parallel |> Async.RunSynchronously |> ignore

let Add (cmd : Commands.AddRepository) =
    let antho = Configuration.LoadAnthology ()
    let repo = { Anthology.Name = cmd.Repo; Anthology.Url = cmd.Url; Anthology.Branch = cmd.Branch }
    let buildableRepo = { Anthology.Repository = repo; Anthology.Builder = cmd.Builder }
    let repos = antho.Repositories |> Set.add buildableRepo
                                   |> Seq.distinctBy (fun x -> x.Repository.Name)
                                   |> Set
    let newAntho = {antho
                    with Repositories = repos}
    Configuration.SaveAnthology newAntho

let Drop (name : string) =
    let repoName = name |> Anthology.RepositoryId.from
    let antho = Configuration.LoadAnthology ()
    let projectsInRepo = antho.Projects |> Set.filter (fun x -> x.Repository = repoName)
    let projectOutputsInRepo = projectsInRepo |> Set.map (fun x -> x.ProjectId)

    let refOutsideRepo = antho.Projects |> Set.filter (fun x -> x.Repository <> repoName && Set.intersect x.ProjectReferences projectOutputsInRepo <> Set.empty)
    if refOutsideRepo <> Set.empty then
        printfn "Repository %s is referenced from following projects:" name
        refOutsideRepo |> Set.iter (fun x -> printfn " - %s/%s" x.Repository.toString x.RelativeProjectFile.toString)
    else
        let newAntho = { antho
                         with Projects = Set.difference antho.Projects projectsInRepo
                              Repositories = antho.Repositories |> Set.filter (fun x -> x.Repository.Name <> repoName) }
        Configuration.SaveAnthology newAntho

//let CollectRepoHash wsDir vcsType (repos : Repository set) =
//    let getRepoHash (repo : Repository) =
//        let tip = Vcs.Tip wsDir repo
//        { Repository = repo.Name; Version = BookmarkVersion tip}
//
//    repos |> Set.map getRepoHash
//