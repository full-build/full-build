module Tag
open Collections

[<RequireQualifiedAccess>]
type TagInfo =
    { Branch : string
      BuildNumber : string 
      Incremental : bool }

let (|FullBuild|Unknown|) x =
    match x with
    | "fullbuild" -> FullBuild
    | _ -> Unknown

let (|Incremental|Full|Unknown|) x =
    match x with
    | "inc" -> Incremental
    | "full" -> Full
    | _ -> Unknown

let Parse (tag : string) : TagInfo =
    let items = tag.Split('-') |> List.ofArray
    match items with
    | [FullBuild; branch; version; Incremental] -> { TagInfo.Branch = branch; TagInfo.BuildNumber = version; TagInfo.Incremental = true }
    | [FullBuild; branch; version; Full] -> { TagInfo.Branch = branch; TagInfo.BuildNumber = version; TagInfo.Incremental = false }
    | _ -> failwithf "Unknown tag"

let Format (tagInfo : TagInfo) : string =
    let inc = tagInfo.Incremental ? ("inc", "full")
    sprintf "fullbuild-%s-%s-%s" tagInfo.Branch tagInfo.BuildNumber inc
