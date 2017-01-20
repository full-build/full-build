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
type Baseline(graph : Graph, tagInfo : Tag.TagInfo, isHead : bool) = class end
with
    let mutable bookmarks : Bookmark set option = None
    let collectBookmarks () =
        if bookmarks = None then
            let repos = graph.MasterRepository |> Set.singleton
                                               |> Set.union graph.Repositories

            let wsDir = Env.GetFolder Env.Folder.Workspace
            let res = repos |> Set.map (fun x -> let tag = if isHead then Tools.Vcs.Head wsDir x
                                                            else Tag.Format tagInfo
                                                 let hash = Tools.Vcs.TagToHash wsDir x tag
                                                 Bookmark(graph, x, hash))
            bookmarks <- Some res

        bookmarks.Value

    override this.Equals(other : System.Object) = refEquals this other

    interface System.IComparable with
        member this.CompareTo(other) = compareTo this other (fun x -> x.Info)

    member this.Info = tagInfo

    member this.Bookmarks : Bookmark set = 
        collectBookmarks()

    static member (-) (ref : Baseline, target : Baseline) : Bookmark set =
        let changes = Set.difference ref.Bookmarks target.Bookmarks
        changes

    member this.Save () : unit =
        let branch = Configuration.LoadBranch()
        let wsDir = Env.GetFolder Env.Folder.Workspace
        let tag = Tag.Format tagInfo

        graph.Repositories |> Set.iter (fun x -> Tools.Vcs.Tag wsDir x tag)
        Tools.Vcs.Tag wsDir graph.MasterRepository tag

// =====================================================================================================

[<Sealed>] 
type Factory(graph : Graph) = class end
with
    let wsDir = Env.GetFolder Env.Folder.Workspace

    member this.Baseline : Baseline =
        let branch = Configuration.LoadBranch()
        let tagFilter = sprintf "fullbuild_%s_*" branch
        match Tools.Vcs.FindLatestMatchingTag wsDir graph.MasterRepository tagFilter with
        | Some tag -> let tagInfo = Tag.Parse tag
                      Baseline(graph, tagInfo, false)
        | _ -> let tagInfo = { Tag.TagInfo.Branch = branch; Tag.TagInfo.BuildNumber = "dummy"; Tag.TagInfo.Incremental = false}
               Baseline(graph, tagInfo, true)

    member this.CreateBaseline (incremental : bool) (buildNumber : string) : Baseline =
        let graph = Configuration.LoadAnthology() |> Graph.from
        let branch = Configuration.LoadBranch()
        let tagInfo = { Tag.TagInfo.Branch = branch; Tag.TagInfo.BuildNumber = buildNumber; Tag.TagInfo.Incremental = incremental }
        Baseline(graph, tagInfo, true)

// =====================================================================================================

let from graph =
    Factory(graph)
