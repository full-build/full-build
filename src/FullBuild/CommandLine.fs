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

module CommandLine
open Anthology
open Collections


type SetupWorkspace = 
    { MasterRepository : RepositoryUrl
      MasterArtifacts : string
      Path : string }

type InitWorkspace = 
    { MasterRepository : RepositoryUrl
      Path : string }

type CheckoutWorkspace = 
    { Version : string }

type CloneRepositories = 
    { Filters : RepositoryId set 
      Shallow : bool}

type TestAssemblies = 
    { Filters : string list 
      Excludes : string list }

type NuGetUrl = 
    { Url : string }

type AddView = 
    { Name : ViewId
      Filters : string list }

type ViewName = 
    { Name : ViewId }

type PublishApplications = 
    { Filters : string list }

type CheckoutVersion =
    { Version : BookmarkVersion }

type AddApplication =
    { Name : ApplicationId
      Publisher : PublisherType
      Project : ProjectId }

type BuildView =
    { Name : ViewId
      Config : string 
      Clean : bool
      Multithread : bool }

type GraphView =
    { Name : ViewId
      All : bool }

type Exec = 
    { Command : string }

type AddRepository =
    {
        Repo : RepositoryId
        Url : RepositoryUrl
        Type : VcsType
    }

type PullWorkspace =
    {
        Src : bool
        Bin : bool
    }

type Command = 
    | Version
    | Usage
    | Error

    // workspace
    | SetupWorkspace of SetupWorkspace
    | InitWorkspace of InitWorkspace
    | IndexWorkspace
    | ConvertWorkspace
    | PushWorkspace
    | CheckoutWorkspace of CheckoutVersion
    | PullWorkspace of PullWorkspace
    | Exec of Exec
    | CleanWorkspace
    | UpdateGuids of RepositoryId
    | TestAssemblies of TestAssemblies

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

    | Migrate
