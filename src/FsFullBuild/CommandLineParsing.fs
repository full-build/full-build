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

type CreateWorkspace = 
    { Path : string }

type CheckoutWorkspace = 
    { Version : string }

type CloneRepositories = 
    { Filters : string list }

type NuGetUrl = 
    { Url : string }

type CreateView = 
    { Name : string
      Filters : string list }

type ViewName = 
    { Name : string }

type Command = 
    | Usage
    | Error

    // workspace
    | CreateWorkspace of CreateWorkspace
    | InitWorkspace of CreateWorkspace
    | IndexWorkspace
    | ConvertWorkspace
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
    | Token(Token.Workspace) :: Token(Index) :: [] -> Command.IndexWorkspace
    | Token(Token.Workspace) :: Token(Convert) :: [] -> Command.ConvertWorkspace

    | Token(Token.Repo) :: Token(Token.Add) :: vcs :: name :: url :: [] -> let (ToRepository repo) = (vcs, name, url)
                                                                           AddRepository(repo)
    | Token(Token.Repo) :: Token(Token.List) :: [] -> ListRepositories
    | Token(Token.Repo) :: Token(Token.Clone) :: filters -> CloneRepositories { Filters = filters }

    | Token(Token.View) :: Token(Token.Create) :: name :: Token(Token.Using) :: filters -> Command.CreateView { Name = name; Filters = filters }
    | Token(Token.View) :: Token(Token.Drop) :: name :: [] -> Command.DropView { Name = name }
    | Token(Token.View) :: Token(Token.List) :: [] -> Command.ListViews
    | Token(Token.View) :: Token(Token.Describe) :: name :: [] -> Command.DescribeView { Name = name }
    | Token(Token.View) :: Token(Token.Generate) :: name :: [] -> Command.GenerateView { Name = name }
    | Token(Token.View) :: Token(Token.Graph) :: name :: [] -> Command.GraphView { Name = name }
//    | Token(Token.View) :: Token(Token.Build) :: name :: [] -> Command.BuildView { Name = name }

    | Token(Token.Package) :: Token(Token.Install) :: [] -> Command.InstallPackages
    | Token(Token.Package) :: Token(Token.Simplify) :: [] -> Command.SimplifyPackages
    | Token(Token.Package) :: Token(Token.Update) :: [] -> Command.UpdatePackages
    | Token(Token.Package) :: Token(Token.Outdated) :: [] -> Command.OutdatedPackages
    | Token(Token.Package) :: Token(Token.List) :: [] -> Command.ListPackages

    | _ -> Command.Error

let DisplayUsage() = 
    printfn "Usage:"
    printfn "  help : display help"
    printfn "  create <path> : create a new environment in given path"
    printfn "  workspace init <path> : initialize a new workspace in givne path"
    printfn "  workspace index : synchronize anthology with projects"
    printfn "  workspace convert : adapt projects in workspace"
    printfn ""
    printfn "  repo clone <wildcards> : clone repositories using provided wildcards"
    printfn "  repo add <git|hg> <name> <uri> : declare a new repository"
    printfn "  repo list : list repositories"
    printfn ""
    printfn "  view create <name> using <wildcards> : create a new view using provided repository wildcards"
    printfn "  view drop <name> : drop a view"
    printfn "  view list : list views"
    printfn "  view describe <name> : describe view content"
    printfn "  view generate <name> : generate sln file for view"
    printfn "  view graph <name> : graph view content (project, packages, assemblies)"
    printfn ""
    printfn "  package install : install packages as defined in anthology"
    printfn "  package simplify : simplify package graph, promote assemblies or packages to project where permitted"
    printfn "  package update : update packages"
    printfn "  package outdated : display outdated packages"
    printfn "  package list : list packages"
