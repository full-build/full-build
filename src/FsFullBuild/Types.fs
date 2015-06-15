module Types

type VcsType = 
    | Git
    | Hg

type Repository = 
    {
        Vcs : VcsType
        Name : string
        Url : string
    }

type Package =
    {
        Name : string
        Version : string
    }

let (|ToRepository|) (vcsType : string, vcsUrl : string, vcsName : string) = 
    let vcs = match vcsType with
              | "git" -> Git
              | "hg" -> Hg
              | _ -> failwith (sprintf "Unknown vcs type %A" vcsType)
    { Vcs = vcs; Name = vcsName; Url = vcsUrl }
