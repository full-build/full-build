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

module Commands.Repo

open Collections
open Graph

let List() =
    let graph = Graph.load ()
    let printRepo (repo : Repository) =
        printfn "%s : %s [%s %s %s]" repo.Name repo.Uri (StringHelpers.toString repo.Vcs) (StringHelpers.toString repo.Builder) (StringHelpers.toString repo.Tester)

    graph.Repositories |> Seq.iter printRepo

let Clone (cmd : CLI.Commands.CloneRepositories) =
    let cloneRepoAndInit wsDir shallow (branch : string option) (repo : Repository) =
        async {
            let res = Tools.Vcs.Clone wsDir repo shallow |> ConsoleHelpers.PrintOutput (sprintf "Cloning %s" repo.Name)
            res |> IO.CheckResponseCode

            if branch.IsSome then
                Tools.Vcs.Checkout wsDir repo branch.Value |> ConsoleHelpers.PrintOutput (sprintf "Checkouting %s" repo.Name)
                                                           |> ignore

            return res
        }

    let wsDir = Env.GetFolder Env.Folder.Workspace
    let graph = Graph.load()
    let selectedRepos = PatternMatching.FilterMatch graph.Repositories (fun x -> x.Name) cmd.Filters
    let branch = Configuration.LoadBranch()
    let br = if branch = graph.MasterRepository.Branch then None
             else Some branch
    selectedRepos |> Set.filter (fun x -> not x.IsCloned)
                  |> Threading.ParExec (cloneRepoAndInit wsDir cmd.Shallow br)
                  |> IO.CheckMultipleResponseCode

let Add (cmd : CLI.Commands.AddRepository) =
    let graph = Graph.load()
    let newGraph = graph.CreateRepo cmd.Name cmd.Vcs cmd.Url cmd.Builder cmd.Tester cmd.Branch
    newGraph.Save()

let Drop (name : string) =
    let graph = Graph.load()
    let repo = graph.Repositories |> Seq.find (fun x -> x.Name = name)
    let referencingProjects = repo.Projects |> Set.map (fun x -> x.ReferencedBy)
                                            |> Set.unionMany
    let referencingRepos = referencingProjects |> Set.map (fun x -> x.Repository)
                                               |> Set.remove repo
    if referencingRepos = Set.empty then
        let newGraph = repo.Delete()
        newGraph.Save()
    else
        printfn "Repository %s is referenced from following projects:" name
        referencingProjects |> Set.iter (fun x -> printfn " - %s/%s" x.Repository.Name x.ProjectFile)
