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

module ViewSerializer

open Anthology
open System.IO
open Collections


type private ViewConfig = FSharp.Configuration.YamlConfig<"Examples/View.yaml">


let SerializeView (view : View) =
    let config = new ViewConfig()
    config.name <- view.Name
    config.filters.Clear()
    for filter in view.Filters do
        let filterItem = ViewConfig.filters_Item_Type()
        filterItem.filter <- filter
        config.filters.Add filterItem

    config.referencedBy <- view.UpReferences
    config.references <- view.DownReferences
    config.modified <- view.Modified
    config.tests <- view.Tests
    config.appfilter <- match view.AppFilter with
                        | None -> null
                        | Some appFilter -> appFilter
    config.config <- match view.Configuration with
                     | Some conf -> conf
                     | None -> null
    config.ToString()


let DeserializeView content =
    let config = new ViewConfig()
    config.LoadText content
    { Name = config.name
      Filters = config.filters
                |> Seq.map (fun x -> x.filter)
                |> Set.ofSeq
      UpReferences = config.referencedBy
      DownReferences = config.references
      Modified = config.modified 
      Tests = config.tests
      AppFilter = (config.appfilter = "") ? (None, Some config.appfilter) 
      Configuration = (config.config = "") ? (None, Some config.config) }


let Save (filename : FileInfo) (view : View) =
    let content = SerializeView view
    File.WriteAllText(filename.FullName, content)

let Load (filename : FileInfo) : View =
    let content = File.ReadAllText (filename.FullName)
    DeserializeView content

