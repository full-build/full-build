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

let rec commandTest (args : string list) (excludes : string list) =
    match args with
    | TokenOption TokenOption.Exclude :: category :: tail -> commandTest tail (category :: excludes)
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
    | TokenOption TokenOption.NoShallow :: tail -> commandClone false tail
    | [] -> Command.Error
    | filters -> let repoFilters = filters |> Seq.map RepositoryId.from |> Set
                 CloneRepositories { Filters = repoFilters; Shallow = shallow }




let rec commandGraph (all : bool) (args : string list) =
    match args with
    | TokenOption TokenOption.All :: tail -> commandGraph true tail
    | [MatchViewId name] -> Command.GraphView { Name = name ; All = all }
    | _ -> Command.Error

let commandPublish (args : string list) =
    match args with
    | [] -> Command.Error
    | filters -> PublishApplications {Filters = filters}



let rec commandBuild (config : string) (forceBuild : bool) (args : string list) =
    match args with
    | TokenOption TokenOption.Debug :: tail -> commandBuild "Debug" forceBuild tail
    | [(MatchViewId name)] -> Command.BuildView { Name = name ; Config = config; ForceRebuild = forceBuild }
    | _ -> Command.Error

let commandCheckout (args : string list) =
    match args with
    | [MatchBookmarkVersion version] -> Command.CheckoutWorkspace {Version = version}
    | _ -> Command.Error

let commandPush (args : string list) =
    match args with
    | [] -> Command.PushWorkspace
    | _ -> Command.Error

let rec commandPull (args : string list) (src : bool) (bin : bool) =
    match args with
    | TokenOption TokenOption.All :: tail -> commandPull tail true true
    | TokenOption TokenOption.Src :: tail -> commandPull tail true bin
    | TokenOption TokenOption.Bin :: tail -> commandPull tail src true
    | [] -> if not (src || bin) then failwith "Missing mandatory parameter: --all, --src or --bin"
            Command.PullWorkspace
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

let rec commandAddRepo (repoType : VcsType option) (args : string list) =
    let checkAndSetRepoType newValue =
        if repoType <> None then failwith "Too many parameters : --git, --gerrit or --hg"
        commandAddRepo (Some newValue)

    match args with
    | TokenOption TokenOption.Git :: tail -> checkAndSetRepoType VcsType.Git tail 
    | TokenOption TokenOption.Gerrit :: tail -> checkAndSetRepoType VcsType.Gerrit tail 
    | TokenOption TokenOption.Hg :: tail -> checkAndSetRepoType VcsType.Hg tail 
    | name :: [url] -> match repoType with
                       | None -> failwith "Missing mandatory parameter --git, --gerrit or --hg"
                       | Some x -> Command.AddRepository { Repo = RepositoryId.from name; Url = RepositoryUrl.from url; Type = x }
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
    | Token Token.Clone :: cmdArgs -> commandClone true cmdArgs
    | Token Token.Graph :: cmdArgs -> commandGraph false cmdArgs
    | Token Token.Publish :: cmdArgs -> commandPublish cmdArgs
    | Token Token.Build :: cmdArgs -> commandBuild "Release" false cmdArgs 
    | Token Token.Rebuild :: cmdArgs -> commandBuild "Release" true cmdArgs
    | Token Token.Checkout :: cmdArgs -> commandCheckout cmdArgs
    | Token Token.Push :: cmdArgs -> commandPush cmdArgs
    | Token Token.Pull :: cmdArgs -> commandPull cmdArgs false false
    | Token Token.Clean :: cmdArgs -> commandClean cmdArgs

    | Token Token.Install :: Token Token.Package :: cmdArgs -> commandInstall cmdArgs
    | Token Token.Update :: Token Token.Package :: cmdArgs -> commandUpdate cmdArgs
    | Token Token.Outdated :: Token Token.Package :: cmdArgs -> commandOutdated cmdArgs
    | Token Token.List :: Token Token.Package :: cmdArgs -> commandListPackage cmdArgs

    | Token Token.Add :: Token Token.Repo :: cmdArgs -> commandAddRepo None cmdArgs
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
    let content = [
        "  help : display this help"
        "  version : display full-build version"
        "  setup <master-repository> <master-artifacts> <local-path> : setup a new environment in given path"
        "  init <master-repository> <local-path> : initialize a new workspace in given path"
        "  clone [--noshallow] <repo-wildcard>+ : clone repositories using provided wildcards"
        "  checkout <version> : checkout workspace to version"
        "  build [--debug] <view-name> : build view"
        "  rebuild [--debug] <view-name> : clean & build view"
        "  test [--exclude <category>]* <test-wildcard>+ : test assemblies (match repository/project)"
        "  graph [--all] <view-name> : graph view content (project, packages, assemblies)"
        "  exec <cmd> : execute command for each repository (variables FB_NAME, FB_PATH, FB_URL available)"
        "  index : index workspace"
        "  convert : convert projects in workspace"
        "  pull <--all | --bin | --src> : update to latest version"
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
        "  add repo <--git | --gerrit | --hg> <repo-name> <repo-uri> : declare a new repository"
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
        "  add app <--copy | --azure> <app-name> <project-id>+ : create new application from given project ids"
        "  drop app <app-name> : drop application"
        "  list app : list applications" 
        "  describe app <app-name>" ]

    content

let DisplayUsage() = 
    UsageContent() |> Seq.iter (fun x -> printfn "%s" x)

let DisplayVersion() =
    let version = VersionContent()
    printfn "%s" version
