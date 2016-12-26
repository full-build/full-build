module Tag
open Collections

[<RequireQualifiedAccess>]
type TagInfo =
    { Branch : string
      BuildNumber : string 
      Incremental : bool }

let Parse (tag : string) : TagInfo =
    let items = tag.Split('-')
    if (items.Length <> 4 || items.[0] <> "fullbuild") then failwithf "Unknown tag"
    let branch = items.[1]
    let buildNumber = items.[2]
    let buildType = items.[3]
    { TagInfo.Branch = branch
      TagInfo.BuildNumber = buildNumber
      TagInfo.Incremental = buildType = "incremental" }

let Format (tagInfo : TagInfo) : string =
    let inc = tagInfo.Incremental ? ("incremental", "full")
    sprintf "fullbuild-%s-%s-%s" tagInfo.Branch tagInfo.BuildNumber inc
