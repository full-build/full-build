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

type [<CustomEquality; CustomComparison>] Bookmark =
    { Graph : Graph
      Bookmark : Anthology.Bookmark }
with
    override this.Equals(other : System.Object) = refEquals this other

    interface System.IComparable with
        member this.CompareTo(other) = compareTo this other (fun x -> x.Bookmark)

    member this.Repository =
        this.Graph.Repositories |> Seq.find (fun x -> x.Name = this.Bookmark.Repository.toString)

    member this.Version = this.Bookmark.Version.toString

// =====================================================================================================

and [<CustomEquality; CustomComparison>] Baseline =
    { Graph : Graph
      Baseline : Anthology.Baseline }
with
    override this.Equals(other : System.Object) = refEquals this other

    interface System.IComparable with
        member this.CompareTo(other) = compareTo this other (fun x -> x.Baseline)

    member this.IsIncremental = this.Baseline.Incremental

    member this.Bookmarks =
        this.Baseline.Bookmarks |> Set.map (fun x -> { Graph = this.Graph; Bookmark = x })

    static member (-) (a:Baseline, b : Baseline) =
        let changes = Set.difference a.Bookmarks b.Bookmarks
        changes

    member this.Save () =
        Configuration.SaveBaseline this.Baseline

// =====================================================================================================

and [<Sealed>] Factory(graph : Graph) =
    member this.Baseline =
        let baseline = Configuration.LoadBaseline()
        { Graph = graph; Baseline = baseline }

    member this.CreateBaseline (incremental : bool) =
        let notAllCloned = graph.Repositories |> Seq.exists (fun x -> x.IsCloned |> not)
        if notAllCloned then
            failwith "All repositories must be cloned to compute a baseline"

        let wsDir = Env.GetFolder Env.Folder.Workspace
        let bookmarks = graph.Repositories |> Set.map (fun x -> { Anthology.Bookmark.Repository = Anthology.RepositoryId.from x.Name
                                                                  Anthology.Bookmark.Version = Anthology.BookmarkVersion (Tools.Vcs.Tip wsDir x) })
        let baseline = { Anthology.Baseline.Incremental = incremental
                         Anthology.Baseline.Bookmarks = bookmarks }
        { Graph = graph
          Baseline = baseline }


let from graph =
    Factory(graph)
