module CommandLineParsing
open Types
open CommandLineToken

type InitWorkspace =
    {
        Name : string
    }

type CheckoutWorkspace =
    {
        Version : string
    }

type CloneRepositories =
    {
        Filters : string list
    }

type NuGetUrl =
    {
        Url : string
    }

type InitView =
    {
        Name : string
        Filters : string list
    }

type ViewName =
    {
        Name : string
    }

type Command =
    | Usage
    | InitWorkspace of InitWorkspace
    | RefreshWorkspace
    | IndexWorkspace
    | ConvertWorkspace
    | OptimizeWorkspace
    | BookmarkWorkspace
    | CheckoutWorkspace of CheckoutWorkspace
    | AddRepository of Repository
    | CloneRepositories of CloneRepositories
    | ListRepositories
    | AddNuGet of NuGetUrl
    | ListNuGets
    | ListPackages
    | InstallPackages
    | UpgradePackages
    | UsePackage of Package
    | CheckPackages
    | InitView of InitView
    | DropView of ViewName
    | ListViews
    | DescribeView of ViewName
    | GraphView of ViewName
    | GenerateView of ViewName
    | BuildView of ViewName    
    | RefreshSources
    | ListBinaries

let ParseWorkspace (args : string list) =
    match args with
    | [Token(Create); wsPath] -> Command.InitWorkspace {Name=wsPath}
    | [Token(Index)] -> Command.IndexWorkspace
    | [Token(Update)] -> RefreshWorkspace
    | [Token(Checkout); version ] -> Command.CheckoutWorkspace { Version=version }
    | _ -> Command.Usage

let ParseView (args : string list) =
    match args with
    | [Token (Token.Create); vwName; vwFilter] -> Command.InitView {Name=vwName; Filters=[vwFilter]} // FIXME
    | [Token (Token.Drop); vwName] -> Command.DropView {Name=vwName}
    | [Token (Token.Build); vwName] -> Command.BuildView {Name=vwName}
    | [Token (Token.Graph); vwName] -> Command.GraphView {Name=vwName}
    | _ -> Command.Usage

let ParsePackage (args : string list) =
    match args with
    | [Token(Token.List)] -> ListPackages
    | [Token(Token.Update)] -> InstallPackages
    | [Token(Token.Check)] -> CheckPackages
    | [Token(Token.Upgrade)] -> UpgradePackages
    | [Token(Token.Add); name; version] -> UsePackage {Name=name; Version=version}
    | _ -> Command.Usage

let ParseRepo (args : string list) =
    match args with
    | Token(Token.Clone) :: filters -> CloneRepositories {Filters=filters}
    | [Token(Token.Add); vcs; name; url] -> let (ToRepository repo) = (vcs, name, url)
                                            AddRepository (repo)
    | [Token(Token.List)] -> ListRepositories
    | _ -> Command.Usage




let ParseCommandLine (args : string list) : Command =
    match args with
    | head::tail -> match head with
                    | Token (Token.Help) -> Command.Usage
                    | Token (Token.Workspace) -> ParseWorkspace tail
                    | Token (Token.View) -> ParseView tail
                    | Token (Token.Package) -> ParsePackage tail
                    | Token (Token.Repo) -> ParseRepo tail
                    | _ -> Command.Usage
    | _ -> Command.Usage  

//
//let ParseCommandLine (args : string list) : Command =
//    match args with
//    | [Token(Token.Help)] -> Help
//    | [Token(Token.Workspace); Token(Token.Create); path] -> let (ToRelativePath wsPath) = path
//                                                             InitWorkspace (wsPath)
//    | [Token(Token.Workspace); Token(Token.Update)] -> RefreshWorkspace // RefreshSources
//    | [Token(Token.Workspace); Token(Token.Index)] -> IndexWorkspace
//    | [Token(Token.Workspace); Token(Token.Convert)] -> ConvertWorkspace
//    | [Token(Token.Repo); Token(Token.Clone); filter] -> let (ToNameFilter repoFilter) = filter
//                                                         CloneRepositories (repoFilter)
//    | [Token(Token.Repo); Token(Token.Add); vcs; name; url] -> let (ToVcs repoVcs) = (vcs, name, url)
//                                                               AddRepository (repoVcs)
//    | [Token(Token.Repo); Token(Token.List)] -> ListRepositories
//    //| ["optimize"; "workspace"] -> OptimizeWorkspace
//    | [Token(Token.Workspace); Token(Token.Bookmark)] -> BookmarkWorkspace
//    | [Token(Token.Workspace); Token(Token.Checkout); version] -> let (ToWorkspaceVersion wsVersion) = version
//                                                                  CheckoutWorkspace (wsVersion)
//    | [Token(Token.NuGet); Token(Token.Add); url] -> let (ToUrl ngUrl) = url
//                                                     AddNuGet (ngUrl)
//    | [Token(Token.NuGet); Token(Token.List)] -> ListNuGets
//    | [Token(Token.Package); Token(Token.List)] -> ListPackages
//    | [Token(Token.Package); Token(Token.Update)] -> InstallPackages
//    | ["check"; "packages"] -> CheckPackages
//    | ["upgrade"; "packages"] -> UpgradePackages
//    | ["use"; "packages"; name; version] -> let (ToName pkgName) = name
//                                            let (ToWorkspaceVersion wsVersion) = version
//                                            UsePackage (pkgName, wsVersion)
//    | ["init"; "view"; name; filter] -> let (ToName vwName) = name
//                                        let (ToNameFilter vwFilter) = filter
//                                        InitView (vwName, vwFilter)
//    | ["drop"; "view"; name] -> let (ToName vwName) = name
//                                DropView (vwName)
//    | ["list"; "views"] -> ListViews
//    | ["describe"; "view"; name] -> let (ToName vwName) = name
//                                    DescribeView (vwName)
//    | ["graph"; "view"; name] -> let (ToName vwName) = name
//                                 GraphView (vwName)
//    | ["generate"; "view"; name] -> let (ToName vwName) = name
//                                    GenerateView (vwName)
//    | ["build"; "view"; name] -> let (ToName vwName) = name
//                                 BuildView (vwName)
//    | ["list"; "binaries"] -> ListBinaries
//    | _ -> Usage

let DisplayUsage () = 
    printfn "Usage: TBD"

let DisplayHelp () =
    printfn "Help : TBD"
