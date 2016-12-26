//   Copyright 2014-2016 Pierre Chalamet
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.

module CLI.CommandLine

open Commands
open Collections
open Anthology




type private TokenOption =
    | Debug
    | Up
    | Down
    | All
    | Bin
    | LatestBin
    | Src
    | Exclude
    | Multithread
    | Shallow
    | Default
    | Branch
    | Version
    | Rebase
    | Reset
    | View
    | Modified
    | Html
    | Alpha
    | Beta
    | App
    | Static
    | NoMultithread
    | UnusedProjects
    | Packages
    | Test

let private (|TokenOption|_|) (token : string) =
    match token with
    | "--debug" -> Some TokenOption.Debug
    | "--up" -> Some TokenOption.Up
    | "--down" -> Some TokenOption.Down
    | "--bin" -> Some TokenOption.Bin
    | "--latest-bin" -> Some TokenOption.LatestBin
    | "--src" -> Some TokenOption.Src
    | "--all" -> Some TokenOption.All
    | "--exclude" -> Some TokenOption.Exclude
    | "--mt" -> Some TokenOption.Multithread
    | "--shallow" -> Some TokenOption.Shallow
    | "--default" -> Some TokenOption.Default
    | "--branch" -> Some TokenOption.Branch
    | "--version" -> Some TokenOption.Version
    | "--rebase" -> Some TokenOption.Rebase
    | "--reset" -> Some TokenOption.Reset
    | "--view" -> Some TokenOption.View
    | "--modified" -> Some TokenOption.Modified
    | "--html" -> Some TokenOption.Html
    | "--alpha" -> Some TokenOption.Alpha
    | "--beta" -> Some TokenOption.Beta
    | "--app" -> Some TokenOption.App
    | "--static" -> Some TokenOption.Static
    | "--nomt" -> Some TokenOption.NoMultithread
    | "--unused-projects" -> Some TokenOption.UnusedProjects
    | "--packages" -> Some TokenOption.Packages
    | "--test" -> Some TokenOption.Test
    | _ -> None

type private Token =
    | Version
    | Workspace
    | Help
    | Upgrade
    | Setup
    | Init
    | Clone
    | Update
    | Build
    | Rebuild
    | Index
    | Convert
    | Push
    | Graph
    | Install
    | Simplify
    | Outdated
    | Publish
    | Pull
    | Checkout
    | Branch
    | Exec
    | Test
    | Alter
    | Open
    | Bind
    | History

    | Add
    | Drop
    | List
    | Describe

    | View
    | Repo
    | Package
    | NuGet
    | App

    | Query
    | Unused

    | Clean
    | UpdateGuids
    | Migrate

let (|FullBuildView|_|) (viewFile : string) =
    if viewFile.EndsWith(IoHelpers.Extension.View |> IoHelpers.GetExtensionString |> sprintf ".%s") && System.IO.File.Exists(viewFile) then
        Some viewFile
    else 
        None

let private (|Token|_|) (token : string) =
    match token with
    | "version" -> Some Version
    | "workspace" -> Some Workspace

    | "help" -> Some Help
    | "upgrade" -> Some Upgrade
    | "setup" -> Some Setup
    | "init" -> Some Init
    | "clone" -> Some Clone
    | "update" -> Some Update
    | "build" -> Some Build
    | "rebuild" -> Some Rebuild
    | "index" -> Some Index
    | "convert" -> Some Convert
    | "push" -> Some Push
    | "graph" -> Some Graph
    | "install" -> Some Install
    | "outdated" -> Some Outdated
    | "publish" -> Some Publish
    | "pull" -> Some Pull
    | "checkout" -> Some Checkout
    | "branch" -> Some Branch
    | "exec" -> Some Exec
    | "clean" -> Some Clean
    | "test" -> Some Test
    | "alter" -> Some Alter
    | "open" -> Some Open
    | "bind" -> Some Bind
    | "history" -> Some History

    | "add" -> Some Add
    | "drop" -> Some Drop
    | "list" -> Some List
    | "describe" -> Some Describe

    | "view" -> Some View
    | "repo" -> Some Repo
    | "package" -> Some Package
    | "nuget" -> Some NuGet
    | "app" -> Some App

    | "query" -> Some Query
    | "unused" -> Some Unused

    | "update-guids" -> Some UpdateGuids
    | "migrate" -> Some Migrate
    | _ -> None









let private (|Param|_|) (prm : string) =
    if prm.StartsWith("--") then None
    else Some prm

let private (|Params|_|) (prms : string list) =
    let hasNotParam = prms |> List.exists (fun x -> match x with
                                                    | Param _ -> false
                                                    | _ -> true)
    if hasNotParam then None
    else Some prms

let private (|BookmarkVersion|_|) version =
    match version with
    | Param _ -> Some (BookmarkVersion.from version)
    | _ -> None

let private (|ViewId|_|) view =
    match view with
    | Param _ -> Some view
    | _ -> None

let private (|RepositoryId|_|) name =
    match name with
    | Param _ -> Some (RepositoryId.from name)
    | _ -> None

let private (|ProjectId|_|) name =
    match name with
    | Param _ -> Some (ProjectId.from name)
    | _ -> None

let private (|ApplicationId|_|) name =
    match name with
    | Param _ -> Some (ApplicationId.from name)
    | _ -> None

let private (|BranchId|_|) name =
    match name with
    | Param prm -> Some (BranchId.from prm)
    | _ -> None

let private (|PublisherType|_|) name =
    match name with
    | Param _ -> Some (StringHelpers.fromString<Graph.PublisherType> name)
    | _ -> None

let private commandSetup (args : string list) =
    match args with
    | Param vcs
      :: Param masterRepository
      :: Param masterArtifacts
      :: [Param path] -> Command.SetupWorkspace { MasterRepository = masterRepository
                                                  MasterArtifacts = masterArtifacts
                                                  Type = StringHelpers.fromString<Graph.VcsType> vcs
                                                  Path = path }
    | _ -> Command.Error MainCommand.Setup

let private commandInit (args : string list) =
    match args with
    | Param vcs
      :: Param masterRepository
      :: [Param path] -> Command.InitWorkspace { MasterRepository = masterRepository
                                                 Type = StringHelpers.fromString<Graph.VcsType> vcs
                                                 Path = path }
    | _ -> Command.Error MainCommand.Init


let rec private commandExec (all : bool) (args : string list) =
    match args with
    | TokenOption TokenOption.All
      :: tail -> tail |> commandExec true
    | [Param cmd] -> Command.Exec { Command = cmd; All = all }
    | _ -> Command.Error MainCommand.Exec

let rec private commandTest (excludes : string list) (args : string list) =
    match args with
    | TokenOption TokenOption.Exclude
      :: Param category
      :: tail -> tail |> commandTest (category :: excludes)
    | [] -> Command.Error MainCommand.Test
    | Params filters -> Command.TestAssemblies { Filters = set filters; Excludes = set excludes }
    | _ -> Command.Error MainCommand.Test


let rec private commandIndex (args : string list) =
    match args with
    | [] -> Command.Error MainCommand.Index
    | Params filters -> Command.IndexRepositories { Filters = set filters }
    | _ -> Command.Error MainCommand.Index

let private commandConvert (args : string list) =
    match args with
    | [] -> Command.Error MainCommand.Convert
    | Params filters -> Command.ConvertRepositories { Filters = set filters }
    | _ -> Command.Error MainCommand.Convert

let rec private commandClone (shallow : bool) (all : bool) (mt : bool) (args : string list) =
    match args with
    | TokenOption TokenOption.Shallow
      :: tail -> tail |> commandClone true all mt
    | TokenOption TokenOption.All
      :: tail -> tail |> commandClone shallow true mt
    | TokenOption TokenOption.Multithread
      :: tail -> printfn "WARNING: clone --mt is deprecated"
                 tail |> commandClone shallow all true
    | TokenOption TokenOption.NoMultithread
      :: tail -> tail |> commandClone shallow all false
    | [] -> Command.Error MainCommand.CloneRepository
    | Params filters -> Command.CloneRepositories { Filters = set filters; Shallow = shallow; All = all; Multithread = mt }
    | _ -> Command.Error MainCommand.CloneRepository



let rec private commandGraph (all : bool) (args : string list) =
    match args with
    | TokenOption TokenOption.All
      :: tail -> tail |> commandGraph true
    | [ViewId name] -> Command.GraphView { Name = name ; All = all }
    | _ -> Command.Error MainCommand.GraphView

let rec private commandPublish (mt : bool) view (args : string list) =
    match args with
    | [] -> Command.Error MainCommand.PublishApp
    | TokenOption TokenOption.Multithread
      :: tail -> printfn "WARNING: publish --mt is deprecated"
                 tail |> commandPublish true view
    | TokenOption TokenOption.NoMultithread
      :: tail -> tail |> commandPublish false view
    | TokenOption TokenOption.View
      :: ViewId name
      :: tail -> tail |> commandPublish mt (Some name)
    | Params filters -> Command.PublishApplications {View = view; Filters = filters; Multithread = mt}
    | _ -> Command.Error MainCommand.PublishApp


let rec private commandBuild (config : string) (clean : bool) (multithread : bool) (version : string option) (args : string list) =
    match args with
    | TokenOption TokenOption.Version
      :: Param ver
      :: tail -> tail |> commandBuild config clean multithread (Some ver)
    | TokenOption TokenOption.Debug
      :: tail -> tail |> commandBuild "Debug" clean multithread version
    | TokenOption TokenOption.Multithread
      :: tail -> tail |> commandBuild config clean true version
    | [] -> Command.BuildView { Name = None ; Config = config; Clean = clean; Multithread = multithread; Version = version }
    | [ViewId name] -> Command.BuildView { Name = Some name ; Config = config; Clean = clean; Multithread = multithread; Version = version }
    | _ -> Command.Error MainCommand.BuildView

let private commandCheckout (args : string list) =
    match args with
    | [version] -> Command.CheckoutWorkspace {Version = version}
    | _ -> Command.Error MainCommand.Checkout

let private commandBranch (args : string list) =
    match args with
    |  [version] -> Command.BranchWorkspace {Branch = Some version}
    | [] -> Command.BranchWorkspace {Branch = None}
    | _ -> Command.Error MainCommand.Branch

let rec private commandPush (all : bool) (args : string list) =
    match args with
    | TokenOption TokenOption.All :: tail -> tail |> commandPush true
    | [Param buildNumber] -> Command.PushWorkspace {BuildNumber = buildNumber; Incremental = not all }
    | _ -> Command.Error MainCommand.Push

let rec private commandPull (src : bool) (bin : bool) (rebase : bool) (multithread : bool) (view : string option) (args : string list) =
    match args with
    | TokenOption TokenOption.Src :: tail -> tail |> commandPull true false rebase multithread view
    | TokenOption TokenOption.Bin :: tail -> tail |> commandPull false true rebase multithread view
    | TokenOption TokenOption.Rebase :: tail -> tail |> commandPull src bin true multithread view
    | TokenOption TokenOption.NoMultithread :: tail -> tail |> commandPull src bin rebase false view
    | TokenOption TokenOption.View :: ViewId name :: tail -> tail |> commandPull true true rebase multithread (Some name)
    | [] -> Command.PullWorkspace { Sources = src ; Bin = bin; Rebase = rebase; Multithread = multithread; View = view }
    | _ -> Command.Error MainCommand.Pull

let private commandClean (args : string list) =
    match args with
    | [] -> Command.CleanWorkspace
    | _ -> Command.Error MainCommand.Clean

let private commandInstall (args : string list) =
    match args with
    | [] -> Command.InstallPackages
    | _ -> Command.Error MainCommand.InstallPackage

let private commandUpdate (args : string list) =
    match args with
    | [] -> Command.UpdatePackages
    | _ -> Command.Error MainCommand.UpdatePackage

let private commandOutdated (args : string list) =
    match args with
    | [] -> Command.OutdatedPackages
    | _ -> Command.Error MainCommand.OutdatedPackage

let rec private commandAddRepo (branch : string option) (builder : Graph.BuilderType) (args : string list) =
    match args with
    | TokenOption TokenOption.Branch
      :: Param branch
      :: tail -> tail |> commandAddRepo (Some branch) builder
    | Param name
      :: [Param url] -> Command.AddRepository { Name = name
                                                Url = url
                                                Branch = branch
                                                Builder = builder }
    | _ -> Command.Error MainCommand.AddRepository

let private commandDropRepo (args : string list) =
    match args with
    | [repo] -> Command.DropRepository repo
    | _ -> Command.Error MainCommand.DropRepository

let private commandListRepo (args : string list) =
    match args with
    | [] -> Command.ListRepositories
    | _ -> Command.Error MainCommand.ListRepository

let private commandAddNuGet (args : string list) =
    match args with
    | [Param uri] -> Command.AddNuGet (RepositoryUrl.from uri)
    | _ -> Command.Error MainCommand.AddNuGet

let private commandListNuGet (args : string list) =
    match args with
    | [] -> Command.ListNuGets
    | _ -> Command.Error MainCommand.ListNuget

let rec private commandAddView (upReferences : bool) (downReferences : bool) (modified : bool) (app : string option) (staticView : bool) (test: bool) (args : string list) =
    match args with
    | TokenOption TokenOption.Up
      :: tail -> tail |> commandAddView true downReferences modified app staticView test
    | TokenOption TokenOption.Down
      :: tail -> tail |> commandAddView upReferences true modified app staticView test
    | TokenOption TokenOption.Modified
      :: tail -> tail |> commandAddView upReferences downReferences true app staticView test
    | TokenOption TokenOption.App
      :: appFilter :: tail -> tail |> commandAddView upReferences downReferences modified (Some appFilter) staticView test
    | TokenOption TokenOption.Static
      :: tail -> tail |> commandAddView upReferences downReferences modified app true test
    | TokenOption TokenOption.Test
      :: tail -> tail |> commandAddView upReferences downReferences modified app staticView test
    | ViewId name
      :: Params filters -> Command.AddView { Name = name
                                             Filters = filters
                                             UpReferences = upReferences
                                             DownReferences = downReferences
                                             Modified = modified
                                             AppFilter = app
                                             Static = staticView
                                             Tests = test }
    | _ -> Command.Error MainCommand.AddView

let private commandDropView (args : string list) =
    match args with
    | [ViewId name] -> Command.DropView { Name = name }
    | _ -> Command.Error MainCommand.DropView

let private commandListView (args : string list) =
    match args with
    | [] -> Command.ListViews
    | _ -> Command.Error MainCommand.ListView

let private commandDescribeView (args : string list) =
    match args with
    | [ViewId name] -> Command.DescribeView { Name = name }
    | _ -> Command.Error MainCommand.DescribeView

let rec private commandAlterView (forceDefault : bool option) (forceUpReferences : bool option) (forceDownReferences : bool option) (args : string list) =
    match args with
    | TokenOption TokenOption.Default
      :: tail -> tail |> commandAlterView (Some true) forceUpReferences forceDownReferences
    | TokenOption TokenOption.Up
      :: tail -> tail |> commandAlterView forceDefault (Some true) forceDownReferences
    | TokenOption TokenOption.Down
      :: tail -> tail |> commandAlterView forceDefault forceUpReferences (Some true)
    | TokenOption TokenOption.Bin
      :: tail -> tail |> commandAlterView forceDefault (Some false) (Some false)
    | [ViewId name] -> Command.AlterView { Name = name ; Default = forceDefault; UpReferences = forceUpReferences; DownReferences = forceDownReferences }
    | _ -> Command.Error MainCommand.AlterView

let private commandOpenView (args : string list) =
    match args with
    | [ViewId name] -> Command.OpenView { Name = name }
    | _ -> Command.Error MainCommand.OpenView

let private commandAddApp (args : string list) =
    match args with
    | Param name
      :: PublisherType pub
      :: Params projects -> Command.AddApplication { Name = name; Publisher = pub; Projects = set projects }
    | _ -> Command.Error MainCommand.AddApp

let private commandDropApp (args : string list) =
    match args with
    | [ApplicationId name] -> Command.DropApplication name
    | _ -> Command.Error MainCommand.DropApp

let rec private commandListApp (version : string option) (args : string list) =
    match args with
    | TokenOption TokenOption.Version :: version :: tail -> tail |> commandListApp (Some version)
    | [] -> Command.ListApplications { Version = version }
    | _ -> Command.Error MainCommand.ListApp

let private commandListPackage (args : string list) =
    match args with
    | [] -> Command.ListPackages
    | _ -> Command.Error MainCommand.ListPackage

let private commandUpdateGuids (args : string list) =
    match args with
    | Params filters -> Command.UpdateGuids { Filters = set filters }
    | _ -> Command.Error MainCommand.UpgradeGuids

let private commandBind (args : string list) =
    match args with
    | Params filters -> Command.BindProject { Filters = set filters }
    | _ -> Command.Error MainCommand.Bind

let rec private commandHistory (html : bool) (args : string list) =
    match args with
    | TokenOption TokenOption.Html :: tail -> tail |> commandHistory true
    | [] -> Command.History { Html = html }
    | _ -> Command.Error MainCommand.History

let rec private commandUpgrade (verStatus : string) (args : string list) =
    match args with
    | [] -> Command.Upgrade "stable"
    | [TokenOption TokenOption.Alpha] -> Command.Upgrade "alpha"
    | [TokenOption TokenOption.Beta] -> Command.Upgrade "beta"
    | [Param processId] -> Command.FinalizeUpgrade (System.Int32.Parse(processId))
    | _ -> Command.Error MainCommand.Upgrade

let rec private commandQuery (project : bool) (nuget : bool) (view : string option) (args : string list) =
    match args with
    | TokenOption TokenOption.UnusedProjects :: tail -> tail |> commandQuery true nuget view
    | TokenOption TokenOption.Packages :: tail -> tail |> commandQuery project true view
    | TokenOption TokenOption.View 
      :: ViewId viewName :: tail -> tail |> commandQuery project true (Some viewName)
    | [] when project || nuget -> Command.Query { UnusedProjects = project 
                                                  UsedPackages = nuget 
                                                  View = view }
    | _ -> Command.Error MainCommand.Query


let Parse (args : string list) : Command =
    match args with
    | [Token Token.Version] -> Command.Version
    | [Token Token.Help] -> Command.Usage
    | Token Token.Upgrade :: cmdArgs -> cmdArgs |> commandUpgrade "stable"
    | Token Token.Setup :: cmdArgs -> cmdArgs |> commandSetup
    | Token Token.Init :: cmdArgs -> cmdArgs |> commandInit
    | Token Token.Exec :: cmdArgs -> cmdArgs |> commandExec false
    | Token Token.Test :: cmdArgs -> cmdArgs |> commandTest []
    | Token Token.Index :: cmdArgs -> cmdArgs |> commandIndex
    | Token Token.Convert :: cmdArgs -> cmdArgs |> commandConvert
    | Token Token.Clone :: cmdArgs -> cmdArgs |> commandClone false false true
    | Token Token.Graph :: cmdArgs -> cmdArgs |> commandGraph false
    | Token Token.Publish :: cmdArgs -> cmdArgs |> commandPublish true None
    | Token Token.Build :: cmdArgs -> cmdArgs |> commandBuild "Release" false false None
    | Token Token.Rebuild :: cmdArgs -> cmdArgs |> commandBuild "Release" true false None
    | Token Token.Checkout :: cmdArgs -> cmdArgs |> commandCheckout
    | Token Token.Branch :: cmdArgs -> cmdArgs |> commandBranch
    | Token Token.Push :: cmdArgs -> cmdArgs |> commandPush false
    | Token Token.Pull :: cmdArgs -> cmdArgs |> commandPull true true false true None
    | Token Token.Clean :: cmdArgs -> cmdArgs |> commandClean
    | Token Token.Bind :: cmdArgs -> cmdArgs |> commandBind
    | Token Token.History :: cmdArgs -> cmdArgs |> commandHistory false

    | Token Token.Install :: cmdArgs -> cmdArgs |> commandInstall
    | Token Token.Update :: Token Token.Package :: cmdArgs -> cmdArgs |> commandUpdate
    | Token Token.Outdated :: Token Token.Package :: cmdArgs -> cmdArgs |> commandOutdated
    | Token Token.List :: Token Token.Package :: cmdArgs -> cmdArgs |> commandListPackage

    | Token Token.Add :: Token Token.Repo :: cmdArgs -> cmdArgs |> commandAddRepo None Graph.BuilderType.MSBuild
    | Token Token.Drop :: Token Token.Repo :: cmdArgs -> cmdArgs |> commandDropRepo
    | Token Token.List :: Token Token.Repo :: cmdArgs -> cmdArgs |> commandListRepo

    | Token Token.Add :: Token Token.NuGet :: cmdArgs -> cmdArgs |> commandAddNuGet
    | Token Token.List :: Token Token.NuGet :: cmdArgs -> cmdArgs |> commandListNuGet

    | Token Token.View :: cmdArgs -> cmdArgs |> commandAddView false false false None false false
    | Token Token.Drop :: Token Token.View :: cmdArgs -> cmdArgs |> commandDropView
    | Token Token.List :: Token Token.View :: cmdArgs -> cmdArgs |> commandListView
    | Token Token.Describe :: Token Token.View :: cmdArgs -> cmdArgs |> commandDescribeView
    | Token Token.Alter :: Token Token.View :: cmdArgs -> cmdArgs |> commandAlterView None None None
    | Token Token.Open :: cmdArgs -> cmdArgs |> commandOpenView

    | Token Token.Add :: Token Token.App :: cmdArgs -> cmdArgs |> commandAddApp
    | Token Token.Drop :: Token Token.App :: cmdArgs -> cmdArgs |> commandDropApp
    | Token Token.List :: Token Token.App :: cmdArgs -> cmdArgs |> commandListApp None

    | Token Token.Query :: cmdArgs -> cmdArgs |> commandQuery false false None

    | Token Token.UpdateGuids :: cmdArgs -> cmdArgs |> commandUpdateGuids
    | FullBuildView viewFile :: [] -> Command.FullBuildView { FilePath = viewFile }
    | _ -> Command.Error MainCommand.Unknown


let IsDebug (args : string seq) : (bool * string seq) =
    if (args <> Seq.empty && args |> Seq.head = "--debug") then
        let newArgs = args |> Seq.skip(1)
        (true, newArgs)
    else
        (false, args)


let VersionContent() =
    let version = Env.FullBuildVersion()
    let fbVersion = sprintf "full-build %s" (version.ToString())

    [
        fbVersion
        ""
        "Please refer to enclosed LICENSE.txt for licensing terms."
        ""
        "Copyright 2014-2016 Pierre Chalamet"
        ""
        @"Licensed under the Apache License, Version 2.0 (the ""License"");"
        "you may not use this file except in compliance with the License."
        "You may obtain a copy of the License at"
        ""
        "    http://www.apache.org/licenses/LICENSE-2.0"
        ""
        "Unless required by applicable law or agreed to in writing, software"
        @"distributed under the License is distributed on an ""AS IS"" BASIS,"
        "WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied."
        "See the License for the specific language governing permissions and"
        "limitations under the License."
    ]

let UsageContent() =
    let content = [
        MainCommand.Usage, "help : display this help"
        MainCommand.Version, "version : display full-build version"
        MainCommand.Setup, "setup <git|gerrit> <master-repository> <master-artifacts> <local-path> : setup a new environment in given path"
        MainCommand.Init, "init <master-repository> <local-path> : initialize a new workspace in given path"
        MainCommand.CloneRepository, "clone [--nomt] [--shallow] [--all] <repoId-wildcard>+ : clone repositories using provided wildcards"
        MainCommand.Checkout, "checkout <version> : checkout workspace to version"
        MainCommand.Branch, "branch [<branch>] : switch to branch"
        MainCommand.InstallPackage, "install : install packages"
        MainCommand.AddView, "view [--down] [--up] [--modified] [--app <app-wildcard>] [--static] [--test] <viewId> <viewId-wildcard>+ : add repositories to view"
        MainCommand.OpenView, "open <viewId> : open view with your favorite ide"
        MainCommand.BuildView, "build [--mt] [--debug] [--version <version>] [<viewId>] : build view"
        MainCommand.RebuildView, "rebuild [--mt] [--debug] [--version <version>] [<viewId>] : rebuild view (clean & build)"
        MainCommand.Test, "test [--exclude <category>]* <viewId-wildcard>+ : test assemblies (match repository/project)"
        MainCommand.GraphView, "graph [--all] <viewId> : graph view content (project, packages, assemblies)"
        MainCommand.Exec, "exec [--all] <cmd> : execute command for each repository (variables: FB_NAME, FB_PATH, FB_URL, FB_WKS)"
        MainCommand.Index, "index <repoId-wildcard>+ : index repositories"
        MainCommand.Convert, "convert <repoId-wildcard> : convert projects in repositories"
        MainCommand.Pull, "pull [--src|--bin|--latest-bin] [--nomt] [--rebase] [--view <viewId>]: update to latest version - rebase if requested (ff is default)"
        MainCommand.Push, "push [--all] <buildNumber> : push a baseline from current repositories version and display version"
        MainCommand.PublishApp, "publish [--nomt] [--view <viewId>] <appId-wildcard> : publish application"
        MainCommand.Bind, "bind <projectId-wildcard>+ : update bindings"
        MainCommand.Clean, "clean : DANGER! reset and clean workspace (interactive command)"
        MainCommand.History, "history [--html] : display history since last baseline"
        MainCommand.Upgrade, "upgrade [--alpha|--beta]: upgrade full-build to latest available version"
        MainCommand.Unknown, ""
        MainCommand.UpdatePackage, "update package : update packages"
        MainCommand.OutdatedPackage, "outdated package : display outdated packages"
        MainCommand.ListPackage, "list package : list packages"
        MainCommand.Unknown, ""
        MainCommand.AddRepository, "add repo [--branch <branch>] <repoId> <repo-uri> : declare a new repository"
        MainCommand.DropRepository, "drop repo <repoId> : drop repository"
        MainCommand.ListRepository, "list repo : list repositories"
        MainCommand.Unknown, ""
        MainCommand.AddNuGet, "add nuget <nuget-uri> : add nuget uri"
        MainCommand.ListNuget, "list nuget : list NuGet feeds"
        MainCommand.Unknown, ""
        MainCommand.DropView, "drop view <viewId> : drop view"
        MainCommand.ListView, "list view : list views"
        MainCommand.DescribeView, "describe view <name> : describe view"
        MainCommand.AlterView, "alter view [--default] [--up|--bin] [--down|--bin] <viewId> : alter view"
        MainCommand.Unknown, ""
        MainCommand.AddApp, "add app <appId> <copy|zip> <projectId>+ : create new application from given project ids"
        MainCommand.DropApp, "drop app <appId> : drop application"
        MainCommand.ListApp, "list app [--version <versionId>] : list applications"
        MainCommand.Unknown, ""
        MainCommand.Query, "query <--unused-projects|--packages> [--view <viewId>] : query items"
        MainCommand.Unknown, ""
        MainCommand.UpgradeGuids, "update-guids : DANGER! change guids of all projects in given repository (interactive command)" ]

    content



let PrintUsage (what : MainCommand) =
    let lines = UsageContent () |> Seq.filter (fun (cmd, _) -> cmd = what || what = MainCommand.Unknown)
                                |> Seq.map (fun (_, desc) -> desc)

    printfn "Usage:"
    for line in lines do
        printfn "  %s" line

let PrintVersion () =
    VersionContent() |> Seq.iter (fun x -> printfn "%s" x)
