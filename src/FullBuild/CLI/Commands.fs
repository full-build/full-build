//   Copyright 2014-2017 Pierre Chalamet
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
      Path : string 
      SxS : bool }

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
    { Filters : string set
      Check : bool }

type ConvertRepositories =
    { Filters : string set
      Check : bool
      Reset : bool }

type TestView =
    { Name : string }

type AddView =
    { Name : string
      Filters : string list
      UpReferences : bool
      DownReferences : bool
      Modified : bool
      AppFilter : string option
      Static : bool
      Tests : bool }

type ViewName =
    { Name : string }

type PublishApplications =
    { View : string
      Multithread : bool }

type PushWorkspace =
    { Version : string
      Incremental : bool }


type CheckoutVersion =
    { Version : string }

type BranchWorkspace =
    { Branch : string option
      Bin : bool }

type AddApplication =
    { Name : string
      Publisher : Graph.PublisherType
      Projects : string set }

type BuildView =
    { Name : string option
      Platform : string option
      Configuration : string option
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
      Src : bool 
      Bin : bool }

type Exec =
    { Command : string
      All : bool }

type AddRepository =
    { Name : string
      Url : string
      Branch : string option
      Builder : Graph.BuilderType
      Tester : Graph.TestRunnerType
      Vcs : Graph.VcsType }

type PullWorkspace =
    { Sources : bool
      Bin : bool
      Rebase : bool
      Multithread : bool
      View : string option }

type History =
    { Html : bool }

type ListApplications =
    { Version : string option }

type Query =
    { View : string option
      Source : RepositoryId option
      Destination : RepositoryId option
      UnusedProjects : bool
      UsedPackages : bool 
      References : bool 
      Cycle : bool }

[<RequireQualifiedAccess>]
type MainCommand =
    | Version
    | Usage
    | Setup
    | Init
    | Index
    | Convert
    | Checkout
    | Branch
    | Pull
    | Exec
    | Clean
    | UpgradeGuids
    | Test
    | History
    | Repository
    | ListRepository
    | AddRepository
    | Clone
    | DropRepository
    | View
    | ListView
    | AddView
    | DropView
    | DescribeView
    | Graph
    | BuildView
    | RebuildView
    | AlterView
    | OpenView
    | NuGet
    | Package
    | InstallPackage
    | App
    | ListApp
    | AddApp
    | DropApp
    | Publish
    | Push
    | Query
    | Bind
    | Workspace
    | Doctor
    | Unknown


[<RequireQualifiedAccess>]
type Command =
    | Error of MainCommand

    | Version
    | Usage of MainCommand

    // workspace
    | SetupWorkspace of SetupWorkspace
    | InitWorkspace of InitWorkspace
    | ConvertRepositories of ConvertRepositories
    | CheckoutWorkspace of CheckoutVersion
    | BranchWorkspace of BranchWorkspace
    | PullWorkspace of PullWorkspace
    | Exec of Exec
    | CleanWorkspace
    | UpdateGuids of UpdateGuids
    | TestView of TestView
    | History of History
    | PushWorkspace of PushWorkspace
    | Doctor

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
    | Graph of GraphView
    | BuildView of BuildView
    | AlterView of AlterView
    | OpenView of OpenView
    | FullBuildView of FullBuildView

    // package
    | InstallPackages

    // applications
    | ListApplications of ListApplications
    | AddApplication of AddApplication
    | DropApplication of ApplicationId
    | PublishApplications of PublishApplications

    // query
    | Query of Query


