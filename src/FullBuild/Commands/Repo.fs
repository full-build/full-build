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

module Commands.Repo

open Collections
open Graph

let private cloneRepoAndInit wsDir shallow (repo : Repository) =
    async {
        IoHelpers.DisplayHighlight repo.Name
        Tools.Vcs.Clone wsDir repo shallow
    }

let List() =
    let graph = Configuration.LoadAnthology() |> Graph.from

    let printRepo (repo : Repository) =
        // HACK: use correct builder
        printfn "%s : %s [%A]" repo.Name repo.Uri (StringHelpers.toString BuilderType.MSBuild)

    graph.Repositories |> Seq.iter printRepo

let Clone (cmd : CLI.Commands.CloneRepositories) =
    let wsDir = Env.GetFolder Env.Folder.Workspace
    let graph = Configuration.LoadAnthology() |> Graph.from
    let fakeView = graph.CreateView "clone" cmd.Filters Set.empty cmd.All false false Graph.BuilderType.MSBuild
    let selectedRepos = fakeView.Projects |> Set.map (fun x -> x.Repository)
                                          |> Set.filter (fun x -> not x.IsCloned)

    let maxThrottle = cmd.Multithread ? (System.Environment.ProcessorCount*2, 1)
    selectedRepos |> Seq.map (cloneRepoAndInit wsDir cmd.Shallow)
                  |> Threading.throttle maxThrottle |> Async.Parallel |> Async.RunSynchronously |> ignore

let Add (cmd : CLI.Commands.AddRepository) =
    let graph = Configuration.LoadAnthology () |> Graph.from
    let newGraph = graph.CreateRepo cmd.Name cmd.Url cmd.Builder cmd.Branch
    newGraph.Save()

let Drop (name : string) =
    let graph = Configuration.LoadAnthology () |> Graph.from
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
