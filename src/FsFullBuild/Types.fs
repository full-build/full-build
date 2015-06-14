module Types

type WorkspacePath = string

type NameFilter = string

type Url = string

type Name = string

type Vcs = 
    | Git
    | Hg

type Repository = Vcs * Name * Url

type WorkspaceVersion = string

type PackageVersion = string

let (|ToWorkspacePath|) (path : string) = 
    path

let (|ToNameFilter|) input = input
let (|ToUrl|) (input : string) = input
let (|ToName|) input = input

let (|ToRepository|) (vcsType : string, vcsUrl : string, vcsName : string) = 
    let (ToUrl url) = vcsUrl
    let (ToName name) = vcsName
    match vcsType with
    | "git" -> (Git, name, url)
    | "hg" -> (Hg, name, url)
    | _ -> failwith "unknown vcs type "

let (|ToWorkspaceVersion|) (input : string) = input
