module CommandLine
open Anthology
open Collections
open StringHelpers


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
    { Filters : RepositoryId set }

type TestAssemblies = 
    { Filters : string list }

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
      Projects : ProjectId set }

type BuildView =
    { Name : ViewId
      Config : string 
      ForceRebuild : bool }

type GraphView =
    { Name : ViewId
      All : bool }

type Exec = 
    { Command : string }

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
    | PullWorkspace
    | Exec of Exec
    | CleanWorkspace
    | UpdateGuids of RepositoryId
    | TestAssemblies of TestAssemblies

    // repository
    | ListRepositories
    | AddRepository of RepositoryId * RepositoryUrl
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
