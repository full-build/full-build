﻿//   Copyright 2014-2016 Pierre Chalamet
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




type TokenOption =
    | Debug
    | All
    | Bin
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

let (|TokenOption|_|) (token : string) =
    match token with
    | "--debug" -> Some TokenOption.Debug
    | "--all" -> Some TokenOption.All
    | "--bin" -> Some TokenOption.Bin
    | "--src" -> Some TokenOption.Src
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
    | _ -> None


type Token =
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
    | Pending
    | List
    | Describe

    | View
    | Repo
    | Package
    | NuGet
    | App

    | Clean
    | UpdateGuids
    | Migrate


let (|Token|_|) (token : string) =
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
    | "pending" -> Some Pending
    | "list" -> Some List
    | "describe" -> Some Describe

    | "view" -> Some View
    | "repo" -> Some Repo
    | "package" -> Some Package
    | "nuget" -> Some NuGet
    | "app" -> Some App

    | "update-guids" -> Some UpdateGuids
    | "migrate" -> Some Migrate
    | _ -> None









let (|Param|_|) (prm : string) =
    if prm.StartsWith("--") then None
    else Some prm

let (|Params|_|) (prms : string list) =
    let hasNotParam = prms |> List.exists (fun x -> match x with
                                                    | Param _ -> false
                                                    | _ -> true)
    if hasNotParam then None
    else Some prms

let (|BookmarkVersion|_|) version =
    match version with
    | Param _ -> Some (BookmarkVersion.from version)
    | _ -> None

let (|ViewId|_|) view =
    match view with
    | Param _ -> Some view
    | _ -> None

let (|RepositoryId|_|) name =
    match name with
    | Param _ -> Some (RepositoryId.from name)
    | _ -> None

let (|ProjectId|_|) name =
    match name with
    | Param _ -> Some (ProjectId.from name)
    | _ -> None

let (|ApplicationId|_|) name =
    match name with
    | Param _ -> Some (ApplicationId.from name)
    | _ -> None

let (|BranchId|_|) name =
    match name with
    | Param prm -> Some (BranchId.from prm)
    | _ -> None

let (|PublisherType|_|) name =
    match name with
    | Param _ -> Some (StringHelpers.fromString<Graph.PublisherType> name)
    | _ -> None

let commandSetup (args : string list) =
    match args with
    | Param vcs
      :: Param masterRepository
      :: Param masterArtifacts
      :: [Param path] -> Command.SetupWorkspace { MasterRepository = masterRepository
                                                  MasterArtifacts = masterArtifacts
                                                  Type = StringHelpers.fromString<Graph.VcsType> vcs
                                                  Path = path }
    | _ -> Command.Error MainCommand.Setup

let commandInit (args : string list) =
    match args with
    | Param vcs
      :: Param masterRepository
      :: [Param path] -> Command.InitWorkspace { MasterRepository = masterRepository
                                                 Type = StringHelpers.fromString<Graph.VcsType> vcs
                                                 Path = path }
    | _ -> Command.Error MainCommand.Init


let rec commandExec (all : bool) (args : string list) =
    match args with
    | TokenOption TokenOption.All
      :: tail -> tail |> commandExec true
    | [Param cmd] -> Command.Exec { Command = cmd; All = all }
    | _ -> Command.Error MainCommand.Exec

let rec commandTest (excludes : string list) (args : string list) =
    match args with
    | TokenOption TokenOption.Exclude
      :: Param category
      :: tail -> tail |> commandTest (category :: excludes)
    | [] -> Command.Error MainCommand.Test
    | Params filters -> Command.TestAssemblies { Filters = set filters; Excludes = set excludes }
    | _ -> Command.Error MainCommand.Test


let rec commandIndex (args : string list) =
    match args with
    | [] -> Command.Error MainCommand.Index
    | Params filters -> Command.IndexRepositories { Filters = set filters }
    | _ -> Command.Error MainCommand.Index

let commandConvert (args : string list) =
    match args with
    | [] -> Command.Error MainCommand.Convert
    | Params filters -> Command.ConvertRepositories { Filters = set filters }
    | _ -> Command.Error MainCommand.Convert

let rec commandClone (shallow : bool) (all : bool) (mt : bool) (args : string list) =
    match args with
    | TokenOption TokenOption.Shallow
      :: tail -> tail |> commandClone true all mt
    | TokenOption TokenOption.All
      :: tail -> tail |> commandClone shallow true mt
    | TokenOption TokenOption.Multithread
      :: tail -> tail |> commandClone shallow all true
    | [] -> Command.Error MainCommand.CloneRepository
    | Params filters -> Command.CloneRepositories { Filters = set filters; Shallow = shallow; All = all; Multithread = mt }
    | _ -> Command.Error MainCommand.CloneRepository



let rec commandGraph (all : bool) (args : string list) =
    match args with
    | TokenOption TokenOption.View
      :: tail -> tail |> commandGraph true
    | [ViewId name] -> Command.GraphView { Name = name ; All = all }
    | _ -> Command.Error MainCommand.GraphView

let rec commandPublish (mt : bool) view (args : string list) =
    match args with
    | [] -> Command.Error MainCommand.PublishApp
    | TokenOption TokenOption.Multithread
      :: tail -> tail |> commandPublish true view
    | TokenOption TokenOption.View
      :: ViewId name
      :: tail -> tail |> commandPublish mt (Some name)
    | Params filters -> Command.PublishApplications {View = view; Filters = filters; Multithread = mt}
    | _ -> Command.Error MainCommand.PublishApp


let rec commandBuild (config : string) (clean : bool) (multithread : bool) (version : string option) (args : string list) =
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

let commandCheckout (args : string list) =
    match args with
    | [version] -> Command.CheckoutWorkspace {Version = version}
    | _ -> Command.Error MainCommand.Checkout

let commandBranch (args : string list) =
    match args with
    |  [version] -> Command.BranchWorkspace {Branch = Some version}
    | [] -> Command.BranchWorkspace {Branch = None}
    | _ -> Command.Error MainCommand.Branch

let rec commandPush (branch : string option) (all : bool) (args : string list) =
    match args with
    | TokenOption TokenOption.Branch
      :: Param branch
      :: tail -> tail |> commandPush (Some branch) all
    | TokenOption TokenOption.All
      :: tail -> tail |> commandPush branch true
    | [Param buildNumber] -> Command.PushWorkspace {Branch = branch; BuildNumber = buildNumber; Incremental = not all }
    | _ -> Command.Error MainCommand.Push

let rec commandPull (src : bool) (bin : bool) (rebase : bool) (view : string option) (args : string list) =
    match args with
    | TokenOption TokenOption.Src
      :: tail -> tail |> commandPull true false rebase view
    | TokenOption TokenOption.Bin
      :: tail -> tail |> commandPull false true rebase view
    | TokenOption TokenOption.Rebase
      :: tail -> tail |> commandPull src bin true view
    | TokenOption TokenOption.View
      :: ViewId name
      :: tail -> tail |> commandPull true true rebase (Some name)
    | [] -> Command.PullWorkspace { Src = src ; Bin = bin; Rebase = rebase; View = view }
    | _ -> Command.Error MainCommand.Pull

let commandClean (args : string list) =
    match args with
    | [] -> Command.CleanWorkspace
    | _ -> Command.Error MainCommand.Clean

let commandInstall (args : string list) =
    match args with
    | [] -> Command.InstallPackages
    | _ -> Command.Error MainCommand.InstallPackage

let commandUpdate (args : string list) =
    match args with
    | [] -> Command.UpdatePackages
    | _ -> Command.Error MainCommand.UpdatePackage

let commandOutdated (args : string list) =
    match args with
    | [] -> Command.OutdatedPackages
    | _ -> Command.Error MainCommand.OutdatedPackage

let rec commandAddRepo (branch : BranchId option) (builder : BuilderType) (args : string list) =
    match args with
    | TokenOption TokenOption.Branch
      :: BranchId branch
      :: tail -> tail |> commandAddRepo (Some branch) builder
    | Param name
      :: [Param url] -> Command.AddRepository { Repo = RepositoryId.from name
                                                Url = RepositoryUrl.from url
                                                Branch = branch
                                                Builder = builder }
    | _ -> Command.Error MainCommand.AddRepository

let commandDropRepo (args : string list) =
    match args with
    | [repo] -> Command.DropRepository repo
    | _ -> Command.Error MainCommand.DropRepository

let commandListRepo (args : string list) =
    match args with
    | [] -> Command.ListRepositories
    | _ -> Command.Error MainCommand.ListRepository

let commandAddNuGet (args : string list) =
    match args with
    | [Param uri] -> Command.AddNuGet (RepositoryUrl.from uri)
    | _ -> Command.Error MainCommand.AddNuGet

let commandListNuGet (args : string list) =
    match args with
    | [] -> Command.ListNuGets
    | _ -> Command.Error MainCommand.ListNuget

let rec commandAddView (references : bool) (referencedBy : bool) (modified : bool) (args : string list) =
    match args with
    | TokenOption TokenOption.Src
      :: tail -> tail |> commandAddView true referencedBy modified
    | TokenOption TokenOption.All
      :: tail -> tail |> commandAddView references true modified
    | TokenOption TokenOption.Modified
      :: tail -> tail |> commandAddView references referencedBy true
    | ViewId name
      :: Params filters -> Command.AddView { Name = name; Filters = filters; References = references; ReferencedBy = referencedBy; Modified = modified }
    | _ -> Command.Error MainCommand.AddView

let commandDropView (args : string list) =
    match args with
    | [ViewId name] -> Command.DropView { Name = name }
    | _ -> Command.Error MainCommand.DropView

let commandListView (args : string list) =
    match args with
    | [] -> Command.ListViews
    | _ -> Command.Error MainCommand.ListView

let commandDescribeView (args : string list) =
    match args with
    | [ViewId name] -> Command.DescribeView { Name = name }
    | _ -> Command.Error MainCommand.DescribeView

let rec commandAlterView (forceDefault : bool option) (forceSrc : bool option) (forceParents : bool option) (args : string list) =
    match args with
    | TokenOption TokenOption.Default
      :: tail -> tail |> commandAlterView (Some true) forceSrc forceParents
    | TokenOption TokenOption.Src
      :: tail -> tail |> commandAlterView forceDefault (Some true) forceParents
    | TokenOption TokenOption.Bin
      :: tail -> tail |> commandAlterView forceDefault (Some false) forceParents
    | [ViewId name] -> Command.AlterView { Name = name ; Default = forceDefault; Source = forceSrc; Parents = forceParents }
    | _ -> Command.Error MainCommand.AlterView

let commandOpenView (args : string list) =
    match args with
    | [ViewId name] -> Command.OpenView { Name = name }
    | _ -> Command.Error MainCommand.OpenView

let commandAddApp (args : string list) =
    match args with
    | Param name
      :: PublisherType pub
      :: Params projects -> Command.AddApplication { Name = name; Publisher = pub; Projects = set projects }
    | _ -> Command.Error MainCommand.AddApp

let commandDropApp (args : string list) =
    match args with
    | [ApplicationId name] -> Command.DropApplication name
    | _ -> Command.Error MainCommand.DropApp

let commandListApp (args : string list) =
    match args with
    | [] -> Command.ListApplications
    | _ -> Command.Error MainCommand.ListApp

let commandListPackage (args : string list) =
    match args with
    | [] -> Command.ListPackages
    | _ -> Command.Error MainCommand.ListPackage

let commandUpdateGuids (args : string list) =
    match args with
    | Params filters -> Command.UpdateGuids { Filters = set filters }
    | _ -> Command.Error MainCommand.UpgradeGuids

let commandBind (args : string list) =
    match args with
    | Params filters -> Command.BindProject { Filters = set filters }
    | _ -> Command.Error MainCommand.Bind

let rec commandHistory (html : bool) (args : string list) =
    match args with
    | TokenOption TokenOption.Html :: tail -> tail |> commandHistory true
    | [] -> Command.History { Html = html }
    | _ -> Command.Error MainCommand.History

let commandUpgrade (args : string list) =
    match args with
    | [] -> Command.Upgrade
    | [Param processId] -> Command.FinalizeUpgrade (System.Int32.Parse(processId))
    | _ -> Command.Error MainCommand.Upgrade

let Parse (args : string list) : Command =
    match args with
    | [Token Token.Version] -> Command.Version
    | [Token Token.Help] -> Command.Usage
    | Token Token.Upgrade :: cmdArgs -> cmdArgs |> commandUpgrade
    | Token Token.Setup :: cmdArgs -> cmdArgs |> commandSetup
    | Token Token.Init :: cmdArgs -> cmdArgs |> commandInit
    | Token Token.Exec :: cmdArgs -> cmdArgs |> commandExec false
    | Token Token.Test :: cmdArgs -> cmdArgs |> commandTest []
    | Token Token.Index :: cmdArgs -> cmdArgs |> commandIndex
    | Token Token.Convert :: cmdArgs -> cmdArgs |> commandConvert
    | Token Token.Clone :: cmdArgs -> cmdArgs |> commandClone false false false
    | Token Token.Graph :: cmdArgs -> cmdArgs |> commandGraph false
    | Token Token.Publish :: cmdArgs -> cmdArgs |> commandPublish false None
    | Token Token.Build :: cmdArgs -> cmdArgs |> commandBuild "Release" false false None
    | Token Token.Rebuild :: cmdArgs -> cmdArgs |> commandBuild "Release" true false None
    | Token Token.Checkout :: cmdArgs -> cmdArgs |> commandCheckout
    | Token Token.Branch :: cmdArgs -> cmdArgs |> commandBranch
    | Token Token.Push :: cmdArgs -> cmdArgs |> commandPush None false
    | Token Token.Pull :: cmdArgs -> cmdArgs |> commandPull true true false None
    | Token Token.Clean :: cmdArgs -> cmdArgs |> commandClean
    | Token Token.Bind :: cmdArgs -> cmdArgs |> commandBind
    | Token Token.History :: cmdArgs -> cmdArgs |> commandHistory false

    | Token Token.Install :: cmdArgs -> cmdArgs |> commandInstall
    | Token Token.Update :: Token Token.Package :: cmdArgs -> cmdArgs |> commandUpdate
    | Token Token.Outdated :: Token Token.Package :: cmdArgs -> cmdArgs |> commandOutdated
    | Token Token.List :: Token Token.Package :: cmdArgs -> cmdArgs |> commandListPackage

    | Token Token.Add :: Token Token.Repo :: cmdArgs -> cmdArgs |> commandAddRepo None BuilderType.MSBuild
    | Token Token.Drop :: Token Token.Repo :: cmdArgs -> cmdArgs |> commandDropRepo
    | Token Token.List :: Token Token.Repo :: cmdArgs -> cmdArgs |> commandListRepo

    | Token Token.Add :: Token Token.NuGet :: cmdArgs -> cmdArgs |> commandAddNuGet
    | Token Token.List :: Token Token.NuGet :: cmdArgs -> cmdArgs |> commandListNuGet

    | Token Token.View :: cmdArgs -> cmdArgs |> commandAddView false false false
    | Token Token.Pending :: Token Token.View :: cmdArgs -> cmdArgs |> commandAddView true true true
    | Token Token.Drop :: Token Token.View :: cmdArgs -> cmdArgs |> commandDropView
    | Token Token.List :: Token Token.View :: cmdArgs -> cmdArgs |> commandListView
    | Token Token.Describe :: Token Token.View :: cmdArgs -> cmdArgs |> commandDescribeView
    | Token Token.Alter :: Token Token.View :: cmdArgs -> cmdArgs |> commandAlterView None None None
    | Token Token.Open :: cmdArgs -> cmdArgs |> commandOpenView

    | Token Token.Add :: Token Token.App :: cmdArgs -> cmdArgs |> commandAddApp
    | Token Token.Drop :: Token Token.App :: cmdArgs -> cmdArgs |> commandDropApp
    | Token Token.List :: Token Token.App :: cmdArgs -> cmdArgs |> commandListApp

    | Token Token.UpdateGuids :: cmdArgs -> cmdArgs |> commandUpdateGuids
    | _ -> Command.Error MainCommand.Unknown


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
        MainCommand.Setup, "setup <git|gerrit|hg> <master-repository> <master-artifacts> <local-path> : setup a new environment in given path"
        MainCommand.Init, "init <master-repository> <local-path> : initialize a new workspace in given path"
        MainCommand.CloneRepository, "clone [--mt] [--shallow] [--all] <repoId-wildcard>+ : clone repositories using provided wildcards"
        MainCommand.Checkout, "checkout <version> : checkout workspace to version"
        MainCommand.Branch, "branch [<branch>] : checkout workspace to branch"
        MainCommand.InstallPackage, "install : install packages"
        MainCommand.AddView, "view [--src] [--all] [--modified] <viewId> <viewId-wildcard>+ : add repositories to view"
        MainCommand.OpenView, "open <viewId> : open view with your favorite ide"
        MainCommand.BuildView, "build [--mt] [--debug] [--version <version>] [<viewId>] : build view"
        MainCommand.RebuildView, "rebuild [--mt] [--debug] [--version <version>] [<viewId>] : rebuild view (clean & build)"
        MainCommand.Test, "test [--exclude <category>]* <viewId-wildcard>+ : test assemblies (match repository/project)"
        MainCommand.GraphView, "graph [--all] <viewId> : graph view content (project, packages, assemblies)"
        MainCommand.Exec, "exec [--all] <cmd> : execute command for each repository (variables: FB_NAME, FB_PATH, FB_URL, FB_WKS)"
        MainCommand.Index, "index <repoId-wildcard>+ : index repositories"
        MainCommand.Convert, "convert <repoId-wildcard> : convert projects in repositories"
        MainCommand.Pull, "pull [--src|--bin] [--rebase] [--view <viewId>]: update to latest version - rebase if requested (ff is default)"
        MainCommand.Push, "push [--branch <branch>] [--all] <buildNumber> : push a baseline from current repositories version and display version"
        MainCommand.PublishApp, "publish [--mt] [--view <viewId>] <appId-wildcard> : publish application"
        MainCommand.Bind, "bind <projectId-wildcard>+ : update bindings"
        MainCommand.Clean, "clean : DANGER! reset and clean workspace (interactive command)"
        MainCommand.History, "history [--html] : display history since last baseline"
        MainCommand.Upgrade, "upgrade : upgrade full-build to latest available version"
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
        MainCommand.DropView, "pending view <viewId> : create view with modified and depending repos"
        MainCommand.DropView, "drop view <viewId> : drop view"
        MainCommand.ListView, "list view : list views"
        MainCommand.DescribeView, "describe view <name> : describe view"
        MainCommand.AlterView, "alter view [--default] [--src|--bin] [--all] <viewId> : alter view"
        MainCommand.Unknown, ""
        MainCommand.AddApp, "add app <appId> <copy|zip> <projectId>+ : create new application from given project ids"
        MainCommand.DropApp, "drop app <appId> : drop application"
        MainCommand.ListApp, "list app : list applications"
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
