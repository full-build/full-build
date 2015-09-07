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

type CreateWorkspace = 
    { Path : string }

type CheckoutWorkspace = 
    { Version : string }

type CloneRepositories = 
    { Filters : RepositoryId set }

type NuGetUrl = 
    { Url : string }

type CreateView = 
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
    | CreateWorkspace of CreateWorkspace
    | InitWorkspace of CreateWorkspace
    | IndexWorkspace
    | ConvertWorkspace
    | BookmarkWorkspace
    | CheckoutWorkspace of CheckoutVersion
    // repository
    | AddRepository of Repository
    | CloneRepositories of CloneRepositories
    | ListRepositories
    // view
    | CreateView of CreateView
    | DropView of ViewName
    | ListViews
    | DescribeView of ViewName
    | GenerateView of ViewName
    | GraphView of ViewName
    | BuildView of ViewName

    // package
    | InstallPackages
    | SimplifyPackages
    | UpdatePackages
    | OutdatedPackages
    | ListPackages

    // applications
    | DeployApplications of DeployApplications
    | ListApplications

    // env
//    | RefreshWorkspace
//    | BookmarkWorkspace
//    | CheckoutWorkspace of CheckoutWorkspace
//    | AddNuGet of NuGetUrl
//    | ListNuGets
//    | UsePackage of Package
//    | CheckPackages
//    | BuildView of ViewName
//    | RefreshSources
//    | ListBinaries

let ParseCommandLine(args : string list) : Command = 
    match args with
    | Token(Token.Help) :: [] -> Command.Usage
    | Token(Create) :: path :: [] -> Command.CreateWorkspace { Path = path }
    | Token(Init) :: path :: [] -> Command.InitWorkspace { Path = path }
    | Token(Convert) :: [] -> Command.ConvertWorkspace
    | Token(Token.Bookmark) :: [] -> Command.BookmarkWorkspace
    | Token(Token.Checkout) :: version :: [] -> Command.CheckoutWorkspace {Version = BookmarkVersion version}
    | Token(Token.Update) :: [] -> Command.UpdatePackages
    | Token(Token.Graph) :: name :: [] -> Command.GraphView { Name = ViewId name }
    | Token(Token.Clone) :: filters -> let repoFilters = filters |> Seq.map RepositoryId.Bind |> Set
                                       CloneRepositories { Filters = repoFilters }
    | Token(Token.Deploy) ::names -> let appNames = names |> Seq.map ApplicationId.Bind |> Set
                                     DeployApplications {Names = appNames }
    | Token(Token.Install) :: [] -> Command.InstallPackages
    | Token(Token.Outdated) :: [] -> Command.OutdatedPackages

    | Token(Token.Add) :: Token(Token.Repo) :: vcs :: name :: url :: [] -> let (ToRepository repo) = (vcs, name, url)
                                                                           AddRepository(repo)
    | Token(Token.Add) :: Token(Token.View) :: name :: filters -> let repoFilters = filters |> Seq.map RepositoryId.Bind |> Set
                                                                  Command.CreateView { Name = ViewId name; Filters = repoFilters }

    | Token(Token.List) :: Token(Token.Repo) :: [] -> ListRepositories
    | Token(Token.List) :: Token(Token.View) :: [] -> Command.ListViews
    | Token(Token.Build) :: name :: [] -> Command.BuildView { Name = ViewId name }
    | Token(Token.List) :: Token(Token.Package) :: [] -> Command.ListPackages
    | Token(Token.List) :: Token(Token.Application) :: [] -> ListApplications

    | Token(Token.Drop) :: Token(Token.View) :: name :: [] -> Command.DropView { Name = ViewId name }
//    | Token(Token.Drop) :: Token(Token.Repo) :: name :: [] -> Command.DropRepo { Name = name }

    | Token(Token.Describe) :: Token(Token.View) :: name :: [] -> Command.DescribeView { Name = ViewId name }
//    | Token(Token.View) :: Token(Token.Build) :: name :: [] -> Command.BuildView { Name = name }

    | Token(Token.Debug) :: Token(Token.Simplify) :: [] -> Command.SimplifyPackages
    | Token(Token.Debug) :: Token(Index) :: [] -> Command.IndexWorkspace
    | Token(Token.Debug) :: Token(Token.Generate) :: name :: [] -> Command.GenerateView { Name = ViewId name }
    | _ -> Command.Error

let UsageContent() =
    seq {
        yield "Usage:"
        yield "  help : display help"
        yield "  create <path> : create a new environment in given path"
        yield "  init <path> : initialize a new workspace in given path"
        yield "  convert : adapt projects in workspace"
        yield "  clone <wildcards> : clone repositories using provided wildcards"
        yield "  install : install packages declared in anthology"
        yield "  outdated : display outdated packages"
        yield "  graph <name> : graph view content (project, packages, assemblies)"
        yield "  deploy <name> : deploy application"
        yield "  build <name> : build view"
        yield ""
        yield "  add repo <git|hg> <name> <uri> : declare a new repository"
        yield "  add view <name> <wildcards> : add repositories to view"
        yield "  drop <repo|view|package|app> <name> : drop object"
        yield "  list <repo|view|package|app> : list objects"
        yield "  describe <repo|view> <name> : describe view or repository"
        yield ""
        yield "  debug index : synchronize anthology with projects"    
        yield "  debug simplify : simplify packages graph, promote assemblies or packages to project where permitted"
        yield "  debug generate <name> : generate sln file for view"
        yield ""
    }

let DisplayUsage() = 
    UsageContent() |> Seq.iter (fun x -> printfn "%s" x)
