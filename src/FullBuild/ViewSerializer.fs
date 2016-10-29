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

module ViewSerializer

open Anthology
open System.IO
open Collections


type private ViewConfig = FSharp.Configuration.YamlConfig<"View.yaml">


let SerializeView (view : View) =
    let config = new ViewConfig()
    config.view.builder <- view.Builder.toString
    config.view.name <- view.Name
    config.view.filters.Clear()
    for filter in view.Filters do
        let filterItem = ViewConfig.view_Type.filters_Item_Type()
        filterItem.filter <- filter
        config.view.filters.Add filterItem

    config.view.upward <- view.UpReferences
    config.view.downward <- view.DownReferences
    config.view.modified <- view.Modified
    config.view.appfilter <- match view.AppFilter with
                             | None -> null
                             | Some appFilter -> appFilter

    config.ToString()


let DeserializeView content =
    let config = new ViewConfig()
    config.LoadText content
    { Name = config.view.name
      Filters = config.view.filters
                |> Seq.map (fun x -> x.filter)
                |> Set.ofSeq
      Builder = BuilderType.from config.view.builder
      UpReferences = config.view.upward
      DownReferences = config.view.downward
      Modified = config.view.modified 
      AppFilter = (config.view.appfilter |> isNull) ? (None, Some config.view.appfilter) }


let Save (filename : FileInfo) (view : View) =
    let content = SerializeView view
    File.WriteAllText(filename.FullName, content)

let Load (filename : FileInfo) : View =
    let content = File.ReadAllText (filename.FullName)
//    { Name = "toto"
//      Filters = Set.empty
//      Parameters = Set.empty
//      Builder = Anthology.BuilderType.MSBuild
//      SourceOnly = true
//      Parents = true
//      Modified = true }

    DeserializeView content

