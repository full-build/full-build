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
open IoHelpers

#nowarn "0346" // GetHashCode missing


[<RequireQualifiedAccess>]
type TagInfo =
    { BuildBranch : string
      BuildNumber : string }
with
    member this.Branch = this.BuildBranch

    member this.Version = this.BuildNumber

    member this.Format() =
        sprintf "fullbuild/%s/%s" this.Branch this.BuildNumber

    static member Parse (tag : string) =
        if tag.StartsWith("fullbuild/") |> not then failwithf "Unknown tag"
        let tag = tag.Substring("fullbuild/".Length)
        let idx = tag.LastIndexOf('/')
        if(-1 = idx) then failwithf "Unknown tag"

        let branch = tag.Substring(0, idx)
        let version = tag.Substring(idx+1)
        { TagInfo.BuildBranch = branch; TagInfo.BuildNumber = version }


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
type Baseline(graph : Graph, tagInfo : TagInfo, isHead : bool) = class end
with
    let mutable bookmarks : Bookmark set option = None
    let collectBookmarks () =
        if bookmarks = None then
            let repos = graph.MasterRepository |> Set.singleton
                                               |> Set.union graph.Repositories
                                               |> Set.filter (fun x -> x.IsCloned)

            let wsDir = Env.GetFolder Env.Folder.Workspace
            let res = repos |> Set.map (fun x -> let tag = if isHead then Tools.Vcs.Head wsDir x
                                                            else tagInfo.Format()
                                                 let hash = try
                                                                Tools.Vcs.TagToHash wsDir x tag
                                                            with
                                                                _ -> Tools.Vcs.Head wsDir x

                                                 Bookmark(graph, x, hash))
            bookmarks <- Some res

        bookmarks.Value

    override this.Equals(other : System.Object) = refEquals this other

    interface System.IComparable with
        member this.CompareTo(other) = compareTo this other (fun x -> x.Info)

    member this.Info = tagInfo

    member this.IsHead = isHead

    member this.Bookmarks : Bookmark set =
        collectBookmarks()

    static member (-) (ref : Baseline, target : Baseline) : Bookmark set =
        let changes = Set.difference ref.Bookmarks target.Bookmarks
        changes

    member this.Save (comment : string) : unit =
        let wsDir = Env.GetFolder Env.Folder.Workspace
        let tag = tagInfo.Format()

        let tagRepo wsDir (tag : string) (comment : string) (repo : Repository) = async {
            let res = Tools.Vcs.Tag wsDir repo tag comment
            return res |> IoHelpers.PrintOutput repo.Name
        }

        graph.Repositories |> Seq.filter (fun x -> x.IsCloned)
                           |> Threading.ParExec (tagRepo wsDir tag comment)
                           |> Exec.CheckMultipleResponseCode

        Tools.Vcs.Tag wsDir graph.MasterRepository tag comment |> Exec.CheckResponseCode

// =====================================================================================================

[<Sealed>]
type Factory(graph : Graph) = class end
with
    let wsDir = Env.GetFolder Env.Folder.Workspace

    member this.FindBaseline () : Baseline =
        let branch = Configuration.LoadBranch()
        let tagFilter = sprintf "fullbuild/%s/*" branch
        match Tools.Vcs.FindLatestMatchingTag wsDir graph.MasterRepository tagFilter with
        | Some tag -> let tagInfo = TagInfo.Parse tag
                      Baseline(graph, tagInfo, false)
        | _ -> let tagInfo = { TagInfo.BuildBranch = branch; TagInfo.BuildNumber = "temp" }
               Baseline(graph, tagInfo, true)

    member this.CreateBaseline (buildNumber : string) : Baseline =
        let globals = Configuration.LoadGlobals()
        let antho = Configuration.LoadAnthology()
        let graph = Graph.from globals antho
        let branch = Configuration.LoadBranch()
        let tagInfo = { TagInfo.BuildBranch = branch; TagInfo.BuildNumber = buildNumber }
        Baseline(graph, tagInfo, true)

// =====================================================================================================

let from graph =
    Factory(graph)
