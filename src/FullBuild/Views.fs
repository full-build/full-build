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

module Views

open Graph
open Collections

#nowarn "0346" // GetHashCode missing


type [<CustomEquality; CustomComparison>] View =
    { Graph : Graph
      View : Anthology.View }
with
    override this.Equals(other : System.Object) = refEquals this other

    interface System.IComparable with
        member this.CompareTo(other) = compareTo this other (fun x -> x.View)

    member this.Name = this.View.Name
    member this.Filters = this.View.Filters
    member this.UpReferences = this.View.UpReferences
    member this.DownReferences = this.View.DownReferences
    member this.Modified = this.View.Modified
    member this.AppFilter = this.View.AppFilter
    member this.Tests = this.View.Tests

    member this.Projects : Project set =
        let filters = this.View.Filters |> Set.map (fun x -> if x.IndexOfAny([|'/'; '\\'; '*' |]) = -1 then x + "/*" else x)
                                        |> Set.map (fun x -> x.Replace('\\', '/'))
        let allClonedProjects = this.Graph.Repositories |> Set.filter (fun x -> x.IsCloned)
                                                        |> Set.map (fun x -> x.Projects)
                                                        |> Set.unionMany
        let projects = PatternMatching.FilterMatch<Project>
                            allClonedProjects
                            (fun x -> sprintf "%s/%s" x.Repository.Name x.Output.Name)
                            filters

        let modBookmarks = if this.Modified then
                               // newBaseline contains all repositories bookmarks - even non cloned ones (because true)
                               // this will add new repositories if necessary and discard unchanged repositories
                               // in fine, we only have new repositories and modified repositories
                               let baselineRepo = Baselines.from this.Graph

                               let sourcesBaseline = baselineRepo.GetSourcesBaseline()
                               let binBaseline = baselineRepo.GetBaseline() 

                               let delta = 
                                    match binBaseline with
                                    | Some oldBaseline -> sourcesBaseline - oldBaseline
                                    | None -> sourcesBaseline.Bookmarks

                               // if master repository is modified then all repositories are modified !
                               let isFullRebuild = delta |> Seq.exists (fun x -> x.Repository.Name = this.Graph.MasterRepository.Name)

                               if isFullRebuild then sourcesBaseline.Bookmarks
                               else delta

                           else Set.empty

        let modProjects = modBookmarks |> Set.map (fun x -> x.Repository.Projects)
                                       |> Set.unionMany

        let appProjects = match this.AppFilter with
                          | Some appFilter -> let apps = PatternMatching.FilterMatch this.Graph.Applications (fun x -> x.Name) (Set.singleton appFilter)
                                              apps |> Set.map (fun x -> x.Project)
                          | None -> Set.empty

        let viewProjects = Project.Closure (projects + modProjects + appProjects)
        let depProjects = if this.UpReferences then Project.TransitiveReferencedBy viewProjects
                          else Set.empty
        let refProjects = if this.DownReferences then Project.TransitiveReferences viewProjects
                          else Set.empty
        let projects = viewProjects + depProjects + refProjects
        let unittests = if this.Tests then 
                            projects |> Set.map (fun x -> x.ReferencedBy |> Set.filter (fun y -> y.HasTests))
                                     |> Set.unionMany
                        else Set.empty
        let viewProjects = projects + unittests

        let repositoriesNotCloned = viewProjects |> Set.map (fun x -> x.Repository)
                                                 |> Set.filter (fun x -> x.IsCloned |> not)
        if repositoriesNotCloned <> Set.empty then
            printfn "ERROR: some repositories must be cloned to create the view"
            repositoriesNotCloned |> Set.iter (fun x -> printfn "  %s" x.Name)
            failwithf "Missing repositories"
        viewProjects

    member this.Save (isDefault : bool option) =
        let viewId = Anthology.ViewId this.View.Name
        Configuration.SaveView viewId this.View isDefault
    
    member this.SaveStatic () =
        let staticViewFile = Env.GetStaticViewFile this.View.Name
        ViewSerializer.Save staticViewFile this.View

    member this.Delete () =
        Configuration.DeleteView (Anthology.ViewId this.View.Name)


and [<Sealed>] Factory(graph : Graph) =
    let mutable viewMap : System.Collections.Generic.IDictionary<Anthology.ViewId, View> = null

    member this.ViewMap : System.Collections.Generic.IDictionary<Anthology.ViewId, View> =
        if viewMap |> isNull then
            let vwDir = Env.GetFolder Env.Folder.View
            viewMap <- vwDir.EnumerateFiles(IoHelpers.Extension.View |> IoHelpers.GetExtensionString |> sprintf "*.%s") |> Seq.map (fun x -> System.IO.Path.GetFileNameWithoutExtension(x.Name) |> Anthology.ViewId)
                                                      |> Seq.map Configuration.LoadView
                                                      |> Seq.map (fun x -> x.Name |> Anthology.ViewId, { Graph = graph; View = x })
                                                      |> dict
        viewMap

    member this.Views = this.ViewMap.Values |> set

    member this.DefaultView =
        let viewId = Configuration.DefaultView ()
        match viewId with
        | None -> None
        | Some x -> Some this.ViewMap.[x]

    member this.CreateView name filters downReferences upReferences modified appFilter tests =
        let view = { Anthology.View.Name = name
                     Anthology.View.Filters = filters
                     Anthology.View.DownReferences = downReferences
                     Anthology.View.UpReferences = upReferences
                     Anthology.View.Modified = modified
                     Anthology.View.AppFilter = appFilter
                     Anthology.View.Tests = tests }

        { Graph = graph
          View = view }

let from graph =
    Factory(graph)
