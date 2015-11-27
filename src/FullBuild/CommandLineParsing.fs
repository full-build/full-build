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
open CommandLine



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

let (|MatchPublisherType|) name =
    PublisherType.from name



let commandSetup (args : string list) =
    match args with
    | masterRepository :: masterArtifacts :: [path] -> Command.SetupWorkspace { MasterRepository = RepositoryUrl.from masterRepository
                                                                                MasterArtifacts = masterArtifacts
                                                                                Path = path }
    | _ -> Command.Error


let commandInit (args : string list) =
    match args with 
    | masterRepository:: [path] -> Command.InitWorkspace { MasterRepository = RepositoryUrl.from masterRepository
                                                           Path = path }
    | _ -> Command.Error


let commandExec (args : string list) =
    match args with 
    | [cmd] -> Command.Exec { Command = cmd }
    | _ -> Command.Error

let commandTest (args : string list) =
    match args with
    | [] -> Command.Error
    | filters -> Command.TestAssemblies { Filters = filters }


let commandIndex (args : string list) =
    match args with
    | [] -> Command.IndexWorkspace
    | _ -> Command.Error

let commandConvert (args : string list) =
    match args with
    | [] -> Command.ConvertWorkspace
    | _ -> Command.Error

let commandClone (args : string list) =
    match args with
    | [] -> Command.Error
    | filters -> let repoFilters = filters |> Seq.map RepositoryId.from |> Set
                 CloneRepositories { Filters = repoFilters }

let commandGraph (args : string list) =
    match args with
    | TokenOption TokenOption.All :: [MatchViewId name] -> Command.GraphView { Name = name ; All = true}
    | [MatchViewId name] -> Command.GraphView { Name = name ; All = false }
    | _ -> Command.Error

let commandPublish (args : string list) =
    match args with
    | [] -> Command.Error
    | filters -> PublishApplications {Filters = filters}

let commandBuild (args : string list) (forceBuild : bool) =
    match args with
    | TokenOption TokenOption.Debug :: [MatchViewId name] -> Command.BuildView { Name = name ; Config = "Debug"; ForceRebuild = forceBuild }
    | [(MatchViewId name)] -> Command.BuildView { Name = name ; Config = "Release"; ForceRebuild = forceBuild }
    | _ -> Command.Error

let commandCheckout (args : string list) =
    match args with
    | [MatchBookmarkVersion version] -> Command.CheckoutWorkspace {Version = version}
    | _ -> Command.Error

let commandPush (args : string list) =
    match args with
    | [] -> Command.PushWorkspace
    | _ -> Command.Error

let commandPull (args : string list) =
    match args with
    | [] -> Command.PullWorkspace
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

let commandAddRepo (args : string list) =
    match args with
    | name :: [url] -> Command.AddRepository (RepositoryId.from name, RepositoryUrl.from url)
    | _ -> Command.Error

let commandDropRepo (args : string list) =
    match args with
    | [MatchRepositoryId repo] -> Command.DropRepository repo
    | _ -> Command.Error

let commandListRepo (args : string list) =
    match args with
    | [MatchRepositoryId repo] -> ListRepositories
    | _ -> Command.Error

let commandAddNuGet (args : string list) =
    match args with
    | [uri] -> Command.AddNuGet (RepositoryUrl.from uri)
    | _ -> Command.Error

let commandListNuGet (args : string list) =
    match args with
    | [] -> Command.ListNuGets
    | _ -> Command.Error

let commandAddView (args : string list) =
    match args with
    | MatchViewId name :: filters -> Command.AddView { Name = name; Filters = filters }
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

let commandAddApp (args : string list) =
    match args with
    | MatchApplicationId name :: MatchPublisherType pub :: filters -> let projects = filters |> Seq.map ProjectId.from |> Set
                                                                      Command.AddApplication { Name = name; Publisher = pub; Projects = projects }
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

let commandMigrate (args : string list) =
    match args with
    | [] -> Command.Migrate
    | _ -> Command.Error



let ParseCommandLine (args : string list) : Command = 
    match args with
    | [Token Token.Version] -> Command.Version
    | [Token Token.Help] -> Command.Usage
    | Token Token.Setup :: cmdArgs -> commandSetup cmdArgs
    | Token Token.Init :: cmdArgs -> commandInit cmdArgs
    | Token Token.Exec :: cmdArgs -> commandExec cmdArgs
    | Token Token.Test :: cmdArgs -> commandTest cmdArgs
    | Token Token.Index :: cmdArgs -> commandTest cmdArgs
    | Token Token.Convert :: cmdArgs -> commandConvert cmdArgs
    | Token Token.Clone :: cmdArgs -> commandClone cmdArgs
    | Token Token.Graph :: cmdArgs -> commandGraph cmdArgs
    | Token Token.Publish :: cmdArgs -> commandPublish cmdArgs
    | Token Token.Build :: cmdArgs -> commandBuild cmdArgs false
    | Token Token.Rebuild :: cmdArgs -> commandBuild cmdArgs true
    | Token Token.Checkout :: cmdArgs -> commandCheckout cmdArgs
    | Token Token.Push :: cmdArgs -> commandPush cmdArgs
    | Token Token.Pull :: cmdArgs -> commandPull cmdArgs
    | Token Token.Clean :: cmdArgs -> commandClean cmdArgs

    | Token Token.Install :: Token Token.Package :: cmdArgs -> commandInstall cmdArgs
    | Token Token.Update :: Token Token.Package :: cmdArgs -> commandUpdate cmdArgs
    | Token Token.Outdated :: Token Token.Package :: cmdArgs -> commandOutdated cmdArgs
    | Token Token.List :: Token Token.Package :: cmdArgs -> commandListPackage cmdArgs

    | Token Token.Add :: Token Token.Repo :: cmdArgs -> commandAddRepo cmdArgs
    | Token Token.Drop :: Token Token.Repo :: cmdArgs -> commandDropRepo cmdArgs
    | Token Token.List :: Token Token.Repo :: cmdArgs -> commandListRepo cmdArgs

    | Token Token.Add :: Token Token.NuGet :: cmdArgs -> commandAddNuGet cmdArgs
    | Token Token.List :: Token Token.NuGet :: cmdArgs -> commandListNuGet cmdArgs

    | Token Token.Add :: Token Token.View :: cmdArgs -> commandAddView cmdArgs
    | Token Token.Drop :: Token Token.View :: cmdArgs -> commandDropView cmdArgs
    | Token Token.List :: Token Token.View :: cmdArgs -> commandListView cmdArgs
    | Token Token.Describe :: Token Token.View :: cmdArgs -> commandDescribeView cmdArgs
    
    | Token Token.Add :: Token Token.App :: cmdArgs -> commandAddApp cmdArgs
    | Token Token.Drop :: Token Token.App :: cmdArgs -> commandDropApp cmdArgs
    | Token Token.List :: Token Token.App :: cmdArgs -> commandListApp cmdArgs

    | Token Token.UpdateGuids :: cmdArgs -> commandUpdateGuids cmdArgs
    | Token Token.Migrate :: cmdArgs -> commandMigrate cmdArgs
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
        "  clone <selection-wildcards ...> : clone repositories using provided wildcards"
        "  build [--debug] <view-name> : build view"
        "  rebuild [--debug] <view-name> : clean & build view"
        "  test <test-wildcards ...> : test assemblies"
        "  graph [--all] <view-name> : graph view content (project, packages, assemblies)"
        "  checkout <version|master> : checkout workspace to version"
        "  exec <cmd> : execute command for each repository (variables FB_NAME, FB_PATH, FB_URL available)"
        "  publish <app> : publish application"
        "  pull : update to latest version"
        "  setup <master-repository> <master-artifacts> <local-path> : setup a new environment in given path"
        "  init <master-repository> <local-path> : initialize a new workspace in given path"
        "  index : index workspace"
        "  convert : convert projects in workspace"
        "  push : push a baseline from current repositories version and display version"
        // package
        "  install package : install packages declared in anthology"
        "  update package : update packages"
        "  outdated package : display outdated packages"
        "  list package : list packages"
        // repo
        "  add repo <repo-name> <repo-uri> : declare a new repository (git or hg supported)"
        "  drop repo <repo-name> : drop repository"
        "  list repo : list repositories"
        // nuget
        "  add nuget <nuget-uri> : add nuget uri"
        "  list nuget : list NuGet feeds"
        // view
        "  add view <view-name> <view-wildcards ...> : add repositories to view"
        "  drop view <view-name> : drop view"
        "  list view : list views"
        "  describe view <name> : describe view"
        //app
        "  add app <name> <copy> <project-id-list...> : create new application from given project ids"
        "  drop app <app-name> : drop application"
        "  list app : list applications"
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
