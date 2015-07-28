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

    | RefreshWorkspace
    | OptimizeWorkspace
    | BookmarkWorkspace
    | CheckoutWorkspace of CheckoutWorkspace
    | AddNuGet of NuGetUrl
    | ListNuGets
    | ListPackages
    | InstallPackages
    | UpgradePackages
    | UsePackage of Package
    | CheckPackages
    | GraphView of ViewName
    | BuildView of ViewName
    | RefreshSources
    | ListBinaries

//let ParsePackage(args : string list) = 
//    match args with
//    | [ Token(Token.List) ] -> ListPackages
//    | [ Token(Token.Update) ] -> InstallPackages
//    | [ Token(Token.Check) ] -> CheckPackages
//    | [ Token(Token.Upgrade) ] -> UpgradePackages
//    | [ Token(Token.Add); name; version ] -> UsePackage { Id = name; Version = version; TargetFramework = "net45" } // FIXME
//    | _ -> Command.Usage

let ParseCommandLine(args : string list) : Command = 
    match args with
    | Token(Token.Help) :: [] -> Command.Usage
    | Token(Token.Workspace) :: Token(Create) :: path :: [] -> Command.CreateWorkspace { Path = path }
    | Token(Token.Workspace) :: Token(Index) :: [] -> Command.IndexWorkspace
    | Token(Token.Workspace) :: Token(Convert) :: [] -> Command.ConvertWorkspace

    | Token(Token.Repo) :: Token(Token.Add) :: vcs :: name :: url :: [] -> let (ToRepository repo) = (vcs, name, url)
                                                                           AddRepository(repo)
    | Token(Token.Repo) :: Token(Token.Clone) :: filters -> CloneRepositories { Filters = filters }
    | Token(Token.Repo) :: Token(Token.List) :: [] -> ListRepositories

    | Token(Token.View) :: Token(Token.Create) :: name :: Token(Token.Using) :: filters -> Command.CreateView { Name = name; Filters = filters }
    | Token(Token.View) :: Token(Token.Drop) :: name :: [] -> Command.DropView { Name = name }
    | Token(Token.View) :: Token(Token.List) :: [] -> Command.ListViews
    | Token(Token.View) :: Token(Token.Describe) :: name :: [] -> Command.DescribeView { Name = name }
    | Token(Token.View) :: Token(Token.Build) :: name :: [] -> Command.BuildView { Name = name }
    | Token(Token.View) :: Token(Token.Graph) :: name :: [] -> Command.GraphView { Name = name }

    | _ -> Command.Error

let DisplayUsage() = printfn "Usage: TBD"
let DisplayHelp() = printfn "Help : TBD"
