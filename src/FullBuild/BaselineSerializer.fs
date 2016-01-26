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

module BaselineSerializer

open Anthology
open System.IO
open System
open Collections
open System.Text


type private BaselineConfig = FSharp.Configuration.YamlConfig<"Baseline.yaml">


let SerializeBaseline (baseline : Baseline) =
    let config = new BaselineConfig()
    config.baseline.Clear()
    for bookmark in baseline.Bookmarks do
        let item = new BaselineConfig.baseline_Item_Type ()
        item.repo <- bookmark.Repository.toString
        item.version <- bookmark.Version.toString
        config.baseline.Add item

    config.ToString()


let DeserializeBaseline content =
    let rec convertToBookmark (items : BaselineConfig.baseline_Item_Type list) =
        match items with
        | [] -> Set.empty
        | x :: tail -> convertToBookmark tail |> Set.add { Repository=RepositoryId.from x.repo ; Version=BookmarkVersion x.version }

    let config = new BaselineConfig()
    config.LoadText content    
    { Bookmarks = convertToBookmark (config.baseline |> List.ofSeq) }



let Save (filename : FileInfo) (baseline : Baseline) =
    let content = SerializeBaseline baseline
    File.WriteAllText(filename.FullName, content)

let Load (filename : FileInfo) : Baseline =
    let content = File.ReadAllText (filename.FullName)
    DeserializeBaseline content

