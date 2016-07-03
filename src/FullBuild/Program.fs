﻿//   Copyright 2014-2016 Pierre Chalamet
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
    | SetupWorkspace wsInfo -> Workspace.Create wsInfo.Path wsInfo.MasterRepository wsInfo.MasterArtifacts wsInfo.Type
    | InitWorkspace wsInfo -> Workspace.Init wsInfo.Path wsInfo.MasterRepository wsInfo.Type
    | IndexRepositories idxInfo -> Workspace.Index idxInfo.Filters
    | ConvertRepositories convInfo -> Workspace.Convert convInfo.Filters
    | PushWorkspace buildInfo -> Workspace.Push buildInfo.Branch buildInfo.BuildNumber
    | CheckoutWorkspace version -> Workspace.Checkout version.Version
    | BranchWorkspace branch -> Workspace.Branch branch.Branch
    | PullWorkspace pullInfo -> Workspace.Pull pullInfo.Src pullInfo.Bin pullInfo.Rebase pullInfo.View
    | Exec cmd -> Workspace.Exec cmd.Command cmd.All
    | CleanWorkspace -> Workspace.Clean ()
    | UpdateGuids name -> Workspace.UpdateGuid name
    | TestAssemblies testInfo -> Test.TestAssemblies testInfo.Filters testInfo.Excludes
    | History -> Workspace.History ()

    // repository
    | AddRepository repoInfo -> Repo.Add repoInfo.Repo repoInfo.Url repoInfo.Branch repoInfo.Builder
    | CloneRepositories repoInfo -> Repo.Clone repoInfo.Filters repoInfo.Shallow repoInfo.All repoInfo.Multithread
    | ListRepositories -> Repo.List ()
    | DropRepository repo -> Repo.Drop repo
    | InstallPackages -> Workspace.Install ()

    // view
    | AddView viewInfo -> View.Create viewInfo.Name viewInfo.Filters viewInfo.SourceOnly viewInfo.Parents viewInfo.AddNew
    | DropView viewInfo -> View.Drop viewInfo.Name
    | ListViews -> View.List ()
    | DescribeView viewInfo -> View.Describe viewInfo.Name
    | GraphView viewInfo -> View.Graph viewInfo.Name viewInfo.All
    | BuildView viewInfo -> View.Build viewInfo.Name viewInfo.Config viewInfo.Clean viewInfo.Multithread viewInfo.Version
    | AlterView viewInfo -> View.AlterView viewInfo.Name viewInfo.Default viewInfo.Source viewInfo.Parents
    | OpenView viewInfo -> View.OpenView viewInfo.Name

    // nuget
    | AddNuGet url -> NuGets.Add url
    | ListNuGets -> NuGets.List ()

    // package
    | UpdatePackages -> Package.Update ()
    | OutdatedPackages -> Package.Outdated ()
    | ListPackages -> Package.List ()

    // applications
    | ListApplications -> Application.List ()
    | AddApplication appInfo -> Application.Add appInfo.Name appInfo.Project appInfo.Publisher
    | DropApplication name -> Application.Drop name
    | PublishApplications pubInfo -> Application.Publish pubInfo.Filters pubInfo.Multithread
    | BindProject prjInfo -> Application.BindProject prjInfo.Filters

    // misc
    | Upgrade -> Upgrade.Upgrade ()
    | FinalizeUpgrade processId -> Upgrade.FinalizeUpgrade processId
    | Version -> DisplayVersion ()
    | Usage -> DisplayUsage MainCommand.Unknown
    | Error errInfo -> DisplayUsage errInfo

    let retCode = match cmd with
                  | Error _ -> 5
                  | _ -> 0
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
