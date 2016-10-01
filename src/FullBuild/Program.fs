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

module Main
open Commands

let tryMain argv =
    Env.CheckLicense ()
    Configuration.CheckMinVersion ()

    let cmd = CommandLine.Parse (argv |> Seq.toList)

    let stopWatch = System.Diagnostics.Stopwatch.StartNew()
    match cmd with
    // workspace
    | Command.SetupWorkspace wsInfo -> Workspace.Create wsInfo.Path wsInfo.MasterRepository wsInfo.MasterArtifacts wsInfo.Type
    | Command.InitWorkspace wsInfo -> Workspace.Init wsInfo.Path wsInfo.MasterRepository wsInfo.Type
    | Command.IndexRepositories idxInfo -> Workspace.Index idxInfo.Filters
    | Command.ConvertRepositories convInfo -> Workspace.Convert convInfo.Filters
    | Command.PushWorkspace buildInfo -> Workspace.Push buildInfo.Branch buildInfo.BuildNumber buildInfo.Incremental
    | Command.CheckoutWorkspace version -> Workspace.Checkout version.Version
    | Command.BranchWorkspace branch -> Workspace.Branch branch.Branch
    | Command.PullWorkspace pullInfo -> Workspace.Pull pullInfo.Src pullInfo.Bin pullInfo.Rebase pullInfo.View
    | Command.Exec cmd -> Workspace.Exec cmd.Command cmd.All
    | Command.CleanWorkspace -> Workspace.Clean ()
    | Command.UpdateGuids name -> Workspace.UpdateGuid name
    | Command.TestAssemblies testInfo -> Test.TestAssemblies testInfo.Filters testInfo.Excludes
    | Command.History histInfo -> Workspace.History histInfo.Html

    // repository
    | Command.AddRepository repoInfo -> Repo.Add repoInfo.Repo repoInfo.Url repoInfo.Branch repoInfo.Builder
    | Command.CloneRepositories repoInfo -> Repo.Clone repoInfo.Filters repoInfo.Shallow repoInfo.All repoInfo.Multithread
    | Command.ListRepositories -> Repo.List ()
    | Command.DropRepository repo -> Repo.Drop repo
    | Command.InstallPackages -> Workspace.Install ()

    // view
    | Command.AddView viewInfo -> ViewCommands.Create viewInfo.Name viewInfo.Filters viewInfo.SourceOnly viewInfo.Parents viewInfo.Modified
    | Command.DropView viewInfo -> ViewCommands.Drop viewInfo.Name
    | Command.ListViews -> ViewCommands.List ()
    | Command.DescribeView viewInfo -> ViewCommands.Describe viewInfo.Name
    | Command.GraphView viewInfo -> ViewCommands.Graph viewInfo.Name viewInfo.All
    | Command.BuildView viewInfo -> ViewCommands.Build viewInfo.Name viewInfo.Config viewInfo.Clean viewInfo.Multithread viewInfo.Version
    | Command.AlterView viewInfo -> ViewCommands.AlterView viewInfo.Name viewInfo.Default viewInfo.Source viewInfo.Parents
    | Command.OpenView viewInfo -> ViewCommands.OpenView viewInfo.Name

    // nuget
    | Command.AddNuGet url -> NuGets.Add url
    | Command.ListNuGets -> NuGets.List ()

    // package
    | Command.UpdatePackages -> Package.Update ()
    | Command.OutdatedPackages -> Package.Outdated ()
    | Command.ListPackages -> Package.List ()

    // applications
    | Command.ListApplications -> Application.List ()
    | Command.AddApplication appInfo -> Application.Add appInfo.Name appInfo.Projects appInfo.Publisher
    | Command.DropApplication name -> Application.Drop name
    | Command.PublishApplications pubInfo -> Application.Publish pubInfo.View pubInfo.Filters pubInfo.Multithread
    | Command.BindProject prjInfo -> Application.BindProject prjInfo.Filters

    // misc
    | Command.Upgrade -> Upgrade.Upgrade ()
    | Command.FinalizeUpgrade processId -> Upgrade.FinalizeUpgrade processId
    | Command.Version -> CommandLine.PrintVersion ()
    | Command.Usage -> CommandLine.PrintUsage MainCommand.Unknown
    | Command.Error errInfo -> CommandLine.PrintUsage errInfo

    stopWatch.Stop()
    printfn "Completed in %d seconds." ((int)stopWatch.Elapsed.TotalSeconds)

    let retCode = match cmd with
                  | Command.Error _ -> 5
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
