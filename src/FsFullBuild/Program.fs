

open CommandLineParsing




[<EntryPoint>]
let main argv = 
    let cmd = ParseCommandLine (argv |> Seq.toList)
    match cmd with
    | Usage -> DisplayUsage ()
    | InitWorkspace (path) -> FullBuild.Commands.Workspace.Workspace.InitWorkspace (path)
    | RefreshWorkspace -> FullBuild.Commands.Workspace.Workspace.RefreshWorkspace ()
    | IndexWorkspace -> FullBuild.Commands.Workspace.Workspace.IndexWorkspace ()
    | ConvertWorkspace -> FullBuild.Commands.Workspace.Workspace.ConvertProjects ()
    | OptimizeWorkspace -> FullBuild.Commands.Workspace.Workspace.Optimize ()
    | BookmarkWorkspace -> FullBuild.Commands.Workspace.Workspace.Bookmark ()
    | CheckoutWorkspace wsVersion -> FullBuild.Commands.Workspace.Workspace.CheckoutBookmark (wsVersion)
    | AddRepository (repoVcs) -> let (vcsType, vcsName, vcsUrl) = match repoVcs with
                                                                  | Types.Git (name, url) -> (FullBuild.Config.VersionControlType.Git, name, url)
                                                                  | Types.Hg (name, url) -> (FullBuild.Config.VersionControlType.Hg, name, url)
                                 FullBuild.Commands.Workspace.Workspace.AddRepo(vcsName, vcsType, vcsUrl)
    | CloneRepositories filter -> FullBuild.Commands.Workspace.Workspace.CloneRepo ( [| filter |])
    | ListRepositories -> FullBuild.Commands.Workspace.Workspace.ListRepos ()
    | AddNuGet url -> FullBuild.Commands.Packages.Packages.AddNuGet(url)
    | ListNuGets -> FullBuild.Commands.Packages.Packages.ListNuGets ()
    | ListPackages -> FullBuild.Commands.Binaries.Binaries.List ()
    | InstallPackages -> FullBuild.Commands.Packages.Packages.InstallAll ()
    | UpgradePackages -> FullBuild.Commands.Packages.Packages.UpgradePackages ()
    | UsePackage (pkgName, pkgVersion) -> FullBuild.Commands.Packages.Packages.UsePackage (pkgName, pkgVersion)
    | CheckPackages -> FullBuild.Commands.Packages.Packages.CheckPackages ()
    | InitView (vwName, vwFilter) -> FullBuild.Commands.Views.Views.Init (vwName, [| vwFilter |])
    | DropView vwName -> FullBuild.Commands.Views.Views.Delete (vwName)
    | ListViews -> FullBuild.Commands.Views.Views.List ()
    | DescribeView vwName -> FullBuild.Commands.Views.Views.Describe (vwName)
    | GraphView vwName -> FullBuild.Commands.Views.Views.Graph (vwName)
    | GenerateView vwName -> FullBuild.Commands.Views.Views.Generate (vwName)
    | BuildView vwName -> FullBuild.Commands.Views.Views.BuildView (vwName)
    | RefreshSources -> FullBuild.Commands.Workspace.Workspace.RefreshSources ()
    | ListBinaries -> FullBuild.Commands.Binaries.Binaries.List ()

    0 // return an integer exit code
