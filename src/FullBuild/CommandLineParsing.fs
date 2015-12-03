//   Copyright 2014-2015 Pierre Chalamet
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

let rec commandTest (excludes : string list) (args : string list) =
    match args with
    | TokenOption TokenOption.Exclude :: category :: tail -> tail |> commandTest (category :: excludes)
    | filters -> Command.TestAssemblies { Filters = filters; Excludes = excludes }


let commandIndex (args : string list) =
    match args with
    | [] -> Command.IndexWorkspace
    | _ -> Command.Error

let commandConvert (args : string list) =
    match args with
    | [] -> Command.ConvertWorkspace
    | _ -> Command.Error

let rec commandClone (shallow : bool) (args : string list) =
    match args with
    | TokenOption TokenOption.NoShallow :: tail -> tail |> commandClone false
    | [] -> Command.Error
    | filters -> let repoFilters = filters |> Seq.map RepositoryId.from |> Set
                 CloneRepositories { Filters = repoFilters; Shallow = shallow }




let rec commandGraph (all : bool) (args : string list) =
    match args with
    | TokenOption TokenOption.All :: tail -> tail |> commandGraph true
    | [MatchViewId name] -> Command.GraphView { Name = name ; All = all }
    | _ -> Command.Error

let commandPublish (args : string list) =
    match args with
    | [] -> Command.Error
    | filters -> PublishApplications {Filters = filters}



let rec commandBuild (config : string) (clean : bool) (multithread : bool) (args : string list) =
    match args with
    | TokenOption TokenOption.Debug :: tail -> tail |> commandBuild "Debug" clean multithread
    | TokenOption TokenOption.Multithread :: tail -> tail |> commandBuild config clean true
    | [(MatchViewId name)] -> Command.BuildView { Name = name ; Config = config; Clean = clean; Multithread = multithread }
    | _ -> Command.Error

let commandCheckout (args : string list) =
    match args with
    | [MatchBookmarkVersion version] -> Command.CheckoutWorkspace {Version = version}
    | _ -> Command.Error

let commandPush (args : string list) =
    match args with
    | [] -> Command.PushWorkspace
    | _ -> Command.Error

let rec commandPull (src : bool) (bin : bool) (args : string list) =
    match args with
    | TokenOption TokenOption.Src :: tail -> tail |> commandPull true false
    | TokenOption TokenOption.Bin :: tail -> tail |> commandPull false true
    | [] -> Command.PullWorkspace { Src = src ; Bin = bin }
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

let commandAddRepo  (args : string list) =
    match args with
    | name :: vcs :: [url] -> Command.AddRepository { Repo = RepositoryId.from name; Url = RepositoryUrl.from url; Type = VcsType.from vcs }
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
    | Token Token.Test :: cmdArgs -> commandTest [] cmdArgs
    | Token Token.Index :: cmdArgs -> commandIndex cmdArgs
    | Token Token.Convert :: cmdArgs -> commandConvert cmdArgs
    | Token Token.Clone :: cmdArgs -> cmdArgs |> commandClone true
    | Token Token.Graph :: cmdArgs -> cmdArgs |> commandGraph false
    | Token Token.Publish :: cmdArgs -> commandPublish cmdArgs
    | Token Token.Build :: cmdArgs -> cmdArgs |> commandBuild "Release" false false
    | Token Token.Rebuild :: cmdArgs -> cmdArgs |> commandBuild "Release" true false
    | Token Token.Checkout :: cmdArgs -> commandCheckout cmdArgs
    | Token Token.Push :: cmdArgs -> commandPush cmdArgs
    | Token Token.Pull :: cmdArgs -> cmdArgs |> commandPull true true
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

    [
        fbVersion
        ""
        "Please refer to enclosed LICENSE.txt for licensing terms."
        ""
        "Copyright 2014-2015 Pierre Chalamet"
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
        "  setup <master-repository> <master-artifacts> <local-path> : setup a new environment in given path"
        "  init <master-repository> <local-path> : initialize a new workspace in given path"
        "  clone [--noshallow] <repo-wildcard>+ : clone repositories using provided wildcards"
        "  checkout <version> : checkout workspace to version"
        "  build [--debug] [--mt] <view-name> : build view"
        "  rebuild [--debug] [--mt] <view-name> : rebuild view (clean & build)"
        "  test [--exclude <category>]* <test-wildcard>+ : test assemblies (match repository/project)"
        "  graph [--all] <view-name> : graph view content (project, packages, assemblies)"
        "  exec <cmd> : execute command for each repository (variables FB_NAME, FB_PATH, FB_URL available)"
        "  index : index workspace"
        "  convert : convert projects in workspace"
        "  pull [--src | --bin] : update to latest version"
        "  push : push a baseline from current repositories version and display version"
        "  publish <app> : publish application"
        "  clean : DANGER! reset and clean workspace (interactive command)"
        "  update-guids : DANGER! change guids of all projects in given repository (interactive command)" 
        ""
        "  install package : install packages declared in anthology"
        "  update package : update packages"
        "  outdated package : display outdated packages"
        "  list package : list packages"
        ""
        "  add repo <repo-name> <git | gerrit | hg> <repo-uri> : declare a new repository"
        "  drop repo <repo-name> : drop repository"
        "  list repo : list repositories"
        ""
        "  add nuget <nuget-uri> : add nuget uri"
        "  list nuget : list NuGet feeds"
        ""
        "  add view <view-name> <view-wildcard>+ : add repositories to view"
        "  drop view <view-name> : drop view"
        "  list view : list views"
        "  describe view <name> : describe view"
        ""
        "  add app <app-name> <copy | zip | azure> <project-id>+ : create new application from given project ids"
        "  drop app <app-name> : drop application"
        "  list app : list applications" 
        "  describe app <app-name>" ]

    content

let DisplayUsage() = 
    UsageContent() |> Seq.iter (fun x -> printfn "%s" x)

let DisplayVersion() =
    VersionContent() |> Seq.iter (fun x -> printfn "%s" x)
