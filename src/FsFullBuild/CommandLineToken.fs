module CommandLineToken


type Token =
    | Workspace
    | View
    | Help
    | Create
    | Clone
    | Repo
    | Package
    | Update
    | Index
    | Drop
    | Build
    | Convert
    | Add
    | List
    | Bookmark
    | Checkout
    | NuGet
    | Graph
    | Check
    | Upgrade
    | Unknown
 
let (|Token|) (token : string) =
    match token with 
    | "workspace" -> Workspace
    | "view" -> View
    | "help" -> Help
    | "create" -> Create
    | "clone" -> Clone
    | "repo" -> Repo
    | "package" -> Package
    | "update" -> Update
    | "index" -> Index
    | "drop" -> Drop
    | "build" -> Build
    | "convert" -> Convert
    | "add" -> Add
    | "list" -> List
    | "bookmark" -> Bookmark
    | "checkout" -> Checkout
    | "nuget" -> NuGet
    | "graph" -> Graph
    | "check" -> Check
    | "upgrade" -> Upgrade
    | _ -> Unknown
