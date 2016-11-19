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

module CLI.Commands
open Anthology
open Collections


type SetupWorkspace =
    { MasterRepository : string
      MasterArtifacts : string
      Type : Graph.VcsType
      Path : string }

type InitWorkspace =
    { MasterRepository : string
      Type : Graph.VcsType
      Path : string }

type CheckoutWorkspace =
    { Version : string }

type CloneRepositories =
    { Filters : string set
      Shallow : bool
      All : bool
      Multithread : bool }

type UpdateGuids =
    { Filters : string set }

type IndexRepositories =
    { Filters : string set }

type ConvertRepositories =
    { Filters : string set }

type TestAssemblies =
    { Filters : string set
      Excludes : string set }

type NuGetUrl =
    { Url : string }

type AddView =
    { Name : string
      Filters : string list
      UpReferences : bool
      DownReferences : bool
      Modified : bool 
      AppFilter : string option
      Static : bool }

type ViewName =
    { Name : string }

type PublishApplications =
    { View: string option
      Filters : string list
      Multithread : bool }

type CheckoutVersion =
    { Version : string }

type BranchWorkspace =
    { Branch : string option }

type AddApplication =
    { Name : string
      Publisher : Graph.PublisherType
      Projects : string set }

type BuildView =
    { Name : string option
      Config : string
      Clean : bool
      Multithread : bool
      Version : string option }

type AlterView =
    { Name : string
      Default : bool option
      UpReferences : bool option
      DownReferences : bool option }

type OpenView =
    { Name : string }

type FullBuildView =
    { FilePath : string }

type GraphView =
    { Name : string
      All : bool }

type Exec =
    { Command : string
      All : bool }

type AddRepository =
    { Name : string
      Url : string
      Branch : string option
      Builder : Graph.BuilderType }

type PullWorkspace =
    { Sources : bool
      Bin : bool
      Rebase : bool
      Multithread : bool
      View : string option }

type PushWorkspace =
    { BuildNumber : string
      Branch : string option
      Incremental : bool }

type BindProject =
    { Filters : string set }

type History =
    { Html : bool }

type ListApplications =
    { Version : string option }

type QueryUnused =
    { Project : bool }


[<RequireQualifiedAccess>]
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
    | ListUnused
    | Bind
    | Unknown


[<RequireQualifiedAccess>]
type Command =
    | Error of MainCommand

    | Version
    | Usage
    | Upgrade of string
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
    | UpdateGuids of UpdateGuids
    | TestAssemblies of TestAssemblies
    | History of History

    // repository
    | ListRepositories
    | AddRepository of AddRepository
    | CloneRepositories of CloneRepositories
    | DropRepository of string

    // view
    | ListViews
    | AddView of AddView
    | DropView of ViewName
    | DescribeView of ViewName
    | GraphView of GraphView
    | BuildView of BuildView
    | AlterView of AlterView
    | OpenView of OpenView
    | FullBuildView of FullBuildView

    // nuget
    | AddNuGet of RepositoryUrl
    | ListNuGets

    // package
    | ListPackages
    | InstallPackages
    | UpdatePackages
    | OutdatedPackages

    // applications
    | ListApplications of ListApplications
    | AddApplication of AddApplication
    | DropApplication of ApplicationId
    | PublishApplications of PublishApplications
    | BindProject of BindProject

    // projects
    | QueryUnused of QueryUnused


