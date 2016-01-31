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

    config.view.filters.Clear()
    for filter in view.Filters do
        let filterItem = ViewConfig.view_Type.filters_Item_Type()
        filterItem.filter <- filter
        config.view.filters.Add filterItem

    config.view.parameters.Clear()
    for parameter in view.Parameters do
        let paramItem = ViewConfig.view_Type.parameters_Item_Type()
        paramItem.parameter <- parameter
        config.view.parameters.Add paramItem

    config.ToString()


let DeserializeView content =
    let config = new ViewConfig()
    config.LoadText content    
    { Filters = config.view.filters 
                |> Seq.map (fun x -> x.filter)
                |> Set.ofSeq
      Parameters = config.view.parameters 
                   |> Seq.map (fun x -> x.parameter)
                   |> Set.ofSeq
      Builder = BuilderType.from config.view.builder }


let Save (filename : FileInfo) (view : View) =
    let content = SerializeView view
    File.WriteAllText(filename.FullName, content)

let Load (filename : FileInfo) : View =
    let content = File.ReadAllText (filename.FullName)
    DeserializeView content

