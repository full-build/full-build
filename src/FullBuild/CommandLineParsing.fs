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

module CommandLineParsing

open Anthology
open CommandLineToken
open Collections
open CommandLine



let (|MatchBookmarkVersion|) version =
    BookmarkVersion version

let (|MatchViewId|) view =
    ViewId view

let (|MatchRepositoryId|) name =
    RepositoryId.from name

let (|MatchProjectId|) name =
    ProjectId.from name

let (|MatchApplicationId|) name =
    ApplicationId.from name

let (|MatchBranchId|) name =
    BranchId.from name

let (|MatchPublisherType|) name =
    PublisherType.from name



let commandSetup (args : string list) =
    match args with
    | vcs :: masterRepository :: masterArtifacts :: [path] -> Command.SetupWorkspace { MasterRepository = RepositoryUrl.from masterRepository
                                                                                       MasterArtifacts = masterArtifacts
                                                                                       Type = VcsType.from vcs
                                                                                       Path = path }
    | _ -> Command.Error


let commandInit (args : string list) =
    match args with 
    | vcs :: masterRepository:: [path] -> Command.InitWorkspace { MasterRepository = RepositoryUrl.from masterRepository
                                                                  Type = VcsType.from vcs
                                                                  Path = path }
    | _ -> Command.Error


let rec commandExec (all : bool) (args : string list) =
    match args with 
    | TokenOption TokenOption.All :: tail -> tail |> commandExec true
    | [cmd] -> Command.Exec { Command = cmd; All = all }
    | _ -> Command.Error

let rec commandTest (excludes : string list) (args : string list) =
    match args with
    | TokenOption TokenOption.Exclude :: category :: tail -> tail |> commandTest (category :: excludes)
    | [] -> Command.Error
    | filters -> Command.TestAssemblies { Filters = filters; Excludes = excludes }


let rec commandIndex (args : string list) =
    match args with
    | [] -> Command.Error
    | filters -> let repoFilters = filters |> Seq.map RepositoryId.from |> Set
                 IndexRepositories { Filters = repoFilters }

let commandConvert (args : string list) =
    match args with
    | [] -> Command.Error
    | filters -> let repoFilters = filters |> Seq.map RepositoryId.from |> Set
                 ConvertRepositories { Filters = repoFilters }

let rec commandClone (shallow : bool) (all : bool) (args : string list) =
    match args with
    | TokenOption TokenOption.Shallow :: tail -> tail |> commandClone true all
    | TokenOption TokenOption.All :: tail -> tail |> commandClone shallow true
    | [] -> Command.Error
    | filters -> let repoFilters = filters |> Seq.map RepositoryId.from |> Set
                 CloneRepositories { Filters = repoFilters; Shallow = shallow; All = all }




let rec commandGraph (all : bool) (args : string list) =
    match args with
    | TokenOption TokenOption.All :: tail -> tail |> commandGraph true
    | [MatchViewId name] -> Command.GraphView { Name = name ; All = all }
    | _ -> Command.Error

let commandPublish (args : string list) =
    match args with
    | [] -> Command.Error
    | filters -> PublishApplications {Filters = filters}



let rec commandBuild (config : string) (clean : bool) (multithread : bool) (version : string option) (args : string list) =
    match args with
    | TokenOption TokenOption.Version :: ver :: tail -> tail |> commandBuild config clean multithread (Some ver)
    | TokenOption TokenOption.Debug :: tail -> tail |> commandBuild "Debug" clean multithread version
    | TokenOption TokenOption.Multithread :: tail -> tail |> commandBuild config clean true version
    | [] -> Command.BuildView { Name = None ; Config = config; Clean = clean; Multithread = multithread; Version = version }
    | [MatchViewId name] -> Command.BuildView { Name = Some name ; Config = config; Clean = clean; Multithread = multithread; Version = version }
    | _ -> Command.Error

let commandCheckout (args : string list) =
    match args with
    | [MatchBookmarkVersion version] -> Command.CheckoutWorkspace {Version = version}
    | _ -> Command.Error

let commandPush (args : string list) =
    match args with
    | [buildNumber] -> Command.PushWorkspace { BuildNumber = buildNumber }
    | _ -> Command.Error

let rec commandPull (src : bool) (bin : bool) (rebase : bool) (args : string list) =
    match args with
    | TokenOption TokenOption.Src :: tail -> tail |> commandPull true false rebase
    | TokenOption TokenOption.Bin :: tail -> tail |> commandPull false true rebase
    | TokenOption TokenOption.Rebase :: tail -> tail |> commandPull src bin true
    | [] -> Command.PullWorkspace { Src = src ; Bin = bin; Rebase = rebase }
    | _ -> Command.Error

let commandClean (args : string list) =
    match args with
    | [] -> Command.CleanWorkspace
    | _ -> Command.Error

let commandInstall (args : string list) =
    match args with
    | [] -> Command.InstallPackages
    | _ -> Command.Error

let commandUpdate (args : string list) =
    match args with
    | [] -> Command.UpdatePackages
    | _ -> Command.Error

let commandOutdated (args : string list) =
    match args with
    | [] -> Command.OutdatedPackages
    | _ -> Command.Error

let rec commandAddRepo (branch : BranchId option) (builder : BuilderType) (sticky : bool) (args : string list) =
    match args with
    | TokenOption TokenOption.Sticky :: tail -> tail |> commandAddRepo branch builder true
    | TokenOption TokenOption.Branch :: MatchBranchId branch :: tail -> tail |> commandAddRepo (Some branch) builder sticky
    | name :: [url] -> Command.AddRepository { Repo = RepositoryId.from name
                                               Url = RepositoryUrl.from url
                                               Branch = branch
                                               Builder = builder 
                                               Sticky = sticky }
    | _ -> Command.Error

let commandDropRepo (args : string list) =
    match args with
    | [MatchRepositoryId repo] -> Command.DropRepository repo
    | _ -> Command.Error

let commandListRepo (args : string list) =
    match args with
    | [] -> ListRepositories
    | _ -> Command.Error

let commandAddNuGet (args : string list) =
    match args with
    | [uri] -> Command.AddNuGet (RepositoryUrl.from uri)
    | _ -> Command.Error

let commandListNuGet (args : string list) =
    match args with
    | [] -> Command.ListNuGets
    | _ -> Command.Error

let rec commandAddView (sourceOnly : bool) (args : string list) =
    match args with
    | TokenOption TokenOption.Src :: tail -> tail |> commandAddView true
    | MatchViewId name :: one :: more -> Command.AddView { Name = name; Filters = one::more ; SourceOnly = sourceOnly}
    | _ -> Command.Error

let commandDropView (args : string list) =
    match args with
    | [MatchViewId name] -> Command.DropView { Name = name }
    | _ -> Command.Error

let commandListView (args : string list) =
    match args with
    | [] -> Command.ListViews
    | _ -> Command.Error

let commandDescribeView (args : string list) =
    match args with
    | [MatchViewId name] -> Command.DescribeView { Name = name }
    | _ -> Command.Error

let rec commandAlterView (isDefault : bool) (args : string list) =
    match args with
    | TokenOption TokenOption.Default :: tail -> tail |> commandAlterView true
    | [MatchViewId name] -> Command.AlterView { Name = name ; Default = isDefault }
    | _ -> Command.Error

let rec commandOpenView (forceSrc : bool) (args : string list) =
    match args with
    | TokenOption TokenOption.Src :: tail -> tail |> commandOpenView true
    | [MatchViewId name] -> Command.OpenView { Name = name; ForceSrc = forceSrc }
    | _ -> Command.Error

let commandAddApp (args : string list) =
    match args with
    | MatchApplicationId name :: MatchPublisherType pub :: [MatchProjectId prj] -> Command.AddApplication { Name = name; Publisher = pub; Project = prj }
    | _ -> Command.Error

let commandDropApp (args : string list) =
    match args with
    | [MatchApplicationId name] -> Command.DropApplication name
    | _ -> Command.Error

let commandListApp (args : string list) =
    match args with
    | [] -> ListApplications
    | _ -> Command.Error

let commandListPackage (args : string list) =
    match args with
    | [] -> Command.ListPackages
    | _ -> Command.Error

let commandUpdateGuids (args : string list) =
    match args with
    | [name] -> Command.UpdateGuids (RepositoryId.from name)
    | _ -> Command.Error

let commandBind (args : string list) =
    match args with
    | one :: more -> Command.BindProject { Filters = one::more }
    | _ -> Command.Error

let commandHistory (args : string list) =
    match args with
    | [] -> Command.History
    | _ -> Command.Error

let commandUpgrade (args : string list) =
    match args with
    | [] -> Command.Upgrade
    | _ -> Command.Error


let ParseCommandLine (args : string list) : Command = 
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
    | Token Token.Clone :: cmdArgs -> cmdArgs |> commandClone false false
    | Token Token.Graph :: cmdArgs -> cmdArgs |> commandGraph false
    | Token Token.Publish :: cmdArgs -> cmdArgs |> commandPublish
    | Token Token.Build :: cmdArgs -> cmdArgs |> commandBuild "Release" false false None
    | Token Token.Rebuild :: cmdArgs -> cmdArgs |> commandBuild "Release" true false None
    | Token Token.Checkout :: cmdArgs -> cmdArgs |> commandCheckout
    | Token Token.Push :: cmdArgs -> cmdArgs |> commandPush
    | Token Token.Pull :: cmdArgs -> cmdArgs |> commandPull true true false
    | Token Token.Clean :: cmdArgs -> cmdArgs |> commandClean
    | Token Token.Bind :: cmdArgs -> cmdArgs |> commandBind
    | Token Token.History :: cmdArgs -> cmdArgs |> commandHistory

    | Token Token.Install :: cmdArgs -> cmdArgs |> commandInstall
    | Token Token.Update :: Token Token.Package :: cmdArgs -> cmdArgs |> commandUpdate
    | Token Token.Outdated :: Token Token.Package :: cmdArgs -> cmdArgs |> commandOutdated
    | Token Token.List :: Token Token.Package :: cmdArgs -> cmdArgs |> commandListPackage

    | Token Token.Add :: Token Token.Repo :: cmdArgs -> cmdArgs |> commandAddRepo None BuilderType.MSBuild false
    | Token Token.Drop :: Token Token.Repo :: cmdArgs -> cmdArgs |> commandDropRepo
    | Token Token.List :: Token Token.Repo :: cmdArgs -> cmdArgs |> commandListRepo

    | Token Token.Add :: Token Token.NuGet :: cmdArgs -> cmdArgs |> commandAddNuGet
    | Token Token.List :: Token Token.NuGet :: cmdArgs -> cmdArgs |> commandListNuGet

    | Token Token.View :: cmdArgs -> cmdArgs |> commandAddView false
    | Token Token.Drop :: Token Token.View :: cmdArgs -> cmdArgs |> commandDropView
    | Token Token.List :: Token Token.View :: cmdArgs -> cmdArgs |> commandListView
    | Token Token.Describe :: Token Token.View :: cmdArgs -> cmdArgs |> commandDescribeView
    | Token Token.Alter :: Token Token.View :: cmdArgs -> cmdArgs |> commandAlterView false
    | Token Token.Open :: cmdArgs -> cmdArgs |> commandOpenView false
    
    | Token Token.Add :: Token Token.App :: cmdArgs -> cmdArgs |> commandAddApp
    | Token Token.Drop :: Token Token.App :: cmdArgs -> cmdArgs |> commandDropApp
    | Token Token.List :: Token Token.App :: cmdArgs -> cmdArgs |> commandListApp

    | Token Token.UpdateGuids :: cmdArgs -> cmdArgs |> commandUpdateGuids
    | _ -> Command.Error


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
        "  help : display this help"
        "  version : display full-build version"
        "  setup <git|gerrit|hg> <master-repository> <master-artifacts> <local-path> : setup a new environment in given path"
        "  init <master-repository> <local-path> : initialize a new workspace in given path"
        "  clone [--shallow] [--all] <repo-wildcard>+ : clone repositories using provided wildcards"
        "  checkout <version> : checkout workspace to version"
        "  install : install packages"
        "  view [--src] <view-name> <view-wildcard>+ : add repositories to view"
        "  open [--src] <viewName> : open view with your favorite ide"
        "  build [--debug] [--version <version>] [--mt] [<view-name>] : build view"
        "  rebuild [--debug] [--version <version>] [--mt] [<view-name>] : rebuild view (clean & build)"
        "  test [--exclude <category>]* <test-wildcard>+ : test assemblies (match repository/project)"
        "  graph [--all] <view-name> : graph view content (project, packages, assemblies)"
        "  exec [--all] <cmd> : execute command for each repository (variables: FB_NAME, FB_PATH, FB_URL, FB_WKS)"
        "  index <repo-wildcard>+ : index repositories"
        "  convert <repo-wildcard> : convert projects in repositories"
        "  pull [--src|--bin] [--rebase] : update to latest version - rebase if requested (ff is default)"
        "  push <buildNumber> : push a baseline from current repositories version and display version"
        "  publish <app> : publish application"
        "  bind <projectId-wildcard>+ : update bindings"
        "  clean : DANGER! reset and clean workspace (interactive command)"
        "  history : display history since last baseline"
        "  upgrade : upgrade full-build to latest available version"
        ""
        "  update package : update packages"
        "  outdated package : display outdated packages"
        "  list package : list packages"
        ""
        "  add repo [--branch <branchId>] [--sticky] <repo-name> <repo-uri> : declare a new repository"
        "  drop repo <repo-name> : drop repository"
        "  list repo : list repositories"
        ""
        "  add nuget <nuget-uri> : add nuget uri"
        "  list nuget : list NuGet feeds"
        ""
        "  drop view <view-name> : drop view"
        "  list view : list views"
        "  describe view <name> : describe view"
        "  alter view [--default] <viewName> : alter view"
        ""
        "  add app <app-name> <copy|zip> <project-id>+ : create new application from given project ids"
        "  drop app <app-name> : drop application"
        "  list app : list applications" 
        "  describe app <app-name>"
        ""
        "  update-guids : DANGER! change guids of all projects in given repository (interactive command)" ]

    content

let DisplayUsage() = 
    UsageContent() |> Seq.iter (fun x -> printfn "%s" x)

let DisplayVersion() =
    VersionContent() |> Seq.iter (fun x -> printfn "%s" x)
