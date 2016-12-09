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

module Baselines

open Graph
open Collections

#nowarn "0346" // GetHashCode missing


// =====================================================================================================

[<Sealed>]
type Bookmark(graph : Graph, repository : Repository, hash : string) = class end
with
    override this.Equals(other : System.Object) = refEquals this other

    interface System.IComparable with
        member this.CompareTo(other) = compareTo this other (fun x -> sprintf "%s %s" x.Repository.Name x.Version)

    member this.Repository =
        repository

    member this.Version = hash

// =====================================================================================================

[<Sealed>]
type Baseline(graph : Graph, label : string) = class end
with
    let incremental : bool = false

    let mutable bookmarks : Bookmark set option = None
    let collectBookmarks (tag : string) : Bookmark set =
        let repos = graph.MasterRepository |> Set.singleton
                                           |> Set.union graph.Repositories

        let wsDir = Env.GetFolder Env.Folder.Workspace
        let res = repos |> Set.map (fun x -> let hash = Tools.Vcs.TagToHash wsDir x tag
                                             Bookmark(graph, x, hash))
        bookmarks <- Some res
        res


    override this.Equals(other : System.Object) = refEquals this other

    interface System.IComparable with
        member this.CompareTo(other) = compareTo this other (fun x -> sprintf "%A %A" x.IsIncremental x.BuildNumber)

    member this.IsIncremental = incremental

    member this.BuildNumber = label

    member this.Bookmarks : Bookmark set = 
        bookmarks |> orDefault (collectBookmarks label)

    static member (-) (ref : Baseline, target : Baseline) : Bookmark set =
        let changes = Set.difference ref.Bookmarks target.Bookmarks
        changes

    member this.Save () : unit =
        let wsDir = Env.GetFolder Env.Folder.Workspace
        let comment = this.IsIncremental ? ("incremental", "fullbuild")
        this.Bookmarks |> Set.iter (fun x -> Tools.Vcs.Tag wsDir x.Repository x.Version this.BuildNumber comment)

// =====================================================================================================

[<Sealed>] 
type Factory(graph : Graph) = class end
with
    let wsDir = Env.GetFolder Env.Folder.Workspace

    member this.Baseline : Baseline =
        let branch = Configuration.LoadBranch()
        let tagFilter = sprintf "fullbuild-%s-*" branch
        let tag = Tools.Vcs.FindLatestMatchingTag wsDir graph.MasterRepository tagFilter |> orDefault "HEAD"
        Baseline(graph, tag)

    member this.CreateBaseline (incremental : bool) (buildNumber : string) : Baseline =
        let tag = Tools.Vcs.Head wsDir graph.MasterRepository
        Baseline(graph, tag)

// =====================================================================================================

let from graph =
    Factory(graph)
