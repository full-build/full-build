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
    | Command.SetupWorkspace setupInfo -> WorkspaceCommands.Create setupInfo
    | Command.InitWorkspace initInfo -> WorkspaceCommands.Init initInfo
    | Command.IndexRepositories indexInfo -> WorkspaceCommands.Index indexInfo
    | Command.ConvertRepositories convertInfo -> WorkspaceCommands.Convert convertInfo
    | Command.PushWorkspace pushInfo -> WorkspaceCommands.Push pushInfo
    | Command.CheckoutWorkspace version -> WorkspaceCommands.Checkout version
    | Command.BranchWorkspace branch -> WorkspaceCommands.Branch branch
    | Command.PullWorkspace pullInfo -> WorkspaceCommands.Pull pullInfo
    | Command.Exec cmdInfo -> WorkspaceCommands.Exec cmdInfo
    | Command.CleanWorkspace -> WorkspaceCommands.Clean ()
    | Command.UpdateGuids name -> WorkspaceCommands.UpdateGuid (name.toString)
    | Command.History histInfo -> WorkspaceCommands.History histInfo
    | Command.InstallPackages -> WorkspaceCommands.Install ()

    | Command.TestAssemblies testInfo -> Test.TestAssemblies testInfo.Filters testInfo.Excludes

    // repository
    | Command.AddRepository addInfo -> RepoCommands.Add addInfo
    | Command.CloneRepositories cloneInfo -> RepoCommands.Clone cloneInfo
    | Command.ListRepositories -> RepoCommands.List ()
    | Command.DropRepository dropInfo -> RepoCommands.Drop dropInfo

    // view
    | Command.AddView viewInfo -> ViewCommands.Add viewInfo
    | Command.DropView viewInfo -> ViewCommands.Drop viewInfo.Name
    | Command.ListViews -> ViewCommands.List ()
    | Command.DescribeView viewInfo -> ViewCommands.Describe viewInfo.Name
    | Command.GraphView viewInfo -> ViewCommands.Graph viewInfo
    | Command.BuildView viewInfo -> ViewCommands.Build viewInfo
    | Command.AlterView viewInfo -> ViewCommands.Alter viewInfo
    | Command.OpenView viewInfo -> ViewCommands.Open viewInfo

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
