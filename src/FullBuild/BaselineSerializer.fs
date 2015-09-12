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
        match bookmark.Version with
        | Master -> ()
        | BookmarkVersion version -> let item = new BaselineConfig.baseline_Item_Type ()
                                     item.repo <- bookmark.Repository.toString
                                     item.version <- version
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

