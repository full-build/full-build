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
type BuildType =
    | Full
    | Incremental
    | Draft

[<RequireQualifiedAccess>]
type BuildStatus =
    | Complete
    | Draft


let printTag ((repo, execResult) : (Repository * Exec.ExecResult)) =
    lock consoleLock (fun () -> IoHelpers.DisplayHighlight repo.Name
                                execResult |> Exec.PrintOutput)

let private tagRepo wsDir (tag : string) (repo : Repository) = async {
    return (repo, Tools.Vcs.Tag wsDir repo tag) |> printTag
}


[<RequireQualifiedAccess>]
type TagInfo =
    { BuildBranch : string
      BuildNumber : string 
      BuildType : BuildType }
with
    member this.Branch = this.BuildBranch

    member this.Version = this.BuildNumber

    member this.Type = this.BuildType

    member this.Format() =
        match this.Type with
        | BuildType.Full -> sprintf "fullbuild_%s_%s_full" this.Branch this.BuildNumber
        | BuildType.Incremental -> sprintf "fullbuild_%s_%s_inc" this.Branch this.BuildNumber
        | BuildType.Draft -> sprintf "fullbuild_%s_%s" this.Branch this.BuildNumber

    static member Parse (tag : string) =
        let items = tag.Split('_') |> List.ofArray
        match items with
        | ["fullbuild"; branch; version; "full"] -> { TagInfo.BuildBranch = branch; TagInfo.BuildNumber = version; TagInfo.BuildType = BuildType.Full }
        | ["fullbuild"; branch; version; "inc"] -> { TagInfo.BuildBranch = branch; TagInfo.BuildNumber = version; TagInfo.BuildType = BuildType.Incremental }
        | ["fullbuild"; branch; version] -> { TagInfo.BuildBranch = branch; TagInfo.BuildNumber = version; TagInfo.BuildType = BuildType.Draft }
        | _ -> failwithf "Unknown tag"
        

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

            let wsDir = Env.GetFolder Env.Folder.Workspace
            let res = repos |> Set.map (fun x -> let tag = if isHead then Tools.Vcs.Head wsDir x
                                                            else tagInfo.Format()
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
        let wsDir = Env.GetFolder Env.Folder.Workspace
        let tag = tagInfo.Format()

        let maxThrottle = System.Environment.ProcessorCount*4
        let tagResults = graph.Repositories |> Seq.filter (fun x -> x.IsCloned)
                         |> Seq.map (tagRepo wsDir tag)
                         |> Threading.throttle maxThrottle |> Async.Parallel |> Async.RunSynchronously
        tagResults |> Exec.CheckMultipleResponseCode

        Tools.Vcs.Tag wsDir graph.MasterRepository tag |> Exec.CheckResponseCode

// =====================================================================================================

[<Sealed>] 
type Factory(graph : Graph) = class end
with
    let wsDir = Env.GetFolder Env.Folder.Workspace

    member this.FindBaseline (status : BuildStatus) : Baseline =
        let branch = Configuration.LoadBranch()
        let tagFilter = match status with
                        | BuildStatus.Draft -> sprintf "fullbuild_%s_*" branch
                        | BuildStatus.Complete -> sprintf "fullbuild_%s_*_*" branch
        match Tools.Vcs.FindLatestMatchingTag wsDir graph.MasterRepository tagFilter with
        | Some tag -> let tagInfo = TagInfo.Parse tag
                      Baseline(graph, tagInfo, false)
        | _ -> let tagInfo = { TagInfo.BuildBranch = branch; TagInfo.BuildNumber = "temp"; TagInfo.BuildType = BuildType.Draft}
               Baseline(graph, tagInfo, true)

    member this.CreateBaseline (buildType : BuildType) (buildNumber : string) : Baseline =
        let graph = Configuration.LoadAnthology() |> Graph.from
        let branch = Configuration.LoadBranch()
        let tagInfo = { TagInfo.BuildBranch = branch; TagInfo.BuildNumber = buildNumber; TagInfo.BuildType = buildType }
        Baseline(graph, tagInfo, true)

// =====================================================================================================

let from graph =
    Factory(graph)
