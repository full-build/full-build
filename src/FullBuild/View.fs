////   Copyright 2014-2016 Pierre Chalamet
////
////   Licensed under the Apache License, Version 2.0 (the "License");
////   you may not use this file except in compliance with the License.
////   You may obtain a copy of the License at
////
////       http://www.apache.org/licenses/LICENSE-2.0
////
////   Unless required by applicable law or agreed to in writing, software
////   distributed under the License is distributed on an "AS IS" BASIS,
////   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
////   See the License for the specific language governing permissions and
////   limitations under the License.
//
//module View
//
//open System.IO
//open Collections
//
//
//type private ViewConfig = FSharp.Configuration.YamlConfig<"View.yaml">
//
//
////[<RequireQualifiedAccess>]
////    { ViewName : string 
////      ViewFilters : string seq
////      ViewParameters : string seq
////      ViewDependencies : bool
////      ViewReferencedBy : bool
////      ViewModified : bool
////      ViewBuilder : Graph.BuilderType }
//
//[<Sealed>]
//type View(name : string,
//          filters : string seq,
//          parameters : string seq,
//          dependencies : bool,
//          referencedBy : bool,
//          modified : bool,
//          builder : Graph.BuilderType) = class end
//with
//    member this.Name = name
//    member this.Filters = filters
//    member this.Parameters = parameters
//    member this.Dependencies = dependencies
//    member this.ReferencedBy = referencedBy
//    member this.Modified = modified
//    member this.Builder = builder
//    member this.Solution = 
//        let viewFolder = Env.GetFolder Env.Folder.View
//        viewFolder |> IoHelpers.GetFile (IoHelpers.AddExt IoHelpers.Extension.View name)
//
//    static member serialize (view : View) =
//        let config = new ViewConfig()
//        config.view.builder <- StringHelpers.toString view.Builder
//        config.view.modified <- view.Modified
//
//        config.view.filters.Clear()
//        for filter in view.Filters do
//            let filterItem = ViewConfig.view_Type.filters_Item_Type()
//            filterItem.filter <- filter
//            config.view.filters.Add filterItem
//
//        config.view.parameters.Clear()
//        for parameter in view.Parameters do
//            let paramItem = ViewConfig.view_Type.parameters_Item_Type()
//            paramItem.parameter <- parameter
//            config.view.parameters.Add paramItem
//        config.view.sourceonly <- view.Dependencies
//        config.view.parents <- view.ReferencedBy
//        config.ToString()
//
//    static member deserialize content =
//        let config = new ViewConfig()
//        config.LoadText content
//           
//        View(config.view.name,
//             config.view.filters |> Seq.map (fun x -> x.filter)
//                                 |> Set.ofSeq,
//             config.view.parameters |> Seq.map (fun x -> x.parameter)
//                                    |> Set.ofSeq,
//             config.view.sourceonly,
//             config.view.parents,
//             config.view.modified,
//             StringHelpers.fromString<Graph.BuilderType> config.view.builder)
//
//    static member Load name =
//        let viewFolder = Env.GetFolder Env.Folder.View
//        let viewFile = viewFolder |> IoHelpers.GetFile (IoHelpers.AddExt IoHelpers.Extension.View name)
//        let content = File.ReadAllText viewFile.FullName
//        View.deserialize content
//
//    member this.Save () =
//        let viewFolder = Env.GetFolder Env.Folder.View
//        let viewFile = viewFolder |> IoHelpers.GetFile (IoHelpers.AddExt IoHelpers.Extension.View this.Name)
//        let content = View.serialize this
//        File.WriteAllText(viewFile.FullName, content)
//
//    member this.Projects (graph : Graph.Graph) : Graph.Project seq =
//        // select only available repositories
//        let availableRespos = graph.Repositories |> Seq.filter (fun x -> x.IsCloned)
//                                                 |> set
//
//        // load back filter & generate view accordingly
//        let modifiedFilters = GetModifiedFilter view
//        let selectionFilters = filters |> Set.union modifiedFilters |> Set.map adaptViewFilter
//
//        // build: <repository>/<project>
//        let projects = antho.Projects
//                       |> Seq.filter (fun x -> availableRepos |> Set.contains x.Repository)
//                       |> Seq.map (fun x -> (sprintf "%s/%s" x.Repository.toString x.Output.toString, x.ProjectId))
//                       |> Map
//        let projectNames = projects |> Seq.map (fun x -> x.Key) |> set
//
//        let matchRepoProject filter =
//            projectNames |> Set.filter (fun x -> PatternMatching.Match x filter)
//
//        let matches = selectionFilters |> Set.map matchRepoProject
//                                       |> Set.unionMany
//        let selectedProjectGuids = projects |> Map.filter (fun k _ -> Set.contains k matches)
//                                            |> Seq.map (fun x -> x.Value)
//                                            |> Set
//        let parents = if view.ReferencedBy then AnthologyGraph.CollectAllParents antho.Projects selectedProjectGuids
//                      else Set.empty
//        let allProjectSet = selectedProjectGuids |> Set.union parents
//
//        // find projects
//        let antho = Configuration.LoadAnthology ()
//        let projectRefs = match view.Dependencies with
//                          | true -> AnthologyGraph.ComputeProjectSelectionClosureSourceOnly antho.Projects allProjectSet |> Set
//                          | _ -> AnthologyGraph.ComputeProjectSelectionClosure antho.Projects allProjectSet |> Set
//
//        let projects = antho.Projects |> Set.filter (fun x -> projectRefs |> Set.contains x.ProjectId)
//        projects
//
//
//let Load name = View.Load name
//
//
//let Create name filters parameters dependencies referencedBy modified builder =
//    View(name, filters, parameters, dependencies, referencedBy, modified, builder)
//
//let Delete name =
//    let viewFolder = Env.GetFolder Env.Folder.View
//    let wsFolder = Env.GetFolder Env.Folder.Workspace
//    let viewFile = viewFolder |> IoHelpers.GetFile (IoHelpers.AddExt IoHelpers.Extension.View name)
//    let slnFile = wsFolder |> IoHelpers.GetFile (IoHelpers.AddExt IoHelpers.Extension.Solution name)
//    if viewFile.Exists then viewFile.Delete()
//    if slnFile.Exists then slnFile.Delete()
//
//let Views () =
//    let viewFolder = Env.GetFolder Env.Folder.View
//    let files = viewFolder.EnumerateFiles("*.view")
//    files |> Seq.map (fun x -> System.IO.Path.GetFileNameWithoutExtension(x.Name))
//
//let Default() =
//    let viewFolder = Env.GetFolder Env.Folder.View
//    let defaultFile = viewFolder |> IoHelpers.GetFile "default"
//    if defaultFile.Exists then Some (File.ReadAllText (defaultFile.FullName))
//    else None
