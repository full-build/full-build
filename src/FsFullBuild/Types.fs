module Types

type RelativePath = System.IO.DirectoryInfo

type NameFilter = string

type Url = string

type Name = string

type Vcs = 
    | Git of Name * Url
    | Hg of Name * Url

type WorkspaceVersion = string

type PackageVersion = string

let (|ToRelativePath|) input = 
    let pathInfo = new System.IO.DirectoryInfo(input)
    pathInfo

let (|ToNameFilter|) input = input
let (|ToUrl|) (input : string) = input
let (|ToName|) input = input

let (|ToVcs|) (vcsType : string, vcsUrl : string, vcsName : string) = 
    let (ToUrl url) = vcsUrl
    let (ToName name) = vcsName
    match vcsType with
    | "git" -> Git(name, url)
    | "hg" -> Hg(name, url)
    | _ -> failwith "unknown vcs type "

let (|ToWorkspaceVersion|) (input : string) = input