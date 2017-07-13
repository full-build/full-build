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
type BuildInfo =
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
        { BuildInfo.BuildBranch = branch; BuildInfo.BuildNumber = version }


// =====================================================================================================

[<Sealed>]
type Bookmark(repository : Repository, hash : string) = class end
with
    override this.Equals(other : System.Object) = refEquals this other

    //not comparable, max equitable
    interface System.IComparable with
        member this.CompareTo(other) = compareTo this other (fun x -> sprintf "%s %s" x.Repository.Name x.Version)

    member this.Repository =
        repository

    member this.Version = hash

// =====================================================================================================

type private BaselineFile = FSharp.Configuration.YamlConfig<"Examples/baseline.yaml">

[<Sealed>]
type Baseline(buildInfo : BuildInfo, bookmarks : Bookmark set) = class end
with
    override this.Equals(other : System.Object) = refEquals this other

    interface System.IComparable with
        member this.CompareTo(other) = compareTo this other (fun x -> x.Info)

    member this.Info = buildInfo

    member this.Bookmarks : Bookmark set = bookmarks

    static member (-) (ref : Baseline, target : Baseline) : Bookmark set =
        let changes = Set.difference ref.Bookmarks target.Bookmarks
        changes

// =====================================================================================================

[<Sealed>]
type Factory(graph : Graph) = class end
with
    let wsDir = Env.GetFolder Env.Folder.Workspace

    let parseBaselineFile (filePath:string) =
        let baselineFile = BaselineFile()
        filePath |> baselineFile.Load
        let buildInfo = { BuildInfo.BuildBranch = baselineFile.branch; BuildInfo.BuildNumber = baselineFile.buildnumber }
        let repositories = 
            graph.Repositories 
            |> Set.add graph.MasterRepository 
            |> Seq.map(fun r -> r.Name, r) 
            |> Map.ofSeq
        let bookmarks = 
                baselineFile.repositories 
                    |> Seq.map (fun x -> Bookmark(repositories |> Map.find x.name, x.version))
                    |> Set.ofSeq
        Baseline(buildInfo, bookmarks)    
        
    let serializeBaseline (baseline:Baseline) =
        let repositories = 
            baseline.Bookmarks 
            |> Seq.map(fun b -> let r = BaselineFile.repositories_Item_Type()
                                r.name <- b.Repository.Name
                                r.version <- b.Version
                                r)
            |> Array.ofSeq
        let baselineFile = BaselineFile()
        baselineFile.branch <- baseline.Info.Branch
        baselineFile.buildnumber <- baseline.Info.BuildNumber
        baselineFile.repositories <- repositories
        baselineFile

    let getClonedReposBookmarks () =
        let getRepoHash wsDir (repo : Repository) = async {
            let changesetId = Tools.Vcs.LastCommit wsDir repo ""
            return Bookmark(repo, changesetId)
        }
        graph.Repositories 
        |> Set.add graph.MasterRepository
        |> Seq.filter (fun x -> x.IsCloned)
        |> Threading.ParExec (getRepoHash wsDir)
        |> Set.ofSeq
    
    let updateBookmarks (source:Bookmark set) (overrideWith:Bookmark set) =
        let dif = 
            source 
            |> Seq.where(fun b -> overrideWith 
                                    |> Seq.exists (fun b1 -> b1.Repository.Name = b.Repository.Name) 
                                    |> not) 
            |> Set.ofSeq
        Set.union dif overrideWith

    member this.GetPulledBaseline () : Baseline option =
        let baselineFileInfo = Env.GetBaselineFile() 
        if baselineFileInfo.Exists then
            baselineFileInfo.FullName |> parseBaselineFile |> Some
        else
            None

    member this.GetSourcesBaseline () : Baseline =
        let pulledBaseline = this.GetPulledBaseline()
        let sourcesBookmarks = getClonedReposBookmarks ()

        let resultBaseline = 
            match pulledBaseline with
            | Some baseline -> updateBookmarks baseline.Bookmarks sourcesBookmarks
            | None -> sourcesBookmarks

        let branch = Configuration.LoadBranch()
        let buildInfo = { BuildInfo.BuildBranch = branch; BuildInfo.BuildNumber = "1.0.0" }
        Baseline(buildInfo, resultBaseline) 

    member this.GetBinariesBaseline () : Baseline option =
        let baselineFileInfo = Env.GetTemporaryBaselineFile()
        if baselineFileInfo.Exists then
            baselineFileInfo.FullName |> parseBaselineFile |> Some
        else
            None
               
    member this.UpdateBaseline (buildNumber : string) : unit =
        let branch = Configuration.LoadBranch()
        let buildInfo = { BuildInfo.BuildBranch = branch; BuildInfo.BuildNumber = buildNumber }

        let wsDir = Env.GetFolder Env.Folder.Workspace

        let sourcesBasline = this.GetSourcesBaseline()
        let baseline = Baseline(buildInfo, sourcesBasline.Bookmarks)
        let baselineFile = serializeBaseline baseline
        Env.GetTemporaryBaselineFile().FullName |> baselineFile.Save

     member this.FindMatchingBuildInfo () : BuildInfo option =
        let branch = Configuration.LoadBranch()
        let tagFilter = sprintf "fullbuild/%s/*" branch
        match Tools.Vcs.FindLatestMatchingTag wsDir graph.MasterRepository tagFilter with
        | Some tag -> BuildInfo.Parse tag |> Some
        | _ -> None
        
    member this.TagMasterRepository (comment : string) : unit =
        let wsDir = Env.GetFolder Env.Folder.Workspace
        let baseline = this.GetPulledBaseline() |> Option.get
        let tag = baseline.Info.Format()
        
        Tools.Vcs.Tag wsDir graph.MasterRepository tag comment |> Exec.CheckResponseCode
// =====================================================================================================

let from graph =
    Factory(graph)
