

open CommandLineParsing




[<EntryPoint>]
let main argv = 
    let cmd = ParseCommandLine (argv |> Seq.toList)
    match cmd with
    | Usage -> DisplayUsage ()
    | InitWorkspace {Name=path} -> Workspace.Init path
    | RefreshWorkspace -> FullBuild.Commands.Workspace.Workspace.RefreshWorkspace ()
    | IndexWorkspace -> FullBuild.Commands.Workspace.Workspace.IndexWorkspace ()
    | ConvertWorkspace -> FullBuild.Commands.Workspace.Workspace.ConvertProjects ()
    | OptimizeWorkspace -> FullBuild.Commands.Workspace.Workspace.Optimize ()
    | BookmarkWorkspace -> FullBuild.Commands.Workspace.Workspace.Bookmark ()
    | CheckoutWorkspace {Version=wsVersion} -> FullBuild.Commands.Workspace.Workspace.CheckoutBookmark (wsVersion)
    | AddRepository (repo) -> let vcsType = match repo.Vcs with
                                            | Types.Git -> FullBuild.Config.VersionControlType.Git
                                            | Types.Hg -> FullBuild.Config.VersionControlType.Hg
                              FullBuild.Commands.Workspace.Workspace.AddRepo(repo.Name, vcsType, repo.Url)
    | CloneRepositories {Filters=filters} -> Repo.Clone filters
    | ListRepositories -> Repo.List ()
    | AddNuGet {Url=url} -> FullBuild.Commands.Packages.Packages.AddNuGet(url)
    | ListNuGets -> FullBuild.Commands.Packages.Packages.ListNuGets ()
    | ListPackages -> FullBuild.Commands.Binaries.Binaries.List ()
    | InstallPackages -> FullBuild.Commands.Packages.Packages.InstallAll ()
    | UpgradePackages -> FullBuild.Commands.Packages.Packages.UpgradePackages ()
    | UsePackage {Name=pkgName; Version=pkgVersion} -> FullBuild.Commands.Packages.Packages.UsePackage (pkgName, pkgVersion)
    | CheckPackages -> FullBuild.Commands.Packages.Packages.CheckPackages ()
    | InitView {Name=vwName; Filters=vwFilter} -> FullBuild.Commands.Views.Views.Init (vwName, vwFilter |> Seq.toArray)
    | DropView {Name=vwName} -> FullBuild.Commands.Views.Views.Delete (vwName)
    | ListViews -> FullBuild.Commands.Views.Views.List ()
    | DescribeView {Name=vwName} -> FullBuild.Commands.Views.Views.Describe (vwName)
    | GraphView {Name=vwName} -> FullBuild.Commands.Views.Views.Graph (vwName)
    | GenerateView {Name=vwName} -> FullBuild.Commands.Views.Views.Generate (vwName)
    | BuildView {Name=vwName} -> FullBuild.Commands.Views.Views.BuildView (vwName)
    | RefreshSources -> FullBuild.Commands.Workspace.Workspace.RefreshSources ()
    | ListBinaries -> FullBuild.Commands.Binaries.Binaries.List ()

    0 // return an integer exit code
