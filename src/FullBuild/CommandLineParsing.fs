// Copyright (c) 2014-2015, Pierre Chalamet
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//     * Redistributions of source code must retain the above copyright
//       notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright
//       notice, this list of conditions and the following disclaimer in the
//       documentation and/or other materials provided with the distribution.
//     * Neither the name of Pierre Chalamet nor the
//       names of its contributors may be used to endorse or promote products
//       derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL PIERRE CHALAMET BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
module CommandLineParsing

open Anthology
open CommandLineToken
open Collections
open StringHelpers

type SetupWorkspace = 
    { MasterRepository : RepositoryUrl
      MasterArtifacts : string
      Path : string }

type InitWorkspace = 
    { MasterRepository : RepositoryUrl
      Path : string }

type CheckoutWorkspace = 
    { Version : string }

type CloneRepositories = 
    { Filters : RepositoryId set }

type TestAssemblies = 
    { Filters : string list }

type NuGetUrl = 
    { Url : string }

type AddView = 
    { Name : ViewId
      Filters : string list }

type ViewName = 
    { Name : ViewId }

type PublishApplications = 
    { Filters : string list }

type CheckoutVersion =
    { Version : BookmarkVersion }

type AddApplication =
    { Name : ApplicationId
      Projects : ProjectId set }

type BuildView =
    { Name : ViewId
      Config : string 
      ForceRebuild : bool }

type GraphView =
    { Name : ViewId
      All : bool }

type Exec = 
    { Command : string }

type Command = 
    | Version
    | Usage
    | Error

    // workspace
    | SetupWorkspace of SetupWorkspace
    | InitWorkspace of InitWorkspace
    | IndexWorkspace
    | ConvertWorkspace
    | PushWorkspace
    | CheckoutWorkspace of CheckoutVersion
    | PullWorkspace
    | Exec of Exec
    | CleanWorkspace
    | UpdateGuids of RepositoryId
    | TestAssemblies of TestAssemblies

    // repository
    | ListRepositories
    | AddRepository of RepositoryId * RepositoryUrl
    | CloneRepositories of CloneRepositories
    | DropRepository of RepositoryId

    // view
    | ListViews
    | AddView of AddView
    | DropView of ViewName
    | DescribeView of ViewName
    | GraphView of GraphView
    | BuildView of BuildView

    // nuget
    | AddNuGet of RepositoryUrl
    | ListNuGets

    // package
    | ListPackages
    | InstallPackages
    | UpdatePackages
    | OutdatedPackages

    // applications
    | ListApplications
    | AddApplication of AddApplication
    | DropApplication of ApplicationId
    | PublishApplications of PublishApplications

    | Migrate

let (|MatchBookmarkVersion|) version =
    match version with
    | "master" -> Master
    | x -> BookmarkVersion x

let (|MatchViewId|) view =
    ViewId view

let (|MatchRepositoryId|) repo =
    RepositoryId.from repo

let (|MatchApplicationId|) name =
    ApplicationId.from name


let ParseCommandLine (args : string list) : Command = 
    match args with
    | [Token Token.Version] -> Command.Version
    | [Token Token.Help] -> Command.Usage

    | Token Token.Setup :: masterRepository :: masterArtifacts :: [path] -> Command.SetupWorkspace { MasterRepository=RepositoryUrl.from masterRepository
                                                                                                     MasterArtifacts=masterArtifacts
                                                                                                     Path = path }
    | Token Token.Init :: masterRepository:: [path] -> Command.InitWorkspace { MasterRepository=RepositoryUrl.from masterRepository
                                                                               Path = path }
    | Token Token.Exec :: [cmd] -> Command.Exec { Command = cmd }
    | Token Token.Test :: filters -> Command.TestAssemblies { Filters = filters }

    | [Token Token.Index] -> Command.IndexWorkspace
    | [Token Token.Convert] -> Command.ConvertWorkspace
    | Token Token.Clone :: filters -> let repoFilters = filters |> Seq.map RepositoryId.from |> Set
                                      CloneRepositories { Filters = repoFilters }

    | Token Token.Graph :: TokenOption TokenOption.All :: [MatchViewId name] -> Command.GraphView { Name = name ; All = true}
    | Token Token.Graph :: [MatchViewId name] -> Command.GraphView { Name = name ; All = false }

    | Token Token.Publish :: filters -> PublishApplications {Filters = filters}

    | Token Token.Build :: TokenOption TokenOption.Debug :: [MatchViewId name] -> Command.BuildView { Name = name ; Config="Debug"; ForceRebuild=false }
    | Token Token.Build :: [(MatchViewId name)] -> Command.BuildView { Name = name ; Config = "Release"; ForceRebuild=false }

    | Token Token.Rebuild :: TokenOption TokenOption.Debug :: [MatchViewId name] -> Command.BuildView { Name = name ; Config="Debug"; ForceRebuild=true }
    | Token Token.Rebuild :: [(MatchViewId name)] -> Command.BuildView { Name = name ; Config = "Release"; ForceRebuild=true }

    | Token Token.Checkout :: [MatchBookmarkVersion version] -> Command.CheckoutWorkspace {Version = version}
    | [Token Token.Push] -> Command.PushWorkspace
    | [Token Token.Pull] -> Command.PullWorkspace
    | [Token Token.Clean] -> Command.CleanWorkspace

    | [Token Token.Install] -> Command.InstallPackages
    | [Token Token.Update] -> Command.UpdatePackages
    | [Token Token.Outdated] -> Command.OutdatedPackages

    | Token Token.AddRepo :: name :: [url] -> Command.AddRepository (RepositoryId.from name, RepositoryUrl.from url)
    | Token Token.DropRepo :: [MatchRepositoryId repo] -> Command.DropRepository repo
    | [Token Token.ListRepo] -> ListRepositories

    | Token Token.AddNuGet :: [uri] -> Command.AddNuGet (RepositoryUrl.from uri)
    | [Token Token.ListNuGet] -> Command.ListNuGets

    | Token Token.AddView :: MatchViewId name :: filters -> Command.AddView { Name = name; Filters = filters }
    | Token Token.DropView :: [MatchViewId name] -> Command.DropView { Name = name }
    | [Token Token.ListView] -> Command.ListViews
    | Token Token.DescribeView :: [MatchViewId name] -> Command.DescribeView { Name = name }

    | Token Token.AddApp :: MatchApplicationId name :: filters -> let projects = filters |> Seq.map ProjectId.from |> Set
                                                                  Command.AddApplication { Name = name; Projects = projects }
    | Token Token.DropApp :: [MatchApplicationId name] -> Command.DropApplication name
    | [Token Token.ListApp] -> ListApplications

    | [Token Token.ListPackage] -> Command.ListPackages

    | Token Token.UpdateGuids :: [name] -> Command.UpdateGuids (RepositoryId.from name)
    | [Token Token.Migrate] -> Command.Migrate

    | _ -> Command.Error


let VersionContent() =
    let version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version
    let fbVersion = sprintf "full-build %s" (version.ToString())
    fbVersion

let UsageContent() =
    let fbVersion = VersionContent()

    let content = [
        "  help : display this help"
        "  version : display full-build version"
        "  install : install packages declared in anthology"
        "  clone <selection-wildcards ...> : clone repositories using provided wildcards"
        "  add-view <view-name> <view-wildcards ...> : add repositories to view"
        "  drop-<view|repo|app> <name> : drop named object"
        "  build [--debug] <view-name> : build view"
        "  rebuild [--debug] <view-name> : clean & build view"
        "  test <test-wildcards ...> : test assemblies"
        "  graph [--all] <view-name> : graph view content (project, packages, assemblies)"
        "  list-repo : list repositories"
        "  list-view : list views"
        "  list-nuget : list NuGet feeds"
        "  list-package : list packages"
        "  describe-repo <name> : describe repository"
        "  describe-view <name> : describe view"
        "  checkout <version|master> : checkout workspace to version"
        "  exec <cmd> : execute command for each repository (variables FB_NAME, FB_PATH, FB_URL available)"
        "  publish <app> : publish application"
        "  pull : update to latest version"
        ""
        "  setup <master-repository> <master-artifacts> <local-path> : setup a new environment in given path"
        "  init <master-repository> <local-path> : initialize a new workspace in given path"
        "  add-repo <repo-name> <repo-uri> : declare a new repository (git or hg supported)"
        "  add-nuget <nuget-uri> : add nuget uri"
        "  add-app <name> <project-id-list...> : create new application from given project ids"
        "  index : index workspace"
        "  convert : convert projects in workspace"
        "  update : update packages"
        "  outdated : display outdated packages"
        "  push : push a baseline from current repositories version and display version"
        ""
        "DANGER ZONE!"
        "  clean : reset and clean workspace (interactive command)"
        "  update-guids : change guids of all projects in given repository (interactive command)" 
        ""
        fbVersion ]

    content

let DisplayUsage() = 
    UsageContent() |> Seq.iter (fun x -> printfn "%s" x)

let DisplayVersion() =
    let version = VersionContent()
    printfn "%s" version
