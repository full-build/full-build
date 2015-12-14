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

module Main

open CommandLine
open CommandLineParsing

let tryMain argv = 
    Env.CheckLicense ()

    let cmd = ParseCommandLine (argv |> Seq.toList)
    match cmd with
    // workspace
    | SetupWorkspace wsInfo -> Workspace.Create wsInfo.Path wsInfo.MasterRepository wsInfo.MasterArtifacts
    | InitWorkspace wsInfo -> Workspace.Init wsInfo.Path wsInfo.MasterRepository
    | IndexWorkspace -> Workspace.Index ()
    | ConvertWorkspace -> Workspace.Convert ()
    | PushWorkspace -> Workspace.Push ()
    | CheckoutWorkspace version -> Workspace.Checkout version.Version
    | PullWorkspace pullInfo -> Workspace.Pull pullInfo.Src pullInfo.Bin
    | Exec cmd -> Workspace.Exec cmd.Command
    | CleanWorkspace -> Workspace.Clean ()
    | UpdateGuids name -> Workspace.UpdateGuid name
    | TestAssemblies testInfo -> Test.TestAssemblies testInfo.Filters testInfo.Excludes

    // repository
    | AddRepository repoInfo -> Repo.Add repoInfo.Repo repoInfo.Url repoInfo.Type repoInfo.Branch
    | CloneRepositories repoInfo -> Repo.Clone repoInfo.Filters repoInfo.Shallow
    | ListRepositories -> Repo.List ()
    | DropRepository repo -> Repo.Drop repo

    // view
    | AddView viewInfo -> View.Create viewInfo.Name viewInfo.Filters
    | DropView viewInfo -> View.Drop viewInfo.Name
    | ListViews -> View.List ()
    | DescribeView viewInfo -> View.Describe viewInfo.Name
    | GraphView viewInfo -> View.Graph viewInfo.Name viewInfo.All
    | BuildView viewInfo -> View.Build viewInfo.Name viewInfo.Config viewInfo.Clean viewInfo.Multithread
    | AlterView viewInfo -> View.AlterView viewInfo.Name viewInfo.Default

    // nuget
    | AddNuGet url -> NuGets.Add url
    | ListNuGets -> NuGets.List ()

    // package
    | InstallPackages -> Package.Install ()
    | UpdatePackages -> Package.Update ()
    | OutdatedPackages -> Package.Outdated ()
    | ListPackages -> Package.List ()

    // applications
    | ListApplications -> Application.List ()
    | AddApplication appInfo -> Application.Add appInfo.Name appInfo.Project appInfo.Publisher
    | DropApplication name -> Application.Drop name
    | PublishApplications { Filters = x } -> Application.Publish x

    | Migrate -> Configuration.Migrate ()

    // misc
    | Version -> DisplayVersion ()
    | Usage -> DisplayUsage ()
    | Error -> DisplayUsage ()

    let retCode = if cmd = Error then 5
                  else 0
    retCode

[<EntryPoint>]
let main argv = 
    try
        tryMain argv
    with
        x -> printfn "---------------------------------------------------"
             printfn "Unexpected error:"
             printfn "%A" x
             printfn "---------------------------------------------------"
             5
