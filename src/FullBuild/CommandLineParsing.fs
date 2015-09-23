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

type NuGetUrl = 
    { Url : string }

type AddView = 
    { Name : ViewId
      Filters : RepositoryId set }

type ViewName = 
    { Name : ViewId }

type DeployApplications = 
    { Names : ApplicationId set }

type CheckoutVersion =
    {
        Version : BookmarkVersion
    }

type Command = 
    | Usage
    | Error

    // workspace
    | SetupWorkspace of SetupWorkspace
    | InitWorkspace of InitWorkspace
    | ConvertWorkspace
    | PushWorkspace
    | CheckoutWorkspace of CheckoutVersion
    | PullWorkspace
    | Exec of string

    // repository
    | ListRepositories
    | AddRepository of RepositoryId * RepositoryUrl
    | CloneRepositories of CloneRepositories

    // view
    | ListViews
    | AddView of AddView
    | DropView of ViewName
    | DescribeView of ViewName
    | GraphView of ViewName
    | BuildView of ViewName

    // nuget
    | AddNuGet of RepositoryUrl

    // package
    | ListPackages
    | InstallPackages
    | SimplifyPackages
    | UpdatePackages
    | OutdatedPackages

    // applications
    | ListApplications
    | DeployApplications of DeployApplications

let (|MatchBookmarkVersion|) version =
    match version with
    | "master" -> Master
    | x -> BookmarkVersion x

let (|MatchViewId|) view =
    ViewId view

let ParseCommandLine(args : string list) : Command = 
    match args with
    | Token(Token.Help) :: [] -> Command.Usage

    | Token(Token.Setup) :: masterRepository :: masterArtifacts :: path :: [] -> Command.SetupWorkspace { MasterRepository=RepositoryUrl.from masterRepository
                                                                                                          MasterArtifacts=masterArtifacts
                                                                                                          Path = path }
    | Token(Token.Init) :: masterRepository:: path :: [] -> Command.InitWorkspace { MasterRepository=RepositoryUrl.from masterRepository
                                                                                    Path = path }
    | Token(Token.Exec) :: cmd :: [] -> Command.Exec cmd
    | Token(Token.Convert) :: [] -> Command.ConvertWorkspace
    | Token(Token.Clone) :: filters -> let repoFilters = filters |> Seq.map RepositoryId.from |> Set
                                       CloneRepositories { Filters = repoFilters }
    | Token(Token.Graph) :: (MatchViewId name) :: [] -> Command.GraphView { Name = name }
    | Token(Token.Deploy) :: names -> let appNames = names |> Seq.map ApplicationId.from |> Set
                                      DeployApplications {Names = appNames }
    | Token(Token.Build) :: (MatchViewId name) :: [] -> Command.BuildView { Name = name }
    | Token(Token.Checkout) :: (MatchBookmarkVersion version) :: [] -> Command.CheckoutWorkspace {Version = version}
    | Token(Token.Push) :: [] -> Command.PushWorkspace
    | Token(Token.Pull) :: [] -> Command.PullWorkspace

    | Token(Token.Install) :: [] -> Command.InstallPackages
    | Token(Token.Package) ::Token(Token.Update) :: [] -> Command.UpdatePackages
    | Token(Token.Package) ::Token(Token.Outdated) :: [] -> Command.OutdatedPackages

    | Token(Token.Add) :: Token(Token.Repo) :: name :: url :: [] -> Command.AddRepository (RepositoryId.from name, RepositoryUrl.from url)
    | Token(Token.Add) :: Token(Token.NuGet) :: uri :: [] -> Command.AddNuGet (RepositoryUrl.from uri)
    | Token(Token.Add) :: Token(Token.View) :: (MatchViewId name) :: filters -> let repoFilters = filters |> Seq.map RepositoryId.from |> Set
                                                                                Command.AddView { Name = name; Filters = repoFilters }
    | Token(Token.Drop) :: Token(Token.View) :: (MatchViewId name) :: [] -> Command.DropView { Name = name }
    | Token(Token.List) :: Token(Token.Repo) :: [] -> ListRepositories
    | Token(Token.List) :: Token(Token.View) :: [] -> Command.ListViews
    | Token(Token.List) :: Token(Token.Package) :: [] -> Command.ListPackages
    | Token(Token.List) :: Token(Token.Application) :: [] -> ListApplications
    | Token(Token.Describe) :: Token(Token.View) :: (MatchViewId name) :: [] -> Command.DescribeView { Name = name }

    | _ -> Command.Error

let UsageContent() =
    let content = [
        "Usage:"
        "  help : display help"
        "  setup <master-repository> <master-artifacts> <local-path> : setup a new environment in given path"
        "  init <master-repository> <local-path> : initialize a new workspace in given path"
        "  install : install packages declared in anthology"
        "  clone <selection-wildcards ...> : clone repositories using provided wildcards"
        "  convert : adapt projects in workspace"
        "  build <view-name> : build view"
        "  deploy <view-name> : deploy application"
        "  graph <view-name> : graph view content (project, packages, assemblies)"
        "  exec <cmd> : execute command for each repository (vars FB_NAME, FB_PATH, FB_URL available)"
        ""
        "  checkout <version|master> : checkout workspace to version"
        "  push : push a baseline from current repositories version and display version"
        "  pull : update to latest version"
        "  package update : update packages"
        "  package outdated : display outdated packages"
        ""
        "  add repo <repo-name> <repo-uri> : declare a new repository (git or hg supported)"
        "  add nuget <nuget-uri> : add nuget uri"
        "  add view <view-name> <view-wildcards ...> : add repositories to view"
        "  drop view <view-name> : drop object"
        "  list <repo|view|package|app> : list objects"
        "  describe <repo|view> <name> : describe view or repository"
        ""
        "  debug index : synchronize anthology with projects"    
        "  debug simplify : simplify packages graph, promote assemblies or packages to project where permitted"
        "  debug generate <view-name> : generate sln file for view"
        "" ]
    content

let DisplayUsage() = 
    UsageContent() |> Seq.iter (fun x -> printfn "%s" x)
