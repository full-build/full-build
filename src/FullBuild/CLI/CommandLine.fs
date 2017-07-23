//   Copyright 2014-2017 Pierre Chalamet
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
    | Alpha
    | Beta
    | App
    | Static
    | UnusedProjects
    | Packages
    | Test
    | Check
    | Full
    | Inc
    | Ref
    | Push
    | Status
    | SxS
 
let private (|TokenOption|_|) (token : string) =
    match token with
    | "--debug" -> Some TokenOption.Debug
    | "--bin" -> Some TokenOption.Bin
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
    | "--unused-projects" -> Some TokenOption.UnusedProjects
    | "--packages" -> Some TokenOption.Packages
    | "--test" -> Some TokenOption.Test
    | "--check" -> Some TokenOption.Check
    | "--full" -> Some TokenOption.Full
    | "--inc" -> Some TokenOption.Inc
    | "--ref" -> Some TokenOption.Ref
    | "--push" -> Some TokenOption.Push
    | "--status" -> Some TokenOption.Status
    | "--sxs" -> Some TokenOption.SxS
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
    | Convert
    | Graph
    | Install
    | Outdated
    | Publish
    | Push
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
    | Doctor

let (|FullBuildView|_|) (viewFile : string) =
    if viewFile.EndsWith(IoHelpers.Extension.View |> IoHelpers.GetExtensionString |> sprintf ".%s") && System.IO.File.Exists(viewFile) then
        Some viewFile
    else
        None

let private (|Token|_|) (token : string) =
    match token with
    | "version" -> Some Version
    | "wks" -> Some Workspace

    | "help" -> Some Help
    | "upgrade" -> Some Upgrade
    | "setup" -> Some Setup
    | "init" -> Some Init
    | "clone" -> Some Clone
    | "update" -> Some Update
    | "build" -> Some Build
    | "rebuild" -> Some Rebuild
    | "convert" -> Some Convert
    | "graph" -> Some Graph
    | "install" -> Some Install
    | "outdated" -> Some Outdated
    | "publish" -> Some Publish
    | "push" -> Some Push
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
    | "doctor" -> Some Doctor
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

let rec private commandSetup (sxs : bool) (args : string list) =
    match args with
    | TokenOption TokenOption.SxS :: tail -> tail |> commandSetup true
    | Param vcs :: Param masterRepository :: Param masterArtifacts :: [Param path]
        -> Command.SetupWorkspace { MasterRepository = masterRepository
                                    MasterArtifacts = masterArtifacts
                                    Type = StringHelpers.fromString<Graph.VcsType> vcs
                                    Path = path 
                                    SxS = sxs }
    | _ -> Command.Error MainCommand.Setup

let private commandInit (args : string list) =
    match args with
    | Param vcs :: Param masterRepository :: [Param path]
        -> Command.InitWorkspace { MasterRepository = masterRepository
                                   Type = StringHelpers.fromString<Graph.VcsType> vcs
                                   Path = path }
    | _ -> Command.Error MainCommand.Init


let rec private commandExec (all : bool) (args : string list) =
    match args with
    | TokenOption TokenOption.All :: tail -> tail |> commandExec true
    | [Param cmd] -> Command.Exec { Command = cmd; All = all }
    | _ -> Command.Error MainCommand.Exec

let rec private commandTest (excludes : string list) (args : string list) =
    match args with
    | TokenOption TokenOption.Exclude :: Param category :: tail -> tail |> commandTest (category :: excludes)
    | [] -> Command.Error MainCommand.Test
    | Params filters -> Command.TestAssemblies { Filters = set filters; Excludes = set excludes }
    | _ -> Command.Error MainCommand.Test


let rec private commandConvert (check : bool) (reset : bool) (args : string list) =
    match args with
    | [] -> Command.Error MainCommand.Convert
    | TokenOption TokenOption.Check :: tail -> tail |> commandConvert true reset
    | TokenOption TokenOption.Reset :: tail -> tail |> commandConvert check true
    | Params filters -> Command.ConvertRepositories { Filters = set filters; Check = check; Reset = reset }
    | _ -> Command.Error MainCommand.Convert

let private commandDoctor (args : string list ) =
    match args with
    | [] -> Command.Doctor
    | _ -> Command.Error MainCommand.Doctor

let rec private commandClone (shallow : bool) (all : bool) (args : string list) =
    match args with
    | TokenOption TokenOption.Shallow :: tail -> tail |> commandClone true all
    | TokenOption TokenOption.All :: tail -> tail |> commandClone shallow true
    | [] -> Command.Error MainCommand.Clone
    | Params filters -> Command.CloneRepositories { Filters = set filters; Shallow = shallow; All = all; Multithread = true }
    | _ -> Command.Error MainCommand.Clone



let rec private commandGraph (all : bool) (args : string list) =
    match args with
    | TokenOption TokenOption.All :: tail -> tail |> commandGraph true
    | [ViewId name] -> Command.Graph { Name = name ; All = all }
    | _ -> Command.Error MainCommand.Graph

let rec private commandPublish view version status (args : string list) =
    match args with
    | [] -> Command.Error MainCommand.Publish
    | TokenOption TokenOption.View :: ViewId name :: tail -> tail |> commandPublish (Some name) version status
    | TokenOption TokenOption.Version :: version :: tail -> tail |> commandPublish view (Some version) status
    | TokenOption TokenOption.Status :: status :: tail -> tail |> commandPublish view version (Some status)
    | Params filters -> Command.PublishApplications {View = view; Filters = filters; Multithread = true; Version = version; Status = status }
    | _ -> Command.Error MainCommand.Publish

let rec private commandPush inc (args : string list) =
    match args with
    | [] -> Command.Error MainCommand.Push
    | TokenOption TokenOption.Full :: tail -> tail |> commandPush true
    | [version] -> Command.PushWorkspace { Incremental = inc; Version = version }
    | _ -> Command.Error MainCommand.Publish


let rec private commandBuild (config : string) (clean : bool) (multithread : bool) (version : string option) (args : string list) =
    match args with
    | TokenOption TokenOption.Version :: Param ver :: tail -> tail |> commandBuild config clean multithread (Some ver)
    | TokenOption TokenOption.Debug :: tail -> tail |> commandBuild "Debug" clean multithread version
    | TokenOption TokenOption.Multithread :: tail -> tail |> commandBuild config clean true version
    | [] -> Command.BuildView { Name = None ; Config = config; Clean = clean; Multithread = multithread; Version = version }
    | [ViewId name] -> Command.BuildView { Name = Some name ; Config = config; Clean = clean; Multithread = multithread; Version = version }
    | _ -> Command.Error MainCommand.BuildView

let private commandCheckout (args : string list) =
    match args with
    | [version] -> Command.CheckoutWorkspace {Version = version}
    | _ -> Command.Error MainCommand.Checkout

let private commandBranch (args : string list) =
    match args with
    | [name] -> Command.BranchWorkspace {Branch = Some name}
    | [] -> Command.BranchWorkspace {Branch = None}
    | _ -> Command.Error MainCommand.Branch

let rec private commandPull (src : bool) (bin : bool) (rebase : bool) (view : string option) (args : string list) =
    match args with
    | TokenOption TokenOption.Src :: tail -> tail |> commandPull true false rebase view
    | TokenOption TokenOption.Bin :: tail -> tail |> commandPull false true rebase view
    | TokenOption TokenOption.Rebase :: tail -> tail |> commandPull src bin true view
    | TokenOption TokenOption.View :: ViewId name :: tail -> tail |> commandPull true true rebase (Some name)
    | [] -> Command.PullWorkspace { Sources = src ; Bin = bin; Rebase = rebase; Multithread = true; View = view }
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
    | TokenOption TokenOption.Branch :: Param branch :: tail -> tail |> commandAddRepo (Some branch) builder
    | Param name :: vcs :: Param url :: tester :: [builder] -> Command.AddRepository { Name = name
                                                                                       Url = url
                                                                                       Branch = branch
                                                                                       Vcs = StringHelpers.fromString<Graph.VcsType> vcs
                                                                                       Builder = StringHelpers.fromString<Graph.BuilderType> builder
                                                                                       Tester = StringHelpers.fromString<Graph.TestRunnerType> tester }
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
    | TokenOption TokenOption.Ref :: tail -> tail |> commandAddView true downReferences modified app staticView test
    | TokenOption TokenOption.Src :: tail -> tail |> commandAddView upReferences true modified app staticView test
    | TokenOption TokenOption.Modified :: tail -> tail |> commandAddView upReferences downReferences true app staticView test
    | TokenOption TokenOption.App :: appFilter :: tail -> tail |> commandAddView upReferences downReferences modified (Some appFilter) staticView test
    | TokenOption TokenOption.Static :: tail -> tail |> commandAddView upReferences downReferences modified app true test
    | TokenOption TokenOption.Test :: tail -> tail |> commandAddView upReferences downReferences modified app staticView test
    | ViewId name :: Params filters -> Command.AddView { Name = name
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
    | TokenOption TokenOption.Default :: tail -> tail |> commandAlterView (Some true) forceUpReferences forceDownReferences
    | TokenOption TokenOption.Ref :: tail -> tail |> commandAlterView forceDefault (Some true) forceDownReferences
    | TokenOption TokenOption.Src :: tail -> tail |> commandAlterView forceDefault forceUpReferences (Some true)
    | TokenOption TokenOption.Bin :: tail -> tail |> commandAlterView forceDefault (Some false) (Some false)
    | [ViewId name] -> Command.AlterView { Name = name ; Default = forceDefault; UpReferences = forceUpReferences; DownReferences = forceDownReferences }
    | _ -> Command.Error MainCommand.AlterView

let private commandOpenView (args : string list) =
    match args with
    | [ViewId name] -> Command.OpenView { Name = name }
    | _ -> Command.Error MainCommand.OpenView

let private commandAddApp (args : string list) =
    match args with
    | Param name :: PublisherType pub :: Params projects -> Command.AddApplication { Name = name; Publisher = pub; Projects = set projects }
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

let rec private commandQuery (project : bool) (nuget : bool) (refs : bool) (view : string option) (src : RepositoryId option) (dst : RepositoryId option) (args : string list) =
    match args with
    | TokenOption TokenOption.UnusedProjects :: tail -> tail |> commandQuery true nuget refs view None None
    | TokenOption TokenOption.Packages :: tail -> tail |> commandQuery project true refs view None None
    | TokenOption TokenOption.View :: ViewId viewName :: tail -> tail |> commandQuery project true refs (Some viewName) None None
    | TokenOption TokenOption.Ref :: RepositoryId src :: RepositoryId dst :: tail -> tail |> commandQuery project nuget true view (Some src) (Some dst)
    | [] when project || nuget || refs -> Command.Query { UnusedProjects = project
                                                          UsedPackages = nuget
                                                          References = refs
                                                          View = view 
                                                          Source = src
                                                          Destination = dst }
    | _ -> Command.Error MainCommand.Query


let private commandHelp (args : string list) =
    let cmd = match args with
              | Token Token.Workspace :: _ -> MainCommand.Workspace
              | Token Token.Version :: _ -> MainCommand.Version
              | Token Token.Help :: _ -> MainCommand.Usage
              | Token Token.Update :: _ -> MainCommand.Upgrade
              | Token Token.Setup :: _ -> MainCommand.Setup
              | Token Token.Init :: _ -> MainCommand.Init
              | Token Token.Exec :: _ -> MainCommand.Usage
              | Token Token.Test :: _ -> MainCommand.Test
              | Token Token.Convert :: _ -> MainCommand.Convert
              | Token Token.Clone :: _ -> MainCommand.Clone
              | Token Token.Graph :: _ -> MainCommand.Graph
              | Token Token.Publish :: _ -> MainCommand.Publish
              | Token Token.Build :: _ -> MainCommand.BuildView
              | Token Token.Rebuild :: _ -> MainCommand.RebuildView
              | Token Token.Checkout :: _ -> MainCommand.Checkout
              | Token Token.Branch :: _ -> MainCommand.Branch
              | Token Token.Pull :: _ -> MainCommand.Pull
              | Token Token.Clean :: _ -> MainCommand.Clean
              | Token Token.Bind :: _ -> MainCommand.Bind
              | Token Token.History :: _ -> MainCommand.History
              | Token Token.Install :: _ -> MainCommand.InstallPackage
              | Token Token.Query :: _ -> MainCommand.Query
              | Token Token.UpdateGuids :: _ -> MainCommand.UpgradeGuids
              | Token Token.Package :: _ -> MainCommand.Package
              | Token Token.Repo :: _ -> MainCommand.Repository
              | Token Token.NuGet :: _ -> MainCommand.NuGet
              | Token Token.View :: _ -> MainCommand.View
              | Token Token.App :: _ -> MainCommand.App
              | _ -> MainCommand.Unknown
    Command.Usage cmd

let Parse (args : string list) : Command =
    match args with
    | [Token Token.Version] -> Command.Version
    | Token Token.Help :: cmdArgs -> cmdArgs |> commandHelp
    | Token Token.Upgrade :: cmdArgs -> cmdArgs |> commandUpgrade "stable"
    | Token Token.Setup :: cmdArgs -> cmdArgs |> commandSetup false
    | Token Token.Init :: cmdArgs -> cmdArgs |> commandInit
    | Token Token.Exec :: cmdArgs -> cmdArgs |> commandExec false
    | Token Token.Test :: cmdArgs -> cmdArgs |> commandTest []
    | Token Token.Convert :: cmdArgs -> cmdArgs |> commandConvert false false
    | Token Token.Doctor :: cmdArgs -> cmdArgs |> commandDoctor
    | Token Token.Clone :: cmdArgs -> cmdArgs |> commandClone false false
    | Token Token.Graph :: cmdArgs -> cmdArgs |> commandGraph false
    | Token Token.Publish :: cmdArgs -> cmdArgs |> commandPublish None None None
    | Token Token.Push :: cmdArgs -> cmdArgs |> commandPush false
    | Token Token.Build :: cmdArgs -> cmdArgs |> commandBuild "Release" false false None
    | Token Token.Rebuild :: cmdArgs -> cmdArgs |> commandBuild "Release" true false None
    | Token Token.Checkout :: cmdArgs -> cmdArgs |> commandCheckout
    | Token Token.Branch :: cmdArgs -> cmdArgs |> commandBranch
    | Token Token.Pull :: cmdArgs -> cmdArgs |> commandPull true true false None
    | Token Token.Clean :: cmdArgs -> cmdArgs |> commandClean
    | Token Token.Bind :: cmdArgs -> cmdArgs |> commandBind
    | Token Token.History :: cmdArgs -> cmdArgs |> commandHistory false

    | Token Token.Install :: cmdArgs -> cmdArgs |> commandInstall
    | Token Token.Package :: Token Token.Update :: cmdArgs -> cmdArgs |> commandUpdate
    | Token Token.Package :: Token Token.Outdated :: cmdArgs -> cmdArgs |> commandOutdated
    | Token Token.Package :: Token Token.List :: cmdArgs -> cmdArgs |> commandListPackage

    | Token Token.Repo :: Token Token.Add :: cmdArgs -> cmdArgs |> commandAddRepo None Graph.BuilderType.MSBuild
    | Token Token.Repo :: Token Token.Drop :: cmdArgs -> cmdArgs |> commandDropRepo
    | Token Token.Repo :: Token Token.List :: cmdArgs -> cmdArgs |> commandListRepo

    | Token Token.NuGet :: Token Token.Add :: cmdArgs -> cmdArgs |> commandAddNuGet
    | Token Token.NuGet :: Token Token.List :: cmdArgs -> cmdArgs |> commandListNuGet

    | Token Token.View :: Token Token.Drop :: cmdArgs -> cmdArgs |> commandDropView
    | Token Token.View :: Token Token.List :: cmdArgs -> cmdArgs |> commandListView
    | Token Token.View :: Token Token.Describe :: cmdArgs -> cmdArgs |> commandDescribeView
    | Token Token.View :: Token Token.Alter :: cmdArgs -> cmdArgs |> commandAlterView None None None
    | Token Token.View :: cmdArgs -> cmdArgs |> commandAddView false false false None false false
    | Token Token.Open :: cmdArgs -> cmdArgs |> commandOpenView

    | Token Token.App :: Token Token.Add :: cmdArgs -> cmdArgs |> commandAddApp
    | Token Token.App :: Token Token.Drop :: cmdArgs -> cmdArgs |> commandDropApp
    | Token Token.App :: Token Token.List :: cmdArgs -> cmdArgs |> commandListApp None

    | Token Token.Query :: cmdArgs -> cmdArgs |> commandQuery false false false None None None

    | Token Token.UpdateGuids :: cmdArgs -> cmdArgs |> commandUpdateGuids
    | [FullBuildView viewFile] -> Command.FullBuildView { FilePath = viewFile }
    | _ -> Command.Error MainCommand.Usage


let IsVerbose (args : string list) : (bool * string list) =
    if (args <> List.empty && args |> List.head = "--verbose") then
        let newArgs = args.Tail
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
        "Copyright 2014-2017 Pierre Chalamet"
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
        [MainCommand.Usage], "help [<command|wks|repo|view|app>]: display help on command or area"
        [MainCommand.Version], "version : display full-build version"
        [MainCommand.Workspace; MainCommand.Setup], "setup <git|gerrit> <master-repository> <master-artifacts> <local-path> : setup a new environment in given path"
        [MainCommand.Workspace; MainCommand.Init], "init <git|gerrit> <master-repository> <local-path> : initialize a new workspace in given path"
        [MainCommand.Repository; MainCommand.Clone], "clone [--shallow] [--all] <repoId-wildcard>+ : clone repositories using provided wildcards"
        [MainCommand.Workspace; MainCommand.Checkout], "checkout [version] : checkout workspace or reset to default version"
        [MainCommand.Workspace; MainCommand.Branch], "branch [branch] : switch to branch"
        [MainCommand.Workspace; MainCommand.InstallPackage], "install : install packages"
        [MainCommand.View; MainCommand.AddView], "view [--src] [--ref] [--modified] [--app <app-wildcard>] [--static] [--test] <viewId> <viewId-wildcard>+ : create view with projects"
        [MainCommand.View; MainCommand.OpenView], "open <viewId> : open view with your favorite ide"
        [MainCommand.View; MainCommand.BuildView], "build [--mt] [--debug] [--version <version>] [<viewId>] : build view"
        [MainCommand.View; MainCommand.RebuildView], "rebuild [--mt] [--debug] [--version <version>] [<viewId>] : rebuild view (clean & build)"
        [MainCommand.View; MainCommand.Test], "test [--exclude <category>]* <viewId-wildcard>+ : test assemblies (match repository/project)"
        [MainCommand.View; MainCommand.Graph], "graph [--all] <viewId> : graph view content (project, packages, assemblies)"
        [MainCommand.Workspace; MainCommand.Exec], "exec [--all] <cmd> : execute command for each repository (variables: FB_NAME, FB_PATH, FB_URL, FB_WKS)"
        [MainCommand.Workspace; MainCommand.Convert], "convert [--check] <repoId-wildcard> : convert projects in repositories"
        [MainCommand.Workspace; MainCommand.Doctor], "doctor : check workspace consistency"
        [MainCommand.Workspace; MainCommand.Pull], "pull [--src|--bin] [--rebase] [--view <viewId>]: update sources & binaries - rebase if requested (ff is default)"
        [MainCommand.Workspace; MainCommand.Push], "push [--full] <version> : push artifacts and tag repositories"
        [MainCommand.Workspace; MainCommand.Bind], "bind <projectId-wildcard>+ : update bindings"
        [MainCommand.Workspace; MainCommand.History], "history [--html] : display history since last baseline"
        [MainCommand.Workspace; MainCommand.Upgrade], "upgrade [--alpha|--beta]: upgrade full-build to latest available version"
        [MainCommand.Workspace; MainCommand.Query], "query <--unused-projects|--packages> [--view <viewId>] : query items"
        [MainCommand.Workspace; MainCommand.Clean], "clean : DANGER! reset and clean workspace (interactive command)"
        [MainCommand.Workspace; MainCommand.UpgradeGuids], "update-guids : DANGER! change guids of all projects in given repository (interactive command)"
        [MainCommand.Unknown], ""
        [MainCommand.Package; MainCommand.UpdatePackage], "package update: update packages"
        [MainCommand.Package; MainCommand.OutdatedPackage], "package outdated : display outdated packages"
        [MainCommand.Package; MainCommand.ListPackage], "package list : list packages"
        [MainCommand.Unknown], ""
        [MainCommand.Repository; MainCommand.AddRepository], "repo add [--branch <branch>] <repoId> <repo-vcs> <repo-uri> <repo-tester> <repo-builder> : declare a new repository"
        [MainCommand.Repository; MainCommand.DropRepository], "repo drop <repoId> : drop repository"
        [MainCommand.Repository; MainCommand.ListRepository], "repo list : list repositories"
        [MainCommand.Unknown], ""
        [MainCommand.NuGet; MainCommand.AddNuGet], "nuget add <nuget-uri> : add nuget uri"
        [MainCommand.NuGet; MainCommand.ListNuget], "nuget list : list NuGet feeds"
        [MainCommand.Unknown], ""
        [MainCommand.View; MainCommand.DropView], "view drop <viewId> : drop view"
        [MainCommand.View; MainCommand.ListView], "view list : list views"
        [MainCommand.View; MainCommand.DescribeView], "view describe <name> : describe view"
        [MainCommand.View; MainCommand.AlterView], "view alter [--default] [--src] [--ref] <viewId> : alter view"
        [MainCommand.Unknown], ""
        [MainCommand.App; MainCommand.Publish], "publish [--view <viewId>] [--version <version>] [--status <status>] <appId-wildcard> : publish artifacts"
        [MainCommand.App; MainCommand.AddApp], "app add <appId> <copy|zip> <projectId>+ : create new application from given project ids"
        [MainCommand.App; MainCommand.DropApp], "app drop <appId> : drop application"
        [MainCommand.App; MainCommand.ListApp], "app list [--version <buildNumber>] : list applications" ]

    content



let PrintUsage (what : MainCommand) =
    let lines = UsageContent () |> List.filter (fun (cmd, _) -> cmd |> Seq.contains what || what = MainCommand.Unknown)
                                |> List.map (fun (_, desc) -> desc)

    printfn "Usage:"
    for line in lines do
        printfn "  %s" line

let PrintVersion () =
    VersionContent() |> List.iter (fun x -> printfn "%s" x)
