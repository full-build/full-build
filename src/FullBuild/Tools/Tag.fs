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
    | [FullBuild; branch; version; Full] -> { TagInfo.Branch = branch; TagInfo.BuildNumber = version; TagInfo.Incremental = true }
    | _ -> failwithf "Unknown tag"

//    if (items.Length <> 4 || items.[0] <> "fullbuild" || (items.[3] <> "inc" && items.[3] <> "full")) then failwithf "Unknown tag"
//    let branch = items.[1]
//    let buildNumber = items.[2]
//    let buildType = items.[3]
//    { TagInfo.Branch = branch
//      TagInfo.BuildNumber = buildNumber
//      TagInfo.Incremental = buildType = "inc" }

let Format (tagInfo : TagInfo) : string =
    let inc = tagInfo.Incremental ? ("incremental", "full")
    sprintf "fullbuild-%s-%s-%s" tagInfo.Branch tagInfo.BuildNumber inc
