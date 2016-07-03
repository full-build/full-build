//   Copyright 2014-2016 Pierre Chalamet
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

module CommandLine
open Anthology
open Collections



type SetupWorkspace = 
    { MasterRepository : RepositoryUrl
      MasterArtifacts : string
      Type : VcsType
      Path : string }

type InitWorkspace = 
    { MasterRepository : RepositoryUrl
      Type : VcsType
      Path : string }

type CheckoutWorkspace = 
    { Version : string }

type CloneRepositories = 
    { Filters : RepositoryId set 
      Shallow : bool
      All : bool 
      Multithread : bool }

type IndexRepositories = 
    { Filters : RepositoryId set }

type ConvertRepositories = 
    { Filters : RepositoryId set }

type TestAssemblies = 
    { Filters : string list 
      Excludes : string list }

type NuGetUrl = 
    { Url : string }

type AddView = 
    { Name : ViewId
      Filters : string list 
      SourceOnly : bool 
      Parents : bool 
      AddNew : bool }

type ViewName = 
    { Name : ViewId }

type PublishApplications = 
    { Filters : string list 
      Multithread : bool }

type CheckoutVersion =
    { Version : BookmarkVersion }

type BranchWorkspace = 
    { Branch : BookmarkVersion option }

type AddApplication =
    { Name : ApplicationId
      Publisher : PublisherType
      Project : ProjectId }

type BuildView =
    { Name : ViewId option
      Config : string 
      Clean : bool
      Multithread : bool 
      Version : string option }

type AlterView =
    { Name : ViewId
      Default : bool option
      Source : bool option 
      Parents : bool option }

type OpenView =
    { Name : ViewId }

type GraphView =
    { Name : ViewId
      All : bool }

type Exec = 
    { Command : string 
      All : bool }

type AddRepository =
    { Repo : RepositoryId
      Url : RepositoryUrl
      Branch : BranchId option
      Builder : BuilderType }

type PullWorkspace =
    { Src : bool
      Bin : bool
      Rebase : bool 
      View : ViewId option }

type PushWorkspace =
    { BuildNumber : string 
      Branch : string option }

type BindProject =
    { Filters : string list }




type MainCommand =
    | Version
    | Usage
    | Upgrade
    | Setup
    | Init
    | Index
    | Convert
    | Push
    | Checkout
    | Branch
    | Pull
    | Exec
    | Clean
    | UpgradeGuids
    | Test
    | History
    | ListRepository
    | AddRepository
    | CloneRepository
    | DropRepository
    | ListView
    | AddView
    | DropView
    | DescribeView
    | GraphView
    | BuildView
    | RebuildView
    | AlterView
    | OpenView
    | AddNuGet
    | ListNuget
    | ListPackage
    | InstallPackage
    | UpdatePackage
    | OutdatedPackage
    | ListApp
    | AddApp
    | DropApp
    | PublishApp
    | Bind
    | Unknown


type Command = 
    | Error of MainCommand

    | Version
    | Usage
    | Upgrade
    | FinalizeUpgrade of int
    
    // workspace
    | SetupWorkspace of SetupWorkspace
    | InitWorkspace of InitWorkspace
    | IndexRepositories of IndexRepositories
    | ConvertRepositories of ConvertRepositories
    | PushWorkspace of PushWorkspace
    | CheckoutWorkspace of CheckoutVersion
    | BranchWorkspace of BranchWorkspace
    | PullWorkspace of PullWorkspace
    | Exec of Exec
    | CleanWorkspace
    | UpdateGuids of RepositoryId
    | TestAssemblies of TestAssemblies
    | History

    // repository
    | ListRepositories
    | AddRepository of AddRepository
    | CloneRepositories of CloneRepositories
    | DropRepository of RepositoryId

    // view
    | ListViews
    | AddView of AddView
    | DropView of ViewName
    | DescribeView of ViewName
    | GraphView of GraphView
    | BuildView of BuildView
    | AlterView of AlterView
    | OpenView of OpenView

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
    | BindProject of BindProject
