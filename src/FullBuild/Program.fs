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
open CLI.Commands

let tryMain argv =
    Env.CheckLicense ()
    Configuration.CheckMinVersion ()

    let cmd = CLI.CommandLine.Parse (argv |> Seq.toList)

    let stopWatch = System.Diagnostics.Stopwatch.StartNew()
    match cmd with
    // workspace
    | Command.SetupWorkspace setupInfo -> Commands.Workspace.Create setupInfo
    | Command.InitWorkspace initInfo -> Commands.Workspace.Init initInfo
    | Command.IndexRepositories indexInfo -> Commands.Workspace.Index indexInfo
    | Command.ConvertRepositories convertInfo -> Commands.Workspace.Convert convertInfo
    | Command.PushWorkspace pushInfo -> Commands.Workspace.Push pushInfo
    | Command.CheckoutWorkspace version -> Commands.Workspace.Checkout version
    | Command.BranchWorkspace branch -> Commands.Workspace.Branch branch
    | Command.PullWorkspace pullInfo -> Commands.Workspace.Pull pullInfo
    | Command.Exec cmdInfo -> Commands.Workspace.Exec cmdInfo
    | Command.CleanWorkspace -> Commands.Workspace.Clean ()
    | Command.UpdateGuids updInfo -> Commands.Workspace.UpdateGuid updInfo
    | Command.History histInfo -> Commands.Workspace.History histInfo
    | Command.InstallPackages -> Commands.Workspace.Install ()

    | Command.TestAssemblies testInfo -> Commands.Test.TestAssemblies testInfo.Filters testInfo.Excludes

    // repository
    | Command.AddRepository addInfo -> Commands.Repo.Add addInfo
    | Command.CloneRepositories cloneInfo -> Commands.Repo.Clone cloneInfo
    | Command.ListRepositories -> Commands.Repo.List ()
    | Command.DropRepository dropInfo -> Commands.Repo.Drop dropInfo

    // view
    | Command.AddView viewInfo -> Commands.View.Add viewInfo
    | Command.DropView viewInfo -> Commands.View.Drop viewInfo.Name
    | Command.ListViews -> Commands.View.List ()
    | Command.DescribeView viewInfo -> Commands.View.Describe viewInfo.Name
    | Command.GraphView viewInfo -> Commands.View.Graph viewInfo
    | Command.BuildView viewInfo -> Commands.View.Build viewInfo
    | Command.AlterView viewInfo -> Commands.View.Alter viewInfo
    | Command.OpenView viewInfo -> Commands.View.Open viewInfo

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
    | Command.Version -> CLI.CommandLine.PrintVersion ()
    | Command.Usage -> CLI.CommandLine.PrintUsage MainCommand.Unknown
    | Command.Error errInfo -> CLI.CommandLine.PrintUsage errInfo

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
