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
    { Name : string
      Filters : RepositoryId set }

type ViewName = 
    { Name : string }

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
    | Token(Token.Workspace) :: Token(Init) :: path :: [] -> Command.InitWorkspace { Path = path }
    | Token(Token.Debug) :: Token(Token.Workspace) :: Token(Index) :: [] -> Command.IndexWorkspace
    | Token(Token.Workspace) :: Token(Convert) :: [] -> Command.ConvertWorkspace
    | Token(Token.Bookmark) :: [] -> Command.BookmarkWorkspace
    | Token(Token.Checkout) :: version :: [] -> Command.CheckoutWorkspace {Version = BookmarkVersion version}

    | Token(Token.Repo) :: Token(Token.Add) :: vcs :: name :: url :: [] -> let (ToRepository repo) = (vcs, name, url)
                                                                           AddRepository(repo)
    | Token(Token.Repo) :: Token(Token.List) :: [] -> ListRepositories
    | Token(Token.Repo) :: Token(Token.Clone) :: filters -> let repoFilters = filters |> Seq.map RepositoryId.Bind |> Set
                                                            CloneRepositories { Filters = repoFilters }

    | Token(Token.View) :: Token(Token.Create) :: name :: Token(Token.Using) :: filters -> let repoFilters = filters |> Seq.map RepositoryId.Bind |> Set
                                                                                           Command.CreateView { Name = name; Filters = repoFilters }
    | Token(Token.View) :: Token(Token.Drop) :: name :: [] -> Command.DropView { Name = name }
    | Token(Token.View) :: Token(Token.List) :: [] -> Command.ListViews
    | Token(Token.View) :: Token(Token.Describe) :: name :: [] -> Command.DescribeView { Name = name }
    | Token(Token.View) :: Token(Token.Generate) :: name :: [] -> Command.GenerateView { Name = name }
    | Token(Token.View) :: Token(Token.Graph) :: name :: [] -> Command.GraphView { Name = name }
//    | Token(Token.View) :: Token(Token.Build) :: name :: [] -> Command.BuildView { Name = name }

    | Token(Token.Package) :: Token(Token.Install) :: [] -> Command.InstallPackages
    | Token(Token.Debug) :: Token(Token.Package) :: Token(Token.Simplify) :: [] -> Command.SimplifyPackages
    | Token(Token.Package) :: Token(Token.Update) :: [] -> Command.UpdatePackages
    | Token(Token.Package) :: Token(Token.Outdated) :: [] -> Command.OutdatedPackages
    | Token(Token.Package) :: Token(Token.List) :: [] -> Command.ListPackages

    | Token(Token.Application) :: Token(Token.List) :: [] -> ListApplications
    | Token(Token.Application) :: Token(Token.Deploy) :: names -> let appNames = names |> Seq.map ApplicationId.Bind |> Set
                                                                  DeployApplications {Names = appNames }

    | _ -> Command.Error

let UsageContent() =
    seq {
        yield "Usage:"
        yield "  help : display help"
        yield "  create <path> : create a new environment in given path"
        yield "  workspace init <path> : initialize a new workspace in givne path"
        yield "  workspace convert : adapt projects in workspace"
        yield ""
        yield "  repo clone <wildcards> : clone repositories using provided wildcards"
        yield "  repo add <git|hg> <name> <uri> : declare a new repository"
        yield "  repo list : list repositories"
        yield ""
        yield "  view create <name> using <wildcards> : create a new view using provided repository wildcards"
        yield "  view drop <name> : drop a view"
        yield "  view list : list views"
        yield "  view describe <name> : describe view content"
        yield "  view generate <name> : generate sln file for view"
        yield "  view graph <name> : graph view content (project, packages, assemblies)"
        yield ""
        yield "  package install : install packages as defined in anthology"
        yield "  package update : update packages"
        yield "  package outdated : display outdated packages"
        yield "  package list : list installed packages"
        yield ""
        yield "  debug workspace index : synchronize anthology with projects"    
        yield "  debug package simplify : simplify package graph, promote assemblies or packages to project where permitted"
    }

let DisplayUsage() = 
    UsageContent() |> Seq.iter (fun x -> printfn "%s" x)
